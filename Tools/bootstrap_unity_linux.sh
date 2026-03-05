#!/usr/bin/env bash
# =============================================================================
# bootstrap_unity_linux.sh
# Installs Unity 2022 LTS (2022.3.x) Linux Editor + Android Build Support
# modules (SDK, NDK, OpenJDK) for headless CI builds.
# =============================================================================
set -euo pipefail

UNITY_VERSION="2022.3.52f1"
UNITY_CHANGESET="2a787ba6bd49"
INSTALL_DIR="${UNITY_INSTALL_DIR:-/opt/unity}"
UNITY_HUB_URL="https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage"

echo "=== Emersyn Runner: Unity Linux Bootstrap ==="
echo "Unity Version: ${UNITY_VERSION}"
echo "Install Dir:   ${INSTALL_DIR}"
echo ""

# --- Prerequisites ---
echo "[1/5] Installing prerequisites..."
sudo apt-get update -qq
sudo apt-get install -y -qq \
    curl wget gconf2 libgtk-3-0 libglu1-mesa libnss3 libxss1 \
    libasound2 libgconf-2-4 libx11-xcb1 xvfb ca-certificates \
    openjdk-11-jdk unzip zip

# --- Unity Hub ---
echo "[2/5] Installing Unity Hub..."
UNITY_HUB_PATH="/usr/local/bin/UnityHub.AppImage"
if [ ! -f "${UNITY_HUB_PATH}" ]; then
    wget -q -O "${UNITY_HUB_PATH}" "${UNITY_HUB_URL}"
    chmod +x "${UNITY_HUB_PATH}"
fi

# Alternative: Direct editor download for CI (no Hub needed)
EDITOR_URL="https://download.unity3d.com/download_unity/${UNITY_CHANGESET}/LinuxEditorInstaller/Unity-${UNITY_VERSION}.tar.xz"
ANDROID_MODULE_URL="https://download.unity3d.com/download_unity/${UNITY_CHANGESET}/MacEditorTargetInstaller/UnitySetup-Android-Support-for-Editor-${UNITY_VERSION}.pkg"

echo "[3/5] Downloading Unity Editor..."
DOWNLOAD_DIR="/tmp/unity-install"
mkdir -p "${DOWNLOAD_DIR}"

if [ ! -d "${INSTALL_DIR}/Editor" ]; then
    # Try direct tarball install (preferred for CI)
    cd "${DOWNLOAD_DIR}"

    if [ ! -f "unity-editor.tar.xz" ]; then
        echo "  Downloading Unity Editor tarball..."
        wget -q --show-progress -O unity-editor.tar.xz "${EDITOR_URL}" || {
            echo "  Direct download failed. Trying Unity Hub install..."
            # Fallback: use Unity Hub CLI
            xvfb-run --auto-servernum "${UNITY_HUB_PATH}" -- --headless install \
                --version "${UNITY_VERSION}" \
                --changeset "${UNITY_CHANGESET}" \
                --module android android-sdk-ndk-tools android-open-jdk \
                --installPath "${INSTALL_DIR}" || {
                echo "ERROR: Unity installation failed via both methods."
                exit 1
            }
        }
    fi

    if [ -f "unity-editor.tar.xz" ]; then
        echo "  Extracting Unity Editor..."
        sudo mkdir -p "${INSTALL_DIR}"
        sudo tar xf unity-editor.tar.xz -C "${INSTALL_DIR}" --strip-components=1
    fi
fi

# --- Android Build Support ---
echo "[4/5] Setting up Android build support..."

# Android SDK
ANDROID_SDK="${INSTALL_DIR}/Editor/Data/PlaybackEngines/AndroidPlayer/SDK"
if [ ! -d "${ANDROID_SDK}" ]; then
    echo "  Installing Android SDK tools..."
    CMDLINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-9477386_latest.zip"
    mkdir -p "${ANDROID_SDK}/cmdline-tools"
    wget -q -O /tmp/cmdline-tools.zip "${CMDLINE_TOOLS_URL}" || true
    if [ -f /tmp/cmdline-tools.zip ]; then
        unzip -q /tmp/cmdline-tools.zip -d "${ANDROID_SDK}/cmdline-tools/" || true
        mv "${ANDROID_SDK}/cmdline-tools/cmdline-tools" "${ANDROID_SDK}/cmdline-tools/latest" 2>/dev/null || true
        yes | "${ANDROID_SDK}/cmdline-tools/latest/bin/sdkmanager" \
            "platform-tools" "platforms;android-33" "build-tools;33.0.2" 2>/dev/null || true
    fi
fi

# Android NDK
ANDROID_NDK="${INSTALL_DIR}/Editor/Data/PlaybackEngines/AndroidPlayer/NDK"
if [ ! -d "${ANDROID_NDK}" ]; then
    echo "  Android NDK will be installed by Unity Hub or bundled with editor."
fi

# --- Verify ---
echo "[5/5] Verifying installation..."
UNITY_EDITOR="${INSTALL_DIR}/Editor/Unity"
if [ -f "${UNITY_EDITOR}" ]; then
    echo "  Unity Editor found at: ${UNITY_EDITOR}"
else
    # Check alternative paths
    for candidate in \
        "${INSTALL_DIR}/Editor/Unity" \
        "/opt/Unity/Editor/Unity" \
        "${HOME}/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity"; do
        if [ -f "${candidate}" ]; then
            UNITY_EDITOR="${candidate}"
            echo "  Unity Editor found at: ${UNITY_EDITOR}"
            break
        fi
    done
fi

# Create symlink for convenience
if [ -f "${UNITY_EDITOR}" ]; then
    sudo ln -sf "${UNITY_EDITOR}" /usr/local/bin/unity-editor 2>/dev/null || true
    echo "  Symlinked to /usr/local/bin/unity-editor"
fi

echo ""
echo "=== Bootstrap Complete ==="
echo "Unity Editor: ${UNITY_EDITOR:-NOT FOUND}"
echo "Android SDK:  ${ANDROID_SDK}"
echo ""
echo "To verify: ./Tools/verify_environment.sh"
