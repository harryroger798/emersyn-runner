#!/usr/bin/env bash
# =============================================================================
# build_android.sh
# Runs Unity in headless/batchmode to build the Android APK.
# Fails fast on any error.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOG_FILE="${PROJECT_DIR}/Builds/logs/unity_build.log"
OUTPUT_APK="${PROJECT_DIR}/Builds/EmersynRunner-arm64.apk"

# Find Unity editor
UNITY_EDITOR=""
for candidate in \
    "/usr/local/bin/unity-editor" \
    "/opt/unity/Editor/Unity" \
    "/opt/Unity/Editor/Unity" \
    "${HOME}/Unity/Hub/Editor/2022.3.52f1/Editor/Unity" \
    "$(which unity-editor 2>/dev/null || true)"; do
    if [ -n "$candidate" ] && [ -f "$candidate" ]; then
        UNITY_EDITOR="$candidate"
        break
    fi
done

if [ -z "$UNITY_EDITOR" ]; then
    echo "ERROR: Unity Editor not found. Run Tools/bootstrap_unity_linux.sh first."
    exit 1
fi

echo "=== Emersyn Runner: Android Build ==="
echo "Unity Editor: ${UNITY_EDITOR}"
echo "Project Path: ${PROJECT_DIR}"
echo "Output APK:   ${OUTPUT_APK}"
echo "Log File:     ${LOG_FILE}"
echo ""

# Ensure directories
mkdir -p "${PROJECT_DIR}/Builds/logs"
mkdir -p "${PROJECT_DIR}/Builds/Release"

# Clean old build
rm -f "${OUTPUT_APK}"

# Run Unity build
echo "Starting headless build..."
BUILD_START=$(date +%s)

"${UNITY_EDITOR}" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "${PROJECT_DIR}" \
    -executeMethod BuildAndroid.Build \
    -logFile "${LOG_FILE}" \
    -buildTarget Android 2>&1 || {
    BUILD_EXIT=$?
    echo ""
    echo "=== BUILD FAILED (exit code: ${BUILD_EXIT}) ==="
    echo "Last 50 lines of log:"
    tail -50 "${LOG_FILE}" 2>/dev/null || echo "(no log file)"
    exit ${BUILD_EXIT}
}

BUILD_END=$(date +%s)
BUILD_DURATION=$((BUILD_END - BUILD_START))

# Verify output
if [ -f "${OUTPUT_APK}" ]; then
    APK_SIZE=$(stat -c%s "${OUTPUT_APK}" 2>/dev/null || stat -f%z "${OUTPUT_APK}" 2>/dev/null)
    echo ""
    echo "=== BUILD SUCCESS ==="
    echo "APK:      ${OUTPUT_APK}"
    echo "Size:     ${APK_SIZE} bytes ($(( APK_SIZE / 1048576 )) MB)"
    echo "Duration: ${BUILD_DURATION}s"

    # Copy to Release if signing was applied
    cp "${OUTPUT_APK}" "${PROJECT_DIR}/Builds/Release/" 2>/dev/null || true
    echo "Copied to Builds/Release/"
else
    echo ""
    echo "=== BUILD FAILED: APK not found ==="
    echo "Expected: ${OUTPUT_APK}"
    echo "Last 30 lines of log:"
    tail -30 "${LOG_FILE}" 2>/dev/null || echo "(no log file)"
    exit 1
fi
