# Emersyn Runner — Game Design Document

## 1. Overview

**Title:** Emersyn Runner  
**Genre:** Endless Runner (3-lane)  
**Platform:** Android (ARM64)  
**Engine:** Unity 2022 LTS (URP)  
**Target Audience:** Casual gamers, all ages  

Emersyn Runner is an original 3-lane endless runner featuring Emersyn, a stylized kid runner character. The game delivers smooth, responsive controls with polished visuals across multiple biomes.

## 2. Core Gameplay Loop

1. **Start** → Player taps "Play" on the main menu
2. **Run** → Emersyn runs forward automatically with increasing speed
3. **Dodge** → Swipe to change lanes, jump, or roll to avoid obstacles
4. **Collect** → Gather coins and powerups along the track
5. **Score** → Distance traveled × multiplier = score
6. **Die** → Hit an obstacle → game over (one revive available)
7. **Repeat** → Retry or return to menu

## 3. Controls (Mobile Touch)

| Gesture | Action |
|---------|--------|
| Swipe Left | Move one lane left |
| Swipe Right | Move one lane right |
| Swipe Up | Jump |
| Swipe Down | Roll / Slide |
| Double Tap | Activate shield (if available) |

Desktop testing: Arrow keys / WASD + Space (jump) + E (shield).

## 4. Character: Emersyn

- **Style:** Stylized cartoon kid runner (NOT photoreal)
- **Rig:** Unity Humanoid
- **Animations:**
  - Run loop (base layer, speed-synced)
  - Jump (start → airborne → land)
  - Roll / Slide
  - Lane change lean (additive)
  - Hit reaction / stumble
  - Celebrate / idle (menu)

## 5. Movement & Feel

### Lane Switching
- 3 lanes, 2.5m apart
- Duration: ~0.15s with easing curve
- Snappy but smooth — no jitter or oversteer
- Lane snapping feels confident

### Jump
- Height: ~3m, duration: ~0.7s
- Custom curve: quick rise, hang time, fast fall
- Can cancel into roll (fast-fall)

### Roll / Slide
- Duration: ~0.6s
- Shrinks hitbox to 35% height
- Can cancel jump into roll

### Speed Ramp
- Start: 10 m/s
- Max: 30 m/s
- Ramp time: ~120s with gradual curve
- No sudden unfair spikes

All values in `RunnerTuning` ScriptableObject for easy iteration.

## 6. Obstacles (8+ types)

| Type | Description | Avoidance |
|------|------------|-----------|
| Barrier | Waist-high wall | Jump |
| Overhead | Low ceiling/bar | Roll |
| Low Obstacle | Ground-level hazard | Jump |
| Moving | Shifts between lanes | Time lane change |
| Train (Large) | Spans 2 lanes | Switch to open lane |
| Gap | Hole in ground | Jump |
| Lane Blocker | Blocks full lane | Switch lanes |
| Staggered Combo | Multiple in pattern | Combo of moves |

## 7. Pickups

| Pickup | Effect | Duration |
|--------|--------|----------|
| Coin | +1 score (×multiplier) | Instant |
| Magnet | Attracts nearby coins | 8s |
| 2× Multiplier | Doubles score | 10s |
| Shield | Absorbs one hit | 12s |

## 8. Biomes

The track cycles through 3 biomes every ~15 segments:

1. **City** — Urban streets, buildings, traffic props
2. **Jungle** — Tropical vegetation, vines, temple ruins
3. **Candy** — Sweet-themed, colorful, candy props

Each biome has unique:
- Ground/track textures
- Skybox
- Environmental props
- Color palette

## 9. UI Screens

- **Main Menu** — Play, Settings, Shop, High Score
- **HUD** — Score, Coins, Active powerups, Pause button
- **Pause** — Resume, Main Menu
- **Game Over** — Final score, coins, high score, Revive, Retry, Menu
- **Settings** — Music/SFX volume sliders, Haptics toggle
- **Shop** — Cosmetics placeholder (future expansion)

## 10. Audio

### Music
- Menu loop (upbeat, catchy)
- Gameplay loop (energetic, driving)
- Smooth crossfade between states

### SFX
- Lane change whoosh
- Jump
- Roll / slide
- Coin pickup
- Powerup pickup
- Shield activation
- Hit / collision
- Game over sting
- UI button clicks

### Mixing
- AudioMixer groups: Music, SFX, UI
- Player-adjustable volume
- Music ducking on major events

### Haptics
- Light tick on coin
- Medium buzz on powerup
- Heavy buzz on hit
- Rate-limited to prevent fatigue

## 11. Visual Quality

- **Render Pipeline:** URP (Universal Render Pipeline)
- **Materials:** PBR
- **Lighting:** Directional + ambient
- **Post-Processing:** Bloom, Color Grading, Vignette
- **Textures:** ASTC compression
- **Performance:** Object pooling, LODs, texture streaming
- **Target:** 60 FPS on mid-range Android

## 12. Monetization (Placeholder)

- Coins collected in-game
- Shop for cosmetic items (skins, trails)
- Ad-based revive (placeholder)
- No pay-to-win mechanics
