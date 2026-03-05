#!/usr/bin/env python3
"""
modal_generate_assets.py
Generates ALL game assets for Emersyn Runner using Modal GPU compute.
- Character textures (Emersyn stylized kid runner)
- Environment textures (City, Jungle, Candy biomes)
- Obstacle textures
- UI elements
- Skybox textures
- Audio (music loops, SFX)

Uses Stable Diffusion XL for textures and AudioCraft for audio.
"""

import modal
import os
import io
import struct
import json
import wave
import random
import math
import hashlib
from pathlib import Path

# --- Modal App Setup ---
app = modal.App("emersyn-runner-assets")

# Image with all ML dependencies
gpu_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install(
        "torch>=2.1.0",
        "torchvision",
        "torchaudio",
        "diffusers>=0.25.0",
        "transformers>=4.36.0",
        "accelerate>=0.25.0",
        "safetensors",
        "Pillow>=10.0",
        "numpy",
        "scipy",
    )
    .pip_install("xformers")
)

# Lighter image for audio generation (procedural)
audio_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install("numpy", "scipy", "Pillow")
)

# Volume to store generated assets
asset_volume = modal.Volume.from_name("emersyn-runner-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


# =============================================================================
# TEXTURE GENERATION (Stable Diffusion XL)
# =============================================================================

@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=600,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_texture(prompt: str, filename: str, width: int = 1024, height: int = 1024, negative_prompt: str = ""):
    """Generate a single texture using SDXL."""
    import torch
    from diffusers import StableDiffusionXLPipeline

    pipe = StableDiffusionXLPipeline.from_pretrained(
        "stabilityai/stable-diffusion-xl-base-1.0",
        torch_dtype=torch.float16,
        variant="fp16",
        use_safetensors=True,
    )
    pipe = pipe.to("cuda")
    pipe.enable_xformers_memory_efficient_attention()

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence"
    full_negative = f"{default_negative}, {negative_prompt}" if negative_prompt else default_negative

    # Use deterministic seed from filename for reproducibility
    seed = int(hashlib.md5(filename.encode()).hexdigest()[:8], 16)
    generator = torch.Generator("cuda").manual_seed(seed)

    image = pipe(
        prompt=prompt,
        negative_prompt=full_negative,
        width=width,
        height=height,
        num_inference_steps=30,
        guidance_scale=7.5,
        generator=generator,
    ).images[0]

    # Save to volume
    output_path = os.path.join(ASSET_MOUNT, filename)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    image.save(output_path, "PNG")

    # Also save as bytes for return
    buf = io.BytesIO()
    image.save(buf, format="PNG")
    asset_volume.commit()
    return buf.getvalue()


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=900,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_texture_batch(prompts_and_filenames: list):
    """Generate multiple textures in one GPU session for efficiency."""
    import torch
    from diffusers import StableDiffusionXLPipeline

    pipe = StableDiffusionXLPipeline.from_pretrained(
        "stabilityai/stable-diffusion-xl-base-1.0",
        torch_dtype=torch.float16,
        variant="fp16",
        use_safetensors=True,
    )
    pipe = pipe.to("cuda")
    pipe.enable_xformers_memory_efficient_attention()

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence"
    results = {}

    for item in prompts_and_filenames:
        prompt = item["prompt"]
        filename = item["filename"]
        width = item.get("width", 1024)
        height = item.get("height", 1024)
        negative = item.get("negative", "")

        full_negative = f"{default_negative}, {negative}" if negative else default_negative
        seed = int(hashlib.md5(filename.encode()).hexdigest()[:8], 16)
        generator = torch.Generator("cuda").manual_seed(seed)

        image = pipe(
            prompt=prompt,
            negative_prompt=full_negative,
            width=width,
            height=height,
            num_inference_steps=30,
            guidance_scale=7.5,
            generator=generator,
        ).images[0]

        output_path = os.path.join(ASSET_MOUNT, filename)
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        image.save(output_path, "PNG")

        buf = io.BytesIO()
        image.save(buf, format="PNG")
        results[filename] = buf.getvalue()
        print(f"  Generated: {filename}")

    asset_volume.commit()
    return results


# =============================================================================
# PROCEDURAL AUDIO GENERATION
# =============================================================================

@app.function(
    image=audio_image,
    timeout=300,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_audio_assets():
    """Generate all audio assets procedurally (music loops + SFX)."""
    import numpy as np
    from scipy.io import wavfile

    sample_rate = 44100
    results = {}

    def save_wav(filename, data, sr=sample_rate):
        """Save numpy array as WAV file."""
        # Normalize to int16
        if data.dtype == np.float64 or data.dtype == np.float32:
            data = np.clip(data, -1.0, 1.0)
            data = (data * 32767).astype(np.int16)
        output_path = os.path.join(ASSET_MOUNT, filename)
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        wavfile.write(output_path, sr, data)
        results[filename] = True
        print(f"  Audio: {filename}")

    def sine_wave(freq, duration, sr=sample_rate, volume=0.5):
        t = np.linspace(0, duration, int(sr * duration), endpoint=False)
        return volume * np.sin(2 * np.pi * freq * t)

    def noise(duration, sr=sample_rate, volume=0.1):
        return volume * np.random.randn(int(sr * duration))

    def envelope(signal, attack=0.01, decay=0.05, sustain=0.7, release=0.1, sr=sample_rate):
        n = len(signal)
        env = np.ones(n)
        a = int(attack * sr)
        d = int(decay * sr)
        r = int(release * sr)
        if a > 0:
            env[:a] = np.linspace(0, 1, a)
        if d > 0 and a + d < n:
            env[a:a+d] = np.linspace(1, sustain, d)
        if a + d < n - r:
            env[a+d:n-r] = sustain
        if r > 0:
            env[n-r:] = np.linspace(sustain, 0, r)
        return signal * env

    def lowpass(signal, cutoff_ratio=0.1):
        from scipy.signal import butter, filtfilt
        b, a = butter(4, cutoff_ratio, btype='low')
        return filtfilt(b, a, signal)

    # --- MUSIC: Menu Loop (8 bars, 120 BPM, C major, happy) ---
    bpm = 120
    beat = 60.0 / bpm
    bar = beat * 4
    duration = bar * 8  # 8 bars = 16 seconds

    # Melody (simple pentatonic)
    melody_notes = [523, 587, 659, 784, 880, 784, 659, 587,  # C5 D5 E5 G5 A5 G5 E5 D5
                    523, 659, 784, 880, 1047, 880, 784, 659,
                    523, 587, 659, 784, 659, 587, 523, 440,
                    523, 587, 659, 784, 880, 1047, 880, 784]
    melody = np.zeros(int(sample_rate * duration))
    note_dur = beat * 0.8
    for i, freq in enumerate(melody_notes):
        start = int(i * beat * sample_rate / 2)
        note = envelope(sine_wave(freq, note_dur, volume=0.3), attack=0.01, release=0.05)
        end = start + len(note)
        if end <= len(melody):
            melody[start:end] += note

    # Bass (root notes)
    bass_notes = [131, 131, 165, 165, 175, 175, 131, 131] * 4
    bass = np.zeros(int(sample_rate * duration))
    for i, freq in enumerate(bass_notes):
        start = int(i * beat * sample_rate)
        note = envelope(sine_wave(freq, beat * 0.9, volume=0.4), attack=0.01, release=0.1)
        end = start + len(note)
        if end <= len(bass):
            bass[start:end] += note

    # Pad (sustained chords)
    pad = sine_wave(262, duration, volume=0.08) + sine_wave(330, duration, volume=0.06) + sine_wave(392, duration, volume=0.06)
    pad = lowpass(pad, 0.02)

    menu_music = melody + bass + pad
    menu_music = np.clip(menu_music, -0.9, 0.9)
    save_wav("audio/music/menu_loop.wav", menu_music)

    # --- MUSIC: Gameplay Loop (8 bars, 140 BPM, driving, energetic) ---
    bpm2 = 140
    beat2 = 60.0 / bpm2
    duration2 = beat2 * 4 * 8

    # Driving bass
    gameplay_bass = np.zeros(int(sample_rate * duration2))
    bass_pattern = [65, 65, 87, 65, 98, 65, 87, 65] * 8
    for i, freq in enumerate(bass_pattern):
        start = int(i * beat2 * sample_rate / 2)
        note = envelope(sine_wave(freq, beat2 * 0.4, volume=0.5), attack=0.005, release=0.02)
        end = start + len(note)
        if end <= len(gameplay_bass):
            gameplay_bass[start:end] += note

    # Hi-hat pattern
    hihat = np.zeros(int(sample_rate * duration2))
    for i in range(int(duration2 / (beat2 / 2))):
        start = int(i * beat2 * sample_rate / 2)
        hit = envelope(noise(0.03, volume=0.15), attack=0.001, release=0.02)
        end = start + len(hit)
        if end <= len(hihat):
            hihat[start:end] += hit

    # Melody synth
    gameplay_mel = np.zeros(int(sample_rate * duration2))
    gm_notes = [392, 440, 523, 587, 659, 523, 440, 392,
                349, 392, 440, 523, 659, 523, 440, 349] * 4
    for i, freq in enumerate(gm_notes):
        start = int(i * beat2 * sample_rate / 2)
        note = envelope(sine_wave(freq, beat2 * 0.3, volume=0.2), attack=0.005, release=0.03)
        end = start + len(note)
        if end <= len(gameplay_mel):
            gameplay_mel[start:end] += note

    gameplay_music = gameplay_bass + hihat + gameplay_mel
    gameplay_music = np.clip(gameplay_music, -0.9, 0.9)
    save_wav("audio/music/gameplay_loop.wav", gameplay_music)

    # --- SFX ---

    # Whoosh (lane change)
    t_whoosh = np.linspace(0, 0.25, int(sample_rate * 0.25), endpoint=False)
    whoosh = noise(0.25, volume=0.4)
    whoosh_env = np.exp(-t_whoosh * 8) * np.sin(np.pi * t_whoosh / 0.25)
    whoosh = lowpass(whoosh * whoosh_env, 0.15)
    save_wav("audio/sfx/whoosh.wav", whoosh)

    # Jump
    t_jump = np.linspace(0, 0.3, int(sample_rate * 0.3), endpoint=False)
    jump = 0.5 * np.sin(2 * np.pi * (400 + 600 * t_jump / 0.3) * t_jump)
    jump = envelope(jump, attack=0.01, release=0.1)
    save_wav("audio/sfx/jump.wav", jump)

    # Roll/slide
    roll_dur = 0.4
    roll = lowpass(noise(roll_dur, volume=0.3), 0.08)
    roll = envelope(roll, attack=0.05, sustain=0.8, release=0.1)
    save_wav("audio/sfx/roll.wav", roll)

    # Coin pickup
    coin = envelope(sine_wave(1200, 0.08, volume=0.4), attack=0.005, release=0.02) + \
           envelope(sine_wave(1600, 0.08, volume=0.3), attack=0.005, release=0.02)
    t_coin = np.linspace(0, 0.15, int(sample_rate * 0.15), endpoint=False)
    coin2 = 0.3 * np.sin(2 * np.pi * 2400 * t_coin) * np.exp(-t_coin * 30)
    coin_full = np.zeros(int(sample_rate * 0.15))
    coin_full[:len(coin)] += coin
    coin_full[:len(coin2)] += coin2
    save_wav("audio/sfx/coin_pickup.wav", coin_full)

    # Powerup pickup
    t_pu = np.linspace(0, 0.5, int(sample_rate * 0.5), endpoint=False)
    powerup = 0.3 * np.sin(2 * np.pi * (300 + 700 * t_pu / 0.5) * t_pu) * np.exp(-t_pu * 4)
    powerup += 0.2 * np.sin(2 * np.pi * (600 + 400 * t_pu / 0.5) * t_pu) * np.exp(-t_pu * 5)
    save_wav("audio/sfx/powerup_pickup.wav", powerup)

    # Shield activation
    t_sh = np.linspace(0, 0.6, int(sample_rate * 0.6), endpoint=False)
    shield = 0.3 * np.sin(2 * np.pi * 200 * t_sh) * np.exp(-t_sh * 3)
    shield += 0.2 * np.sin(2 * np.pi * 400 * t_sh) * np.exp(-t_sh * 4)
    shield += lowpass(noise(0.6, volume=0.15) * np.exp(-t_sh * 5), 0.05)
    save_wav("audio/sfx/shield_activate.wav", shield)

    # Hit/collision
    t_hit = np.linspace(0, 0.4, int(sample_rate * 0.4), endpoint=False)
    hit = noise(0.4, volume=0.5) * np.exp(-t_hit * 8)
    hit += 0.4 * np.sin(2 * np.pi * 80 * t_hit) * np.exp(-t_hit * 6)
    save_wav("audio/sfx/hit.wav", hit)

    # Game over sting
    go_notes = [(523, 0.2), (440, 0.2), (349, 0.2), (262, 0.5)]
    go_signal = np.array([], dtype=np.float64)
    for freq, dur in go_notes:
        note = envelope(sine_wave(freq, dur, volume=0.4), attack=0.01, release=0.05)
        go_signal = np.concatenate([go_signal, note])
    save_wav("audio/sfx/game_over.wav", go_signal)

    # UI click
    t_click = np.linspace(0, 0.05, int(sample_rate * 0.05), endpoint=False)
    click = 0.4 * np.sin(2 * np.pi * 800 * t_click) * np.exp(-t_click * 60)
    save_wav("audio/sfx/ui_click.wav", click)

    # Streak/combo stinger
    combo_notes = [659, 784, 880, 1047]
    combo = np.array([], dtype=np.float64)
    for freq in combo_notes:
        note = envelope(sine_wave(freq, 0.1, volume=0.3), attack=0.005, release=0.03)
        combo = np.concatenate([combo, note])
    save_wav("audio/sfx/combo_stinger.wav", combo)

    asset_volume.commit()
    return results


# =============================================================================
# 3D MODEL GENERATION (Procedural geometry as OBJ)
# =============================================================================

@app.function(
    image=audio_image,
    timeout=300,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_3d_models():
    """Generate simple 3D models as OBJ files for obstacles and props."""
    import numpy as np

    results = {}

    def save_obj(filename, vertices, faces, normals=None):
        output_path = os.path.join(ASSET_MOUNT, filename)
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, 'w') as f:
            f.write(f"# Emersyn Runner - {filename}\n")
            for v in vertices:
                f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
            if normals:
                for n in normals:
                    f.write(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}\n")
            for face in faces:
                if normals:
                    f.write("f " + " ".join(f"{v}//{v}" for v in face) + "\n")
                else:
                    f.write("f " + " ".join(str(v) for v in face) + "\n")
        results[filename] = True
        print(f"  Model: {filename}")

    def box(sx, sy, sz, ox=0, oy=0, oz=0):
        """Generate a box mesh."""
        hx, hy, hz = sx/2, sy/2, sz/2
        verts = [
            (ox-hx, oy-hy, oz-hz), (ox+hx, oy-hy, oz-hz),
            (ox+hx, oy+hy, oz-hz), (ox-hx, oy+hy, oz-hz),
            (ox-hx, oy-hy, oz+hz), (ox+hx, oy-hy, oz+hz),
            (ox+hx, oy+hy, oz+hz), (ox-hx, oy+hy, oz+hz),
        ]
        faces = [
            (1,2,3,4), (5,8,7,6), (1,5,6,2),
            (3,7,8,4), (2,6,7,3), (1,4,8,5),
        ]
        return verts, faces

    def cylinder(radius, height, segments=16, ox=0, oy=0, oz=0):
        """Generate a cylinder mesh."""
        verts = []
        faces = []
        # Bottom center
        verts.append((ox, oy, oz))
        # Bottom ring
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            x = ox + radius * math.cos(angle)
            z = oz + radius * math.sin(angle)
            verts.append((x, oy, z))
        # Top center
        verts.append((ox, oy + height, oz))
        top_center = len(verts)
        # Top ring
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            x = ox + radius * math.cos(angle)
            z = oz + radius * math.sin(angle)
            verts.append((x, oy + height, z))
        # Bottom face
        for i in range(segments):
            next_i = (i + 1) % segments
            faces.append((1, i + 2, next_i + 2))
        # Top face
        for i in range(segments):
            next_i = (i + 1) % segments
            faces.append((top_center, top_center + next_i + 1, top_center + i + 1))
        # Side faces
        for i in range(segments):
            next_i = (i + 1) % segments
            b1 = i + 2
            b2 = next_i + 2
            t1 = top_center + i + 1
            t2 = top_center + next_i + 1
            faces.append((b1, b2, t2, t1))
        return verts, faces

    # --- Obstacle: Barrier (waist-high wall) ---
    v, f = box(2.5, 1.2, 0.4)
    # Shift up so bottom is at y=0
    v = [(x, y + 0.6, z) for x, y, z in v]
    save_obj("models/obstacles/barrier.obj", v, f)

    # --- Obstacle: Overhead bar ---
    v, f = box(2.5, 0.3, 0.3, oy=2.0)
    # Add posts
    v2, f2 = box(0.15, 2.0, 0.15, ox=-1.1, oy=1.0)
    v3, f3 = box(0.15, 2.0, 0.15, ox=1.1, oy=1.0)
    offset1 = len(v)
    offset2 = offset1 + len(v2)
    v.extend(v2)
    v.extend(v3)
    f.extend([(a + offset1, b + offset1, c + offset1, d + offset1) for a, b, c, d in f2])
    f.extend([(a + offset2, b + offset2, c + offset2, d + offset2) for a, b, c, d in f3])
    save_obj("models/obstacles/overhead.obj", v, f)

    # --- Obstacle: Low obstacle (ground level) ---
    v, f = box(2.0, 0.4, 1.0, oy=0.2)
    save_obj("models/obstacles/low_obstacle.obj", v, f)

    # --- Obstacle: Train (large, spans 2 lanes) ---
    v, f = box(5.0, 3.0, 8.0, oy=1.5)
    save_obj("models/obstacles/train.obj", v, f)

    # --- Obstacle: Lane blocker (full lane width) ---
    v, f = box(2.5, 2.5, 0.5, oy=1.25)
    save_obj("models/obstacles/lane_blocker.obj", v, f)

    # --- Pickup: Coin ---
    v, f = cylinder(0.3, 0.08, 16, oy=0.8)
    save_obj("models/pickups/coin.obj", v, f)

    # --- Pickup: Powerup capsule ---
    v, f = cylinder(0.25, 0.6, 12, oy=0.8)
    save_obj("models/pickups/powerup_capsule.obj", v, f)

    # --- Track segment (flat ground) ---
    v, f = box(7.5, 0.2, 20.0, oy=-0.1)
    save_obj("models/environment/track_segment.obj", v, f)

    # --- Props: Building (City) ---
    v, f = box(3.0, 8.0, 3.0, ox=0, oy=4.0)
    save_obj("models/props/city_building.obj", v, f)

    # --- Props: Tree (Jungle) ---
    # Trunk
    v, f = cylinder(0.2, 3.0, 8)
    # Canopy (approximated as box)
    v2, f2 = box(2.0, 1.5, 2.0, oy=3.75)
    offset = len(v)
    v.extend(v2)
    f.extend([(a + offset, b + offset, c + offset, d + offset) for a, b, c, d in f2])
    save_obj("models/props/jungle_tree.obj", v, f)

    # --- Props: Candy cane ---
    v, f = cylinder(0.15, 2.5, 8, oy=0)
    save_obj("models/props/candy_cane.obj", v, f)

    # --- Player placeholder (capsule-like) ---
    # Body
    v, f = cylinder(0.3, 1.2, 12, oy=0.3)
    # Head
    v2, f2 = cylinder(0.2, 0.4, 12, oy=1.5)
    offset = len(v)
    v.extend(v2)
    f.extend(tuple(idx + offset for idx in face) for face in f2)
    save_obj("models/player/emersyn_placeholder.obj", v, f)

    asset_volume.commit()
    return results


# =============================================================================
# MASTER ORCHESTRATOR
# =============================================================================

@app.function(
    image=audio_image,
    timeout=60,
)
def get_texture_prompts():
    """Return all texture generation prompts."""
    style_base = "game texture, stylized cartoon, vibrant colors, hand-painted look, mobile game art style"

    prompts = [
        # Character textures
        {
            "prompt": f"character texture sheet for a cute stylized cartoon kid runner named Emersyn, "
                      f"bright colorful outfit with sneakers, happy expression, {style_base}",
            "filename": "textures/player/emersyn_diffuse.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"normal map texture for cartoon character clothing, fabric wrinkles and folds, "
                      f"purple and blue tones, seamless, {style_base}",
            "filename": "textures/player/emersyn_normal.png",
            "width": 1024,
            "height": 1024,
        },

        # City biome textures
        {
            "prompt": f"seamless asphalt road texture with lane markings, top down view, urban street, {style_base}",
            "filename": "textures/environment/city_ground.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"seamless concrete building wall texture, stylized urban, windows and bricks, {style_base}",
            "filename": "textures/environment/city_wall.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"seamless metal railing texture, urban guardrail, side view, {style_base}",
            "filename": "textures/environment/city_rail.png",
            "width": 512,
            "height": 512,
        },

        # Jungle biome textures
        {
            "prompt": f"seamless jungle dirt path texture with moss, top down view, tropical, {style_base}",
            "filename": "textures/environment/jungle_ground.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"seamless tropical jungle vegetation wall, dense leaves and vines, {style_base}",
            "filename": "textures/environment/jungle_wall.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"seamless ancient stone temple texture, mossy ruins, carved stone, {style_base}",
            "filename": "textures/environment/jungle_ruins.png",
            "width": 1024,
            "height": 1024,
        },

        # Candy biome textures
        {
            "prompt": f"seamless candy road texture, colorful swirls, lollipop patterns, sugar coating, {style_base}",
            "filename": "textures/environment/candy_ground.png",
            "width": 1024,
            "height": 1024,
        },
        {
            "prompt": f"seamless candy wall texture, wafer cookies, chocolate, sprinkles pattern, {style_base}",
            "filename": "textures/environment/candy_wall.png",
            "width": 1024,
            "height": 1024,
        },

        # Obstacle textures
        {
            "prompt": f"metal barrier texture, construction barricade, orange and white stripes, {style_base}",
            "filename": "textures/obstacles/barrier.png",
            "width": 512,
            "height": 512,
        },
        {
            "prompt": f"wooden crate texture, stylized cartoon, nailed planks, {style_base}",
            "filename": "textures/obstacles/crate.png",
            "width": 512,
            "height": 512,
        },
        {
            "prompt": f"train car side texture, colorful graffiti, subway train, cartoon, {style_base}",
            "filename": "textures/obstacles/train.png",
            "width": 1024,
            "height": 512,
        },

        # Pickup textures
        {
            "prompt": f"gold coin texture, shiny, game coin icon, front face, circular, {style_base}",
            "filename": "textures/pickups/coin.png",
            "width": 256,
            "height": 256,
        },
        {
            "prompt": f"magnet powerup icon, red and silver horseshoe magnet, game item, {style_base}",
            "filename": "textures/pickups/magnet.png",
            "width": 256,
            "height": 256,
        },
        {
            "prompt": f"2x multiplier powerup icon, golden star with number 2, game item, {style_base}",
            "filename": "textures/pickups/multiplier.png",
            "width": 256,
            "height": 256,
        },
        {
            "prompt": f"energy shield powerup icon, blue glowing bubble, force field, game item, {style_base}",
            "filename": "textures/pickups/shield.png",
            "width": 256,
            "height": 256,
        },

        # Skybox textures
        {
            "prompt": f"panoramic city skyline, blue sky with clouds, stylized urban buildings horizon, game background, {style_base}",
            "filename": "textures/skybox/city_skybox.png",
            "width": 2048,
            "height": 1024,
        },
        {
            "prompt": f"panoramic tropical jungle canopy, green trees, exotic sky, game background, {style_base}",
            "filename": "textures/skybox/jungle_skybox.png",
            "width": 2048,
            "height": 1024,
        },
        {
            "prompt": f"panoramic candy land horizon, colorful clouds, cotton candy sky, rainbow, game background, {style_base}",
            "filename": "textures/skybox/candy_skybox.png",
            "width": 2048,
            "height": 1024,
        },

        # UI textures
        {
            "prompt": f"game title logo 'Emersyn Runner' text, stylized cartoon font, colorful letters, game logo, transparent background",
            "filename": "textures/ui/title_logo.png",
            "width": 1024,
            "height": 512,
        },
        {
            "prompt": f"game UI button, rounded rectangle, gradient blue to purple, shiny, cartoon game button, {style_base}",
            "filename": "textures/ui/button_primary.png",
            "width": 512,
            "height": 128,
        },
        {
            "prompt": f"game pause icon, two vertical bars, white on dark circle, UI icon, {style_base}",
            "filename": "textures/ui/pause_icon.png",
            "width": 128,
            "height": 128,
        },
        {
            "prompt": f"game heart icon, red heart, UI health icon, cartoon, {style_base}",
            "filename": "textures/ui/heart_icon.png",
            "width": 128,
            "height": 128,
        },
        {
            "prompt": f"game background pattern, subtle diagonal stripes, dark blue gradient, menu background, {style_base}",
            "filename": "textures/ui/menu_background.png",
            "width": 1024,
            "height": 2048,
        },
    ]
    return prompts


@app.local_entrypoint()
def main():
    """Main entrypoint: generate all assets."""
    print("=" * 60)
    print("EMERSYN RUNNER - ASSET GENERATION PIPELINE")
    print("=" * 60)

    # Step 1: Get texture prompts
    print("\n[1/3] Preparing texture prompts...")
    prompts = get_texture_prompts.remote()
    print(f"  {len(prompts)} textures to generate")

    # Step 2: Generate textures in batches (batch by GPU session)
    print("\n[2/3] Generating textures via SDXL on GPU...")
    batch_size = 6
    all_results = {}

    for i in range(0, len(prompts), batch_size):
        batch = prompts[i:i + batch_size]
        print(f"  Batch {i // batch_size + 1}/{(len(prompts) + batch_size - 1) // batch_size} ({len(batch)} textures)...")
        batch_results = generate_texture_batch.remote(batch)
        all_results.update(batch_results)

    print(f"  Generated {len(all_results)} textures")

    # Step 3: Generate audio
    print("\n[3/3] Generating audio assets...")
    audio_results = generate_audio_assets.remote()
    print(f"  Generated {len(audio_results)} audio files")

    # Step 4: Generate 3D models
    print("\n[4/4] Generating 3D models...")
    model_results = generate_3d_models.remote()
    print(f"  Generated {len(model_results)} models")

    print("\n" + "=" * 60)
    print("ASSET GENERATION COMPLETE")
    print(f"Total assets: {len(all_results) + len(audio_results) + len(model_results)}")
    print("Assets stored in Modal volume: emersyn-runner-assets")
    print("=" * 60)
