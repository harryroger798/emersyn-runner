# Emersyn Runner

An original 3-lane endless runner Android game built with Unity 2022 LTS. Features smooth animation-driven movement, responsive swipe controls, polished camera, audio + SFX, and automated AWS Device Farm testing.

## Features

- **3-Lane Endless Runner** — Swipe to dodge obstacles, collect coins and powerups
- **Emersyn Character** — Stylized kid runner with smooth animation blending
- **3 Biomes** — City, Jungle, Candy with unique props and visuals
- **8+ Obstacle Types** — Barriers, overheads, moving, trains, gaps, combos
- **Powerups** — Magnet, 2x Multiplier, Shield
- **Full Audio** — Music with crossfade, SFX, haptic feedback
- **URP Visuals** — PBR materials, post-processing (bloom, color grading, vignette)
- **Automated Testing** — AWS Device Farm integration with visual regression

## Quick Start

```bash
# Install Unity 2022 LTS + Android modules
./Tools/bootstrap_unity_linux.sh

# Verify environment
./Tools/verify_environment.sh

# Build APK
./Tools/build_android.sh

# Test on Device Farm
./Tools/devicefarm_upload_run.sh
./Tools/devicefarm_wait_run.sh
./Tools/devicefarm_fetch_artifacts.sh
```

## Project Structure

```
Assets/
  Editor/          -- Build scripts
  Scripts/         -- Game code (Core, Player, Obstacles, Pickups, UI, Audio, Camera, etc.)
  Animations/      -- Animation clips and controllers
  Materials/       -- PBR materials
  Prefabs/         -- Prefabs for obstacles, pickups, environment, player, UI
  Scenes/          -- Unity scenes
  Audio/           -- Music and SFX clips
  Settings/        -- URP and render pipeline settings
Docs/
  GDD.md           -- Game Design Document
  BuildAndRelease.md -- Build guide
  TestLoop.md      -- Automated test loop docs
  Tuning.md        -- Feel/tuning parameter reference
Tools/
  bootstrap_unity_linux.sh    -- Unity Linux installer
  verify_environment.sh       -- Environment validator
  build_android.sh            -- Headless APK builder
  devicefarm_upload_run.sh    -- Device Farm upload and run
  devicefarm_wait_run.sh      -- Wait for run completion
  devicefarm_fetch_artifacts.sh -- Download artifacts
  screenshot_regression.py    -- Visual regression checker
  appium_test.py              -- Automated gameplay test
Builds/
  logs/            -- Build logs
  Reports/         -- Test reports
  Release/         -- Signed APKs
```

## Documentation

- [Game Design Document](Docs/GDD.md)
- [Build and Release Guide](Docs/BuildAndRelease.md)
- [Test Loop](Docs/TestLoop.md)
- [Tuning Guide](Docs/Tuning.md)

## Tech Stack

- **Engine:** Unity 2022 LTS (2022.3.x)
- **Render Pipeline:** URP (Universal Render Pipeline)
- **Scripting Backend:** IL2CPP
- **Target:** Android ARM64, API 24+
- **Testing:** AWS Device Farm + Appium
- **Storage:** S3-compatible (iDrive E2)

## License

All rights reserved. Original game -- no reverse engineering, no copied assets.
