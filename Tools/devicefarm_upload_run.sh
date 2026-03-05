#!/usr/bin/env bash
# =============================================================================
# devicefarm_upload_run.sh
# Uploads APK to AWS Device Farm and schedules a test run.
# Outputs the run ARN for polling.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
APK_PATH="${1:-${PROJECT_DIR}/Builds/EmersynRunner-arm64.apk}"
REGION="us-west-2"

# Validate
if [ ! -f "${APK_PATH}" ]; then
    echo "ERROR: APK not found at ${APK_PATH}"
    exit 1
fi

PROJECT_ARN="${DEVICEFARM_PROJECT_ARN:?DEVICEFARM_PROJECT_ARN not set}"
DEVICE_POOL_ARN="${DEVICEFARM_DEVICE_POOL_ARN:-}"

echo "=== Device Farm: Upload & Run ==="
echo "APK:         ${APK_PATH}"
echo "Project ARN: ${PROJECT_ARN}"
echo ""

# --- Create device pool if not set ---
if [ -z "${DEVICE_POOL_ARN}" ]; then
    echo "Creating device pool 'TopDevices'..."
    DEVICE_POOL_ARN=$(aws devicefarm create-device-pool \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --name "TopDevices" \
        --rules '[{"attribute":"PLATFORM","operator":"EQUALS","value":"\"ANDROID\""},{"attribute":"OS_VERSION","operator":"GREATER_THAN_OR_EQUALS","value":"\"10\""}]' \
        --query 'devicePool.arn' \
        --output text 2>/dev/null || true)

    if [ -z "${DEVICE_POOL_ARN}" ]; then
        # Pool might already exist
        DEVICE_POOL_ARN=$(aws devicefarm list-device-pools \
            --region "${REGION}" \
            --arn "${PROJECT_ARN}" \
            --query "devicePools[?name=='TopDevices'].arn | [0]" \
            --output text)
    fi

    echo "Device Pool ARN: ${DEVICE_POOL_ARN}"
    export DEVICEFARM_DEVICE_POOL_ARN="${DEVICE_POOL_ARN}"
fi

# --- Upload APK ---
echo ""
echo "Creating upload..."
UPLOAD_RESPONSE=$(aws devicefarm create-upload \
    --region "${REGION}" \
    --project-arn "${PROJECT_ARN}" \
    --name "$(basename ${APK_PATH})" \
    --type "ANDROID_APP" \
    --output json)

UPLOAD_ARN=$(echo "${UPLOAD_RESPONSE}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['arn'])")
UPLOAD_URL=$(echo "${UPLOAD_RESPONSE}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['url'])")

echo "Upload ARN: ${UPLOAD_ARN}"
echo "Uploading APK..."
curl -s -T "${APK_PATH}" "${UPLOAD_URL}"
echo "Upload complete."

# Wait for upload processing
echo "Waiting for upload processing..."
for i in $(seq 1 60); do
    STATUS=$(aws devicefarm get-upload \
        --region "${REGION}" \
        --arn "${UPLOAD_ARN}" \
        --query 'upload.status' \
        --output text)
    if [ "${STATUS}" = "SUCCEEDED" ]; then
        echo "Upload processed successfully."
        break
    elif [ "${STATUS}" = "FAILED" ]; then
        echo "ERROR: Upload processing failed."
        aws devicefarm get-upload --region "${REGION}" --arn "${UPLOAD_ARN}" --output json
        exit 1
    fi
    sleep 5
done

# --- Upload test spec (Appium if available) ---
TEST_TYPE="BUILTIN_FUZZ"
TEST_SPEC_ARN=""
APPIUM_ZIP="${PROJECT_DIR}/Tools/appium_test.zip"

if [ -f "${APPIUM_ZIP}" ]; then
    echo "Uploading Appium test package..."
    TEST_UPLOAD=$(aws devicefarm create-upload \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --name "appium_test.zip" \
        --type "APPIUM_PYTHON_TEST_PACKAGE" \
        --output json)

    TEST_SPEC_ARN=$(echo "${TEST_UPLOAD}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['arn'])")
    TEST_URL=$(echo "${TEST_UPLOAD}" | python3 -c "import sys,json; print(json.load(sys.stdin)['upload']['url'])")
    curl -s -T "${APPIUM_ZIP}" "${TEST_URL}"
    TEST_TYPE="APPIUM_PYTHON"

    # Wait for test upload processing
    for i in $(seq 1 30); do
        STATUS=$(aws devicefarm get-upload \
            --region "${REGION}" \
            --arn "${TEST_SPEC_ARN}" \
            --query 'upload.status' \
            --output text)
        [ "${STATUS}" = "SUCCEEDED" ] && break
        sleep 3
    done
fi

# --- Schedule Run ---
echo ""
echo "Scheduling test run..."
VERSION_TAG="v$(date +%Y%m%d-%H%M%S)"

if [ "${TEST_TYPE}" = "APPIUM_PYTHON" ] && [ -n "${TEST_SPEC_ARN}" ]; then
    RUN_RESPONSE=$(aws devicefarm schedule-run \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --app-arn "${UPLOAD_ARN}" \
        --device-pool-arn "${DEVICE_POOL_ARN}" \
        --name "EmersynRunner-${VERSION_TAG}" \
        --test "type=${TEST_TYPE},testPackageArn=${TEST_SPEC_ARN}" \
        --output json)
else
    RUN_RESPONSE=$(aws devicefarm schedule-run \
        --region "${REGION}" \
        --project-arn "${PROJECT_ARN}" \
        --app-arn "${UPLOAD_ARN}" \
        --device-pool-arn "${DEVICE_POOL_ARN}" \
        --name "EmersynRunner-${VERSION_TAG}" \
        --test "type=BUILTIN_FUZZ" \
        --output json)
fi

RUN_ARN=$(echo "${RUN_RESPONSE}" | python3 -c "import sys,json; print(json.load(sys.stdin)['run']['arn'])")

echo ""
echo "=== Run Scheduled ==="
echo "Run ARN:     ${RUN_ARN}"
echo "Version Tag: ${VERSION_TAG}"
echo ""
echo "To wait for completion: ./Tools/devicefarm_wait_run.sh '${RUN_ARN}'"

# Save run info for other scripts
echo "${RUN_ARN}" > "${PROJECT_DIR}/Builds/logs/latest_run_arn.txt"
echo "${VERSION_TAG}" > "${PROJECT_DIR}/Builds/logs/latest_version_tag.txt"
