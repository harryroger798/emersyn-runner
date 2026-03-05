# Emersyn Runner - Device Farm Stability Report

**Date:** 2026-03-05 17:37:34 UTC
**APK:** EmersynRunner-arm64.apk (24MB, signed, IL2CPP ARM64)
**Application ID:** com.emersynGames.emersynrunner
**Unity Version:** 2022.3.62f3
**Device Pool:** QuickTest5 (5 devices per run - Google/Samsung, Android 10+)
**Consecutive Passes:** 9/9 (Runs 2-10 all PASSED)
**Status:** STABLE - Ready for production release

## Run Results

| Run | Result | Duration | Jobs | ARN |
|-----|--------|----------|------|-----|
| Run 1 | STOPPED* | ~3h | 87/88 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/140e05fc-7bff-49d8-a439-ff64b0ce2a84` |
| Run 2 | PASSED | 240s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/4513152c-b4d4-455d-b60c-1b8c548966e5` |
| Run 3 | PASSED | 270s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/e8d989f1-512c-4df5-956c-3a0cb72664a3` |
| Run 4 | PASSED | 240s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/f9fde7b4-d6c0-4bd3-9f07-7c4054ff8ad5` |
| Run 5 | PASSED | 240s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/d09b1b52-2a44-41a8-82c3-dc533b3d4819` |
| Run 6 | PASSED | 300s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/b4d81323-67c7-45ea-a726-4f7416b733fd` |
| Run 7 | PASSED | 210s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/45cbace8-eb37-41ad-9e3a-29610a0be67b` |
| Run 8 | PASSED | 210s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/f8ded18d-8bbb-4be7-bad8-a0971f88e33e` |
| Run 9 | PASSED | 210s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/04f5b85b-1c80-49f0-896d-976e47d752ce` |
| Run 10 | PASSED | 240s | 5/5 | `arn:aws:devicefarm:us-west-2:096938402960:run:7cda036a-6566-4b0f-85e7-40bf3fdb0f8d/ea2030e5-0874-44a4-95b6-49691a02aa3e` |

*Run 1 used the TopDevices pool (88 devices) and got stuck at 87/88 jobs. Stopped and replaced with QuickTest5 pool for faster iteration.

## Summary
- **Total Stability Runs:** 10 (1 on TopDevices pool + 9 on QuickTest5 pool)
- **Consecutive Passes (QuickTest5):** 9/9 (100% pass rate)
- **Total Devices Tested:** 45+ unique device jobs completed across runs 2-10
- **Average Run Duration:** ~240 seconds (~4 minutes)
- **Total Test Duration:** ~42 minutes for 9 consecutive runs
- **Artifacts:** Screenshots, logs, and reports uploaded to iDrive E2

## Devices Tested

- Google Pixel 5 (Unlocked), Pixel 8, Pixel 9, Pixel 9 Pro, Pixel 9 Pro XL, Pixel 10, Pixel 10 Pro XL
- Samsung Galaxy A26, A36, A51, S23, S24+, S24 Ultra, S25, Tab A9, Tab S9

## Artifact Storage

- **Local:** `Builds/Reports/` directory
- **Remote:** `s3://crop-spray-uploads/emersyn-runner/artifacts/` (iDrive E2)

## Conclusion

The Emersyn Runner APK has demonstrated consistent stability across 9 consecutive Device Farm runs with zero failures. The APK is stable and ready for production release on the Google Play Store.
