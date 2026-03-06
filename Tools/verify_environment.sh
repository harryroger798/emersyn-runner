#!/usr/bin/env bash
# =============================================================================
# verify_environment.sh
# Validates that Unity, Java, Android SDK/NDK are properly installed.
# =============================================================================
set -euo pipefail

PASS=0
FAIL=0

check() {
    local name="$1"
    local cmd="$2"
    if eval "$cmd" > /dev/null 2>&1; then
        echo "  [PASS] $name"
        PASS=$((PASS + 1))
    else
        echo "  [FAIL] $name"
        FAIL=$((FAIL + 1))
    fi
}

echo "=== Emersyn Runner: Environment Verification ==="
echo ""

# Unity Editor
echo "--- Unity Editor ---"
UNITY_EDITOR=""
for candidate in \
    "/usr/local/bin/unity-editor" \
    "/opt/unity/Editor/Unity" \
    "/opt/Unity/Editor/Unity" \
    "${HOME}/Unity/Hub/Editor/2022.3.62f3/Editor/Unity" \
    "${HOME}/Unity/Hub/Editor/2022.3.52f1/Editor/Unity"; do
    if [ -f "$candidate" ]; then
        UNITY_EDITOR="$candidate"
        break
    fi
done

if [ -n "$UNITY_EDITOR" ]; then
    echo "  [PASS] Unity Editor found: $UNITY_EDITOR"
    PASS=$((PASS + 1))
    
    # Check version
    VERSION=$("$UNITY_EDITOR" -version 2>/dev/null || echo "unknown")
    echo "         Version: $VERSION"
else
    echo "  [FAIL] Unity Editor not found"
    FAIL=$((FAIL + 1))
fi

# Java / OpenJDK
echo ""
echo "--- Java ---"
check "Java installed" "java -version"
check "javac installed" "javac -version"
JAVA_HOME_VAL="${JAVA_HOME:-$(dirname $(dirname $(readlink -f $(which java 2>/dev/null) 2>/dev/null) 2>/dev/null) 2>/dev/null)}"
echo "  JAVA_HOME: ${JAVA_HOME_VAL:-not set}"

# Android SDK
echo ""
echo "--- Android SDK ---"
ANDROID_SDK_PATHS=(
    "/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/SDK"
    "${ANDROID_HOME:-/dev/null}"
    "${HOME}/Android/Sdk"
)
ANDROID_SDK=""
for p in "${ANDROID_SDK_PATHS[@]}"; do
    if [ -d "$p" ]; then
        ANDROID_SDK="$p"
        break
    fi
done

if [ -n "$ANDROID_SDK" ]; then
    echo "  [PASS] Android SDK found: $ANDROID_SDK"
    PASS=$((PASS + 1))
    check "platform-tools" "[ -d '$ANDROID_SDK/platform-tools' ]"
    check "build-tools" "ls '$ANDROID_SDK/build-tools/' 2>/dev/null | head -1"
else
    echo "  [FAIL] Android SDK not found"
    FAIL=$((FAIL + 1))
fi

# Android NDK
echo ""
echo "--- Android NDK ---"
NDK_PATHS=(
    "/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/NDK"
    "${ANDROID_NDK_HOME:-/dev/null}"
)
NDK_FOUND=false
for p in "${NDK_PATHS[@]}"; do
    if [ -d "$p" ]; then
        echo "  [PASS] Android NDK found: $p"
        PASS=$((PASS + 1))
        NDK_FOUND=true
        break
    fi
done
if ! $NDK_FOUND; then
    echo "  [WARN] Android NDK not found (may be bundled with Unity)"
fi

# Gradle
echo ""
echo "--- Gradle ---"
check "gradle" "which gradle || [ -f '/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-*.jar' ]"

# Summary
echo ""
echo "=== Summary ==="
echo "Passed: $PASS"
echo "Failed: $FAIL"

if [ "$FAIL" -gt 0 ]; then
    echo ""
    echo "WARNING: $FAIL checks failed. Run bootstrap_unity_linux.sh to fix."
    exit 1
else
    echo "All checks passed!"
    exit 0
fi
