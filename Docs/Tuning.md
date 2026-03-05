# Tuning Guide

## Overview

All game feel parameters are centralized in the `RunnerTuning` ScriptableObject (`Assets/Resources/RunnerTuning.asset`). Modify values in the Unity Inspector for instant iteration.

## Parameter Reference

### Lane Switching
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| laneChangeDuration | 0.15s | 0.05-0.5s | How fast lane switches complete |
| laneChangeEasing | EaseInOut | Curve | Easing curve shape |
| laneWidth | 2.5m | 1.5-4.0m | Distance between lane centers |

**Feel Notes:**
- Lower duration = snappier (arcade feel)
- Higher duration = more deliberate (realistic feel)
- The easing curve prevents jitter at start/end

### Jump
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| jumpHeight | 3.0m | 1.5-5.0m | Peak height |
| jumpDuration | 0.7s | 0.4-1.2s | Total up+down time |
| jumpCurve | Custom | Curve | Height over time |

**Feel Notes:**
- Jump curve has 40% rise, 60% fall for satisfying hang time
- Shorter duration = twitchy; longer = floaty
- Can cancel into roll for fast-fall

### Roll / Slide
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| rollDuration | 0.6s | 0.3-1.0s | How long the roll lasts |
| rollColliderScale | 0.35 | 0.1-0.8 | Hitbox height multiplier |

**Feel Notes:**
- Scale < 0.4 feels generous (forgiving)
- Scale > 0.6 requires precise timing

### Speed & Acceleration
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| startSpeed | 10 m/s | 5-15 | Initial game speed |
| maxSpeed | 30 m/s | 20-40 | Cap speed |
| timeToMaxSpeed | 120s | 60-300s | Duration of full ramp |
| accelerationCurve | Linear | Curve | Speed ramp shape |

**Feel Notes:**
- Linear curve = steady increase
- Ease-in curve = slow start, late ramp (more forgiving)
- Ease-out curve = fast start, early plateau

### Camera
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| cameraFollowDamping | 0.15 | 0.01-1.0 | Smoothing (lower = smoother) |
| cameraOffset | (0, 5, -8) | Vector3 | Position behind player |
| cameraLookAhead | 5.0 | 0-10 | Forward offset at speed |

### Input
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| swipeThreshold | 0.05 | 0.01-0.3 | Min swipe distance (screen %) |
| swipeMaxTime | 0.5s | 0.2-1.0s | Max gesture duration |

**Feel Notes:**
- Lower threshold = more sensitive (may cause false swipes)
- Higher threshold = requires deliberate swipes

### Obstacles
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| minObstacleSpacing | 15m | 8-25m | Closest obstacles can be |
| maxObstacleSpacing | 35m | 20-50m | Farthest apart |
| spacingSpeedFactor | 0.2 | 0-1 | How speed affects spacing |

### Collision
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| hitboxWidthScale | 0.7 | 0.3-1.0 | Width multiplier (forgiving) |
| hitboxHeightScale | 0.85 | 0.5-1.0 | Height multiplier |
| hitInvincibilityDuration | 1.5s | 0.5-3.0s | i-frames after hit |

### Powerups
| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| magnetDuration | 8s | 3-15s | Magnet active time |
| multiplierDuration | 10s | 5-20s | 2x score time |
| shieldDuration | 12s | 5-20s | Shield active time |
| magnetRadius | 5m | 2-10m | Coin attraction range |
| magnetSpeed | 15 m/s | 5-25 | Coin pull speed |

## Tuning Workflow

1. Open `Assets/Resources/RunnerTuning` in Inspector
2. Enter Play mode
3. Adjust values live
4. Note values that feel good
5. Exit Play mode and apply final values
6. Commit updated asset

## Reference: "Feels Right" Benchmarks

Based on popular endless runners:
- Lane switch: 0.1-0.2s
- Jump: 0.5-0.8s total
- Roll: 0.4-0.7s
- Start speed: 8-12 m/s
- Max speed: 25-35 m/s
- Time to max: 90-180s
