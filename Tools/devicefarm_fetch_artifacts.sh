#!/usr/bin/env bash
# =============================================================================
# devicefarm_fetch_artifacts.sh
# Downloads artifacts (screenshots, videos, logs) from a Device Farm run.
# Uploads them to S3-compatible storage with version tags.
# Generates a run report.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
REGION="us-west-2"

RUN_ARN="${1:-$(cat "${PROJECT_DIR}/Builds/logs/latest_run_arn.txt" 2>/dev/null || echo "")}"
VERSION_TAG="${2:-$(cat "${PROJECT_DIR}/Builds/logs/latest_version_tag.txt" 2>/dev/null || echo "$(date +%Y%m%d-%H%M%S)")}"

if [ -z "${RUN_ARN}" ]; then
    echo "ERROR: No run ARN. Usage: $0 <run-arn> [version-tag]"
    exit 1
fi

ARTIFACTS_DIR="${PROJECT_DIR}/Builds/Reports/${VERSION_TAG}"
mkdir -p "${ARTIFACTS_DIR}/screenshots" "${ARTIFACTS_DIR}/videos" "${ARTIFACTS_DIR}/logs"

echo "=== Device Farm: Fetch Artifacts ==="
echo "Run ARN:     ${RUN_ARN}"
echo "Version Tag: ${VERSION_TAG}"
echo "Output Dir:  ${ARTIFACTS_DIR}"
echo ""

# --- List Jobs ---
echo "Fetching jobs..."
JOBS=$(aws devicefarm list-jobs \
    --region "${REGION}" \
    --arn "${RUN_ARN}" \
    --output json)

JOB_COUNT=$(echo "${JOBS}" | python3 -c "import sys,json; print(len(json.load(sys.stdin)['jobs']))")
echo "Found ${JOB_COUNT} jobs."

# --- List Artifacts per Job ---
echo "Downloading artifacts..."
echo "${JOBS}" | python3 -c "
import sys, json, subprocess, os

jobs = json.load(sys.stdin)['jobs']
artifacts_dir = '${ARTIFACTS_DIR}'
region = '${REGION}'

for job in jobs:
    job_arn = job['arn']
    device_name = job.get('device', {}).get('name', 'unknown').replace(' ', '_')
    print(f'  Device: {device_name}')

    for artifact_type in ['SCREENSHOT', 'VIDEO', 'LOG', 'FILE']:
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
            for art in artifacts:
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
                    print(f'    Downloaded: {subdir}/{filename}')
        except Exception as e:
            print(f'    Warning: {e}')
"

# --- Upload to S3-compatible storage ---
echo ""
echo "Uploading artifacts to storage..."
S3_ENDPOINT="${S3_ENDPOINT_URL:-}"
S3_BUCKET_NAME="${S3_BUCKET:-}"
S3_PREFIX_PATH="${S3_PREFIX:-emersyn-runner}"

if [ -n "${S3_BUCKET_NAME}" ]; then
    S3_DEST="s3://${S3_BUCKET_NAME}/${S3_PREFIX_PATH}/artifacts/${VERSION_TAG}/"
    
    S3_CMD="aws s3 sync ${ARTIFACTS_DIR}/ ${S3_DEST}"
    if [ -n "${S3_ENDPOINT}" ]; then
        # Use S3-compatible credentials
        export AWS_ACCESS_KEY_ID="${S3_ACCESS_KEY_ID:-${AWS_ACCESS_KEY_ID}}"
        export AWS_SECRET_ACCESS_KEY="${S3_SECRET_ACCESS_KEY:-${AWS_SECRET_ACCESS_KEY}}"
        S3_CMD="${S3_CMD} --endpoint-url ${S3_ENDPOINT}"
    fi
    
    eval "${S3_CMD}" && echo "Uploaded to ${S3_DEST}" || echo "WARNING: S3 upload failed"
else
    echo "No S3 bucket configured. Artifacts saved locally only."
fi

# --- Generate Run Report ---
echo ""
echo "Generating run report..."
REPORT_FILE="${PROJECT_DIR}/Builds/Reports/RunReport-${VERSION_TAG}.md"

RUN_INFO=$(aws devicefarm get-run \
    --region "${REGION}" \
    --arn "${RUN_ARN}" \
    --output json)

python3 -c "
import json, os, glob

run_info = json.loads('''${RUN_INFO}''')
run = run_info['run']

report = []
report.append('# Device Farm Run Report')
report.append(f'')
report.append(f'**Version:** ${VERSION_TAG}')
report.append(f'**Run Name:** {run.get(\"name\", \"N/A\")}')
report.append(f'**Status:** {run.get(\"status\", \"N/A\")}')
report.append(f'**Result:** {run.get(\"result\", \"N/A\")}')
report.append(f'**Platform:** {run.get(\"platform\", \"N/A\")}')
report.append(f'')

# Counters
counters = run.get('counters', {})
report.append('## Test Counters')
report.append(f'| Metric | Count |')
report.append(f'|--------|-------|')
for k, v in counters.items():
    report.append(f'| {k} | {v} |')
report.append(f'')

# Device minutes
dm = run.get('deviceMinutes', {})
report.append('## Device Minutes')
report.append(f'- Total: {dm.get(\"total\", 0)}')
report.append(f'- Metered: {dm.get(\"metered\", 0)}')
report.append(f'- Unmetered: {dm.get(\"unmetered\", 0)}')
report.append(f'')

# Artifacts
artifacts_dir = '${ARTIFACTS_DIR}'
report.append('## Artifacts')
for subdir in ['screenshots', 'videos', 'logs']:
    files = glob.glob(os.path.join(artifacts_dir, subdir, '*'))
    if files:
        report.append(f'### {subdir.title()}')
        for f in sorted(files):
            fname = os.path.basename(f)
            report.append(f'- {fname}')
        report.append(f'')

# Crash logs
report.append('## Crash Logs')
log_files = glob.glob(os.path.join(artifacts_dir, 'logs', '*'))
crash_found = False
for lf in log_files:
    try:
        with open(lf, 'r') as f:
            content = f.read()
            if any(kw in content.lower() for kw in ['crash', 'fatal', 'exception', 'anr']):
                report.append(f'### {os.path.basename(lf)}')
                # Show first 50 lines of crash
                lines = content.split('\\n')[:50]
                report.append('\\`\\`\\`')
                report.append('\\n'.join(lines))
                report.append('\\`\\`\\`')
                crash_found = True
    except:
        pass
if not crash_found:
    report.append('No crash logs detected.')

with open('${REPORT_FILE}', 'w') as f:
    f.write('\\n'.join(report))

print(f'Report written to ${REPORT_FILE}')
"

echo ""
echo "=== Artifacts Fetched ==="
echo "Local:  ${ARTIFACTS_DIR}/"
echo "Report: ${REPORT_FILE}"
