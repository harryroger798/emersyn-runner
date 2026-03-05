# Automated Test Loop

## Overview

Every build triggers an automated test-and-iterate loop:

1. **Build** APK headlessly
2. **Upload** to AWS Device Farm
3. **Run** tests on real devices
4. **Collect** artifacts (screenshots, videos, logs)
5. **Analyze** results (visual regression, crash detection)
6. **Fix** any issues found
7. **Repeat** until stable

## Device Farm Configuration

- **Region:** us-west-2
- **Device Pool:** TopDevices (Android 10+, mid-range+)
- **Test Types:**
  - Fuzz test (built-in, always runs)
  - Appium test (automated gameplay sequence)

## Appium Test Sequence

The automated gameplay test performs:

1. Launch app
2. Wait for menu to load
3. Tap "Play" button
4. Execute swipe sequence:
   - Swipe left
   - Swipe right
   - Swipe up (jump)
   - Swipe down (roll)
   - Repeat for 90 seconds
5. Capture screenshots at:
   - Menu screen
   - Gameplay start
   - 30 seconds in
   - 60 seconds in
   - Game over screen
6. Verify app stays in foreground (no crash)

## Visual Regression

`Tools/screenshot_regression.py` compares screenshots between builds:

- **Black screen detection** — avg brightness < 5
- **Missing UI** — no visual content in expected UI regions
- **Broken rendering** — uniform color (low stddev)
- **Major changes** — pixel diff > 10% tolerance

## Success Criteria

The build is considered stable when:

- 10 consecutive Device Farm runs pass
- No crashes in any run
- Automated gameplay test completes without crash
- Visual regression check passes
- Screenshots show correct UI and gameplay

## Running the Loop

```bash
# Full automated loop
./Tools/build_android.sh && \
./Tools/devicefarm_upload_run.sh && \
./Tools/devicefarm_wait_run.sh && \
./Tools/devicefarm_fetch_artifacts.sh

# Visual regression (compare with baseline)
python3 Tools/screenshot_regression.py \
  --baseline Builds/Reports/baseline/screenshots/ \
  --current Builds/Reports/latest/screenshots/ \
  --output Builds/Reports/regression.md
```

## Artifacts Storage

Artifacts are uploaded to S3-compatible storage:
```
s3://<bucket>/<prefix>/artifacts/<version-tag>/
  screenshots/
  videos/
  logs/
```

## Reports

Generated in `Builds/Reports/`:
- `RunReport-<version>.md` — Device Farm run summary
- `regression.md` — Visual regression results
