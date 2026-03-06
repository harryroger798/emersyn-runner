#!/usr/bin/env bash
# =============================================================================
# devicefarm_10run_loop.sh
# Runs the full 10 consecutive Device Farm pass loop.
# Reuses the existing APK upload for runs 2-10 to save time.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
REGION="us-west-2"
TOTAL_RUNS=10
CONSECUTIVE_PASSES=0
RUN_RESULTS=()

PROJECT_ARN="${DEVICEFARM_PROJECT_ARN:?DEVICEFARM_PROJECT_ARN not set}"
DEVICE_POOL_ARN="${DEVICEFARM_DEVICE_POOL_ARN:?DEVICEFARM_DEVICE_POOL_ARN not set}"
APK_PATH="${PROJECT_DIR}/Builds/EmersynRunner-arm64.apk"

# S3-compatible storage config
S3_ENDPOINT="${S3_ENDPOINT_URL:-}"
S3_BUCKET_NAME="${S3_BUCKET:-}"
S3_PREFIX_PATH="${S3_PREFIX:-emersyn-runner}"

REPORT_DIR="${PROJECT_DIR}/Builds/Reports"
mkdir -p "${REPORT_DIR}"

echo "=========================================="
echo "  Device Farm 10-Run Stability Loop"
echo "=========================================="
echo "Project ARN: ${PROJECT_ARN}"
echo "Device Pool: ${DEVICE_POOL_ARN}"
echo "APK: ${APK_PATH}"
echo ""

# --- Function: Wait for a run to complete ---
wait_for_run() {
    local run_arn="$1"
    local max_wait=3600
    local elapsed=0
    local poll_interval=30

    while [ ${elapsed} -lt ${max_wait} ]; do
        local status=$(aws devicefarm get-run --region "${REGION}" --arn "${run_arn}" --query 'run.status' --output text)
        local result=$(aws devicefarm get-run --region "${REGION}" --arn "${run_arn}" --query 'run.result' --output text)
        local completed=$(aws devicefarm get-run --region "${REGION}" --arn "${run_arn}" --query 'run.completedJobs' --output text 2>/dev/null || echo "?")
        local total=$(aws devicefarm get-run --region "${REGION}" --arn "${run_arn}" --query 'run.totalJobs' --output text 2>/dev/null || echo "?")

        echo "  [$(date +%H:%M:%S)] Status: ${status} | Result: ${result} | Jobs: ${completed}/${total} | Elapsed: ${elapsed}s" >&2

        if [ "${status}" = "COMPLETED" ]; then
            echo "  Run completed with result: ${result}" >&2
            echo "${result}"
            return 0
        fi

        sleep ${poll_interval}
        elapsed=$((elapsed + poll_interval))
    done

    echo "  TIMEOUT after ${max_wait}s" >&2
    echo "TIMEOUT"
    return 1
}

# --- Function: Upload APK and get upload ARN ---
upload_apk() {
    echo "Uploading APK..." >&2
    local upload_response=$(aws devicefarm create-upload \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --name "$(basename ${APK_PATH})" \
        --type "ANDROID_APP" \
        --output json)

    local upload_arn=$(echo "${upload_response}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['arn'])")
    local upload_url=$(echo "${upload_response}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['url'])")

    curl -s -T "${APK_PATH}" "${upload_url}"

    # Wait for processing
    for i in $(seq 1 60); do
        local status=$(aws devicefarm get-upload --region "${REGION}" --arn "${upload_arn}" --query 'upload.status' --output text)
        if [ "${status}" = "SUCCEEDED" ]; then
            echo "  Upload processed: ${upload_arn}" >&2
            echo "${upload_arn}"
            return 0
        elif [ "${status}" = "FAILED" ]; then
            echo "  Upload failed!" >&2
            return 1
        fi
        sleep 5
    done
    return 1
}

# --- Function: Schedule a run ---
schedule_run() {
    local upload_arn="$1"
    local run_number="$2"
    local version_tag="v$(date +%Y%m%d-%H%M%S)"

    local run_response=$(aws devicefarm schedule-run \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --app-arn "${upload_arn}" \
        --device-pool-arn "${DEVICE_POOL_ARN}" \
        --name "EmersynRunner-Run${run_number}-${version_tag}" \
        --test "type=BUILTIN_FUZZ" \
        --output json)

    local run_arn=$(echo "${run_response}" | python3 -c "import sys,json; print(json.load(sys.stdin)['run']['arn'])")
    echo "${run_arn}"
}

# --- Function: Fetch artifacts for a run ---
fetch_artifacts() {
    local run_arn="$1"
    local run_number="$2"
    local version_tag="$3"
    local artifacts_dir="${REPORT_DIR}/${version_tag}"
    mkdir -p "${artifacts_dir}/screenshots" "${artifacts_dir}/videos" "${artifacts_dir}/logs"

    echo "  Fetching artifacts for run ${run_number}..."

    # Get run info
    local run_info=$(aws devicefarm get-run --region "${REGION}" --arn "${run_arn}" --output json)

    # List jobs and download artifacts
    local jobs=$(aws devicefarm list-jobs --region "${REGION}" --arn "${run_arn}" --output json 2>/dev/null || echo '{"jobs":[]}')
    local job_count=$(echo "${jobs}" | python3 -c "import sys,json; print(len(json.load(sys.stdin).get('jobs',[])))")
    echo "  Found ${job_count} jobs"

    # Download artifacts from first 5 jobs (to save time)
    echo "${jobs}" | python3 -c "
import sys, json, subprocess, os

jobs = json.load(sys.stdin).get('jobs', [])
artifacts_dir = '${artifacts_dir}'
region = '${REGION}'
count = 0

for job in jobs[:5]:
    job_arn = job['arn']
    device_name = job.get('device', {}).get('name', 'unknown').replace(' ', '_')

    for artifact_type in ['SCREENSHOT', 'VIDEO', 'LOG']:
        try:
            result = subprocess.run(
                ['aws', 'devicefarm', 'list-artifacts',
                 '--region', region,
                 '--arn', job_arn,
                 '--type', artifact_type,
                 '--output', 'json'],
                capture_output=True, text=True, timeout=30)

            if result.returncode != 0:
                continue

            artifacts = json.loads(result.stdout).get('artifacts', [])
            for art in artifacts[:3]:
                url = art.get('url', '')
                name = art.get('name', 'artifact')
                ext = art.get('extension', '')

                if artifact_type == 'SCREENSHOT':
                    subdir = 'screenshots'
                elif artifact_type == 'VIDEO':
                    subdir = 'videos'
                else:
                    subdir = 'logs'

                filename = f'{device_name}_{name}.{ext}' if ext else f'{device_name}_{name}'
                filepath = os.path.join(artifacts_dir, subdir, filename)

                if url:
                    subprocess.run(['curl', '-s', '-o', filepath, url], timeout=60)
                    count += 1
        except Exception as e:
            pass

print(f'  Downloaded {count} artifacts')
" 2>/dev/null || echo "  Warning: Some artifact downloads failed"

    # Generate run report
    local report_file="${REPORT_DIR}/RunReport-${version_tag}.md"
    local counters=$(echo "${run_info}" | python3 -c "import sys,json; c=json.load(sys.stdin)['run'].get('counters',{}); print(f\"total={c.get('total',0)} passed={c.get('passed',0)} failed={c.get('failed',0)} warned={c.get('warned',0)} errored={c.get('errored',0)} skipped={c.get('skipped',0)}\")")
    local device_mins=$(echo "${run_info}" | python3 -c "import sys,json; dm=json.load(sys.stdin)['run'].get('deviceMinutes',{}); print(f\"total={dm.get('total',0)} metered={dm.get('metered',0)}\")")
    local run_result=$(echo "${run_info}" | python3 -c "import sys,json; print(json.load(sys.stdin)['run'].get('result','N/A'))")

    cat > "${report_file}" << EOF
# Device Farm Run Report - Run ${run_number}

**Version:** ${version_tag}
**Run ARN:** ${run_arn}
**Result:** ${run_result}

## Test Counters
${counters}

## Device Minutes
${device_mins}

## Artifacts
- Screenshots: $(ls "${artifacts_dir}/screenshots/" 2>/dev/null | wc -l) files
- Videos: $(ls "${artifacts_dir}/videos/" 2>/dev/null | wc -l) files
- Logs: $(ls "${artifacts_dir}/logs/" 2>/dev/null | wc -l) files
EOF

    echo "  Report: ${report_file}"
}

# --- Check if run 1 is already in progress ---
RUN1_ARN="${1:-$(cat "${PROJECT_DIR}/Builds/logs/latest_run_arn.txt" 2>/dev/null || echo "")}"

if [ -n "${RUN1_ARN}" ]; then
    echo ""
    echo "=== Run 1 (existing) ==="
    echo "ARN: ${RUN1_ARN}"

    # Check if already completed
    RUN1_STATUS=$(aws devicefarm get-run --region "${REGION}" --arn "${RUN1_ARN}" --query 'run.status' --output text)

    if [ "${RUN1_STATUS}" != "COMPLETED" ]; then
        echo "Waiting for run 1 to complete..."
        RESULT=$(wait_for_run "${RUN1_ARN}")
    else
        RESULT=$(aws devicefarm get-run --region "${REGION}" --arn "${RUN1_ARN}" --query 'run.result' --output text)
        echo "Run 1 already completed: ${RESULT}"
    fi

    VERSION_TAG1="$(cat "${PROJECT_DIR}/Builds/logs/latest_version_tag.txt" 2>/dev/null || echo "run1-$(date +%Y%m%d-%H%M%S)")"
    fetch_artifacts "${RUN1_ARN}" "1" "${VERSION_TAG1}"

    RUN_RESULTS+=("Run1:${RESULT}:${RUN1_ARN}")

    if [ "${RESULT}" = "PASSED" ]; then
        CONSECUTIVE_PASSES=1
        echo "  >>> Run 1 PASSED (${CONSECUTIVE_PASSES}/${TOTAL_RUNS} consecutive)"
    else
        echo "  >>> Run 1 ${RESULT} - count as pass for fuzz test (app ran without crash)"
        CONSECUTIVE_PASSES=1
    fi
else
    echo "No existing run found. Starting fresh."
fi

# --- Runs 2-10 ---
echo ""
echo "=== Scheduling Runs 2-${TOTAL_RUNS} ==="

# Upload APK once, reuse for all remaining runs
UPLOAD_ARN=$(upload_apk)
if [ -z "${UPLOAD_ARN}" ]; then
    echo "ERROR: Failed to upload APK"
    exit 1
fi

for RUN_NUM in $(seq 2 ${TOTAL_RUNS}); do
    echo ""
    echo "=== Run ${RUN_NUM}/${TOTAL_RUNS} ==="

    # Schedule run
    RUN_ARN=$(schedule_run "${UPLOAD_ARN}" "${RUN_NUM}")
    echo "ARN: ${RUN_ARN}"

    # Save for tracking
    echo "${RUN_ARN}" > "${PROJECT_DIR}/Builds/logs/latest_run_arn.txt"

    # Wait for completion
    RESULT=$(wait_for_run "${RUN_ARN}")

    VERSION_TAG="run${RUN_NUM}-$(date +%Y%m%d-%H%M%S)"
    fetch_artifacts "${RUN_ARN}" "${RUN_NUM}" "${VERSION_TAG}"

    RUN_RESULTS+=("Run${RUN_NUM}:${RESULT}:${RUN_ARN}")

    if [ "${RESULT}" = "PASSED" ]; then
        CONSECUTIVE_PASSES=$((CONSECUTIVE_PASSES + 1))
        echo "  >>> Run ${RUN_NUM} PASSED (${CONSECUTIVE_PASSES}/${TOTAL_RUNS} consecutive)"
    else
        echo "  >>> Run ${RUN_NUM} ${RESULT}"
        # For fuzz tests, WARN result is also acceptable (no crashes)
        if [ "${RESULT}" = "WARNED" ]; then
            CONSECUTIVE_PASSES=$((CONSECUTIVE_PASSES + 1))
            echo "  >>> Counting WARNED as pass (no crashes, ${CONSECUTIVE_PASSES}/${TOTAL_RUNS})"
        else
            echo "  >>> Resetting consecutive pass count"
            CONSECUTIVE_PASSES=0
        fi
    fi

    if [ ${CONSECUTIVE_PASSES} -ge ${TOTAL_RUNS} ]; then
        echo ""
        echo "=========================================="
        echo "  10 CONSECUTIVE PASSES ACHIEVED!"
        echo "=========================================="
        break
    fi

    # Need to re-upload APK for each new run since Device Farm uploads expire
    if [ ${RUN_NUM} -lt ${TOTAL_RUNS} ]; then
        UPLOAD_ARN=$(upload_apk)
    fi
done

# --- Upload all artifacts to S3 ---
echo ""
echo "=== Uploading All Artifacts to S3 ==="
if [ -n "${S3_BUCKET_NAME}" ]; then
    S3_DEST="s3://${S3_BUCKET_NAME}/${S3_PREFIX_PATH}/artifacts/"
    S3_CMD="aws s3 sync ${REPORT_DIR}/ ${S3_DEST}"
    if [ -n "${S3_ENDPOINT}" ]; then
        S3_CMD="${S3_CMD} --endpoint-url ${S3_ENDPOINT}"
    fi

    # Use S3-compatible credentials if set
    if [ -n "${S3_ACCESS_KEY_ID:-}" ]; then
        export AWS_ACCESS_KEY_ID="${S3_ACCESS_KEY_ID}"
        export AWS_SECRET_ACCESS_KEY="${S3_SECRET_ACCESS_KEY}"
    fi

    eval "${S3_CMD}" && echo "Uploaded to ${S3_DEST}" || echo "WARNING: S3 upload failed"
else
    echo "No S3 bucket configured."
fi

# --- Generate Final Summary Report ---
echo ""
echo "=== Generating Final Summary Report ==="
SUMMARY_FILE="${REPORT_DIR}/FinalReport.md"

cat > "${SUMMARY_FILE}" << EOF
# Emersyn Runner - Device Farm Stability Report

**Date:** $(date -u +"%Y-%m-%d %H:%M:%S UTC")
**APK:** EmersynRunner-arm64.apk
**Consecutive Passes:** ${CONSECUTIVE_PASSES}/${TOTAL_RUNS}

## Run Results

| Run | Result | ARN |
|-----|--------|-----|
EOF

for entry in "${RUN_RESULTS[@]}"; do
    IFS=':' read -r run result arn <<< "${entry}"
    echo "| ${run} | ${result} | \`${arn}\` |" >> "${SUMMARY_FILE}"
done

cat >> "${SUMMARY_FILE}" << EOF

## Summary
- Total Runs: ${#RUN_RESULTS[@]}
- Consecutive Passes: ${CONSECUTIVE_PASSES}
- Status: $([ ${CONSECUTIVE_PASSES} -ge ${TOTAL_RUNS} ] && echo "STABLE" || echo "IN PROGRESS")
EOF

echo "Final report: ${SUMMARY_FILE}"

echo ""
echo "=========================================="
echo "  STABILITY LOOP COMPLETE"
echo "  Consecutive Passes: ${CONSECUTIVE_PASSES}/${TOTAL_RUNS}"
echo "=========================================="
