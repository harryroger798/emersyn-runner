#!/usr/bin/env bash
# =============================================================================
# devicefarm_wait_run.sh
# Polls AWS Device Farm until a run completes, then reports status.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
REGION="us-west-2"

RUN_ARN="${1:-$(cat "${PROJECT_DIR}/Builds/logs/latest_run_arn.txt" 2>/dev/null || echo "")}"

if [ -z "${RUN_ARN}" ]; then
    echo "ERROR: No run ARN provided. Usage: $0 <run-arn>"
    exit 1
fi

echo "=== Device Farm: Waiting for Run ==="
echo "Run ARN: ${RUN_ARN}"
echo ""

POLL_INTERVAL=30
MAX_WAIT=3600  # 1 hour max
ELAPSED=0

while [ $ELAPSED -lt $MAX_WAIT ]; do
    RUN_INFO=$(aws devicefarm get-run \
        --region "${REGION}" \
        --arn "${RUN_ARN}" \
        --output json)

    STATUS=$(echo "${RUN_INFO}" | python3 -c "import sys,json; print(json.load(sys.stdin)['run']['status'])")
    RESULT=$(echo "${RUN_INFO}" | python3 -c "import sys,json; print(json.load(sys.stdin)['run'].get('result','PENDING'))")

    echo "[$(date +%H:%M:%S)] Status: ${STATUS} | Result: ${RESULT} | Elapsed: ${ELAPSED}s"

    if [ "${STATUS}" = "COMPLETED" ]; then
        echo ""
        echo "=== Run Completed ==="
        echo "Result: ${RESULT}"

        # Get device minutes
        DEVICE_MINUTES=$(echo "${RUN_INFO}" | python3 -c "
import sys, json
run = json.load(sys.stdin)['run']
dm = run.get('deviceMinutes', {})
print(f\"  Total: {dm.get('total', 0)} min | Metered: {dm.get('metered', 0)} min | Unmetered: {dm.get('unmetered', 0)} min\")
")
        echo "Device Minutes: ${DEVICE_MINUTES}"

        # Get counters
        echo ""
        echo "Test Counters:"
        echo "${RUN_INFO}" | python3 -c "
import sys, json
run = json.load(sys.stdin)['run']
counters = run.get('counters', {})
for k, v in counters.items():
    print(f'  {k}: {v}')
"

        # Save result
        echo "${RESULT}" > "${PROJECT_DIR}/Builds/logs/latest_run_result.txt"

        if [ "${RESULT}" = "PASSED" ]; then
            echo ""
            echo "ALL TESTS PASSED!"
            exit 0
        else
            echo ""
            echo "TESTS DID NOT ALL PASS. Check artifacts for details."
            echo "Run: ./Tools/devicefarm_fetch_artifacts.sh '${RUN_ARN}'"
            exit 1
        fi
    fi

    sleep ${POLL_INTERVAL}
    ELAPSED=$((ELAPSED + POLL_INTERVAL))
done

echo ""
echo "ERROR: Run timed out after ${MAX_WAIT}s"
exit 1
