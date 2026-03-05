# Build & Release Guide

## Prerequisites

- Linux (Ubuntu 20.04+ recommended)
- Unity 2022 LTS (2022.3.x) with Android Build Support
- Android SDK (API 33), NDK, OpenJDK 11
- AWS CLI configured (for Device Farm)

## Quick Start

```bash
# 1. Bootstrap Unity (first time only)
chmod +x Tools/*.sh
./Tools/bootstrap_unity_linux.sh

# 2. Verify environment
./Tools/verify_environment.sh

# 3. Build APK
./Tools/build_android.sh

# 4. Upload to Device Farm & run tests
./Tools/devicefarm_upload_run.sh

# 5. Wait for results
./Tools/devicefarm_wait_run.sh

# 6. Fetch artifacts
./Tools/devicefarm_fetch_artifacts.sh
```

## Build Pipeline Details

### Headless Build
The build runs Unity in batchmode without graphics:
```
unity-editor -batchmode -nographics -quit \
  -projectPath <path> \
  -executeMethod BuildAndroid.Build \
  -logFile Builds/logs/unity_build.log
```

### Build Configuration
- **Scripting Backend:** IL2CPP
- **Target Architecture:** ARM64
- **Min SDK:** Android API 24 (Android 7.0)
- **Target SDK:** Android API 33
- **Output:** `Builds/EmersynRunner-arm64.apk`

### Signing
- Set env vars: `KEYSTORE_FILE`, `KEYSTORE_ALIAS`, `KEYSTORE_PASSWORD`, `KEY_PASSWORD`
- If not set, debug keystore is used
- NEVER commit keystore files or passwords

## Release Checklist

1. All 10 consecutive Device Farm runs pass
2. No crashes detected in logs
3. Visual regression check passes
4. Tuning values reviewed (see Docs/Tuning.md)
5. Signed APK in `Builds/Release/`
6. Run report generated in `Builds/Reports/`

## Troubleshooting

- **Build fails:** Check `Builds/logs/unity_build.log`
- **Unity not found:** Run `Tools/bootstrap_unity_linux.sh`
- **Android SDK issues:** Run `Tools/verify_environment.sh`
- **Device Farm fails:** Check AWS credentials and region (us-west-2)
