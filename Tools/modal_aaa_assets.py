#!/usr/bin/env python3
"""
modal_aaa_assets.py - Generate AAA-quality game assets for Emersyn Runner
Uses SDXL on Modal GPU for high-quality tileable textures + improved audio.
Assets are saved to Assets/Resources/ for runtime loading in Unity.
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-runner-aaa")

# GPU image with SDXL
gpu_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install(
        "torch>=2.1.0",
        "torchvision",
        "diffusers>=0.25.0",
        "transformers>=4.36.0",
        "accelerate>=0.25.0",
        "safetensors",
        "Pillow>=10.0",
        "numpy",
        "xformers",
    )
)

# Audio image
audio_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install("numpy", "scipy", "Pillow")
)

asset_volume = modal.Volume.from_name("emersyn-aaa-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=1800,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_all_textures():
    """Generate all game textures in one GPU session for efficiency."""
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

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence, ugly, deformed"

    textures = [
        # === ENVIRONMENT ===
        {
            "name": "tex_road_asphalt",
            "prompt": "seamless tileable dark grey asphalt road texture, game art style, top down view, clean surface with subtle cracks, stylized mobile game, subway surfers style",
            "size": 512,
        },
        {
            "name": "tex_road_sidewalk",
            "prompt": "seamless tileable light grey concrete sidewalk texture, game art style, clean paving stones pattern, stylized mobile game",
            "size": 512,
        },
        {
            "name": "tex_building_red",
            "prompt": "seamless tileable red brick building facade texture, stylized game art, windows and details, colorful cartoon style mobile game",
            "size": 512,
        },
        {
            "name": "tex_building_blue",
            "prompt": "seamless tileable blue painted building facade texture, stylized game art, windows with shutters, colorful cartoon style mobile game",
            "size": 512,
        },
        {
            "name": "tex_building_yellow",
            "prompt": "seamless tileable yellow stucco building facade texture, stylized game art, windows and balconies, colorful cartoon mobile game",
            "size": 512,
        },
        {
            "name": "tex_building_grey",
            "prompt": "seamless tileable modern grey concrete building facade texture, stylized game art, glass windows, sleek mobile game style",
            "size": 512,
        },
        {
            "name": "tex_building_pink",
            "prompt": "seamless tileable pink pastel building facade texture, stylized game art, cute windows and flower boxes, colorful mobile game",
            "size": 512,
        },
        # === GROUND / TRACK ===
        {
            "name": "tex_grass",
            "prompt": "seamless tileable bright green grass texture, stylized game art, cartoon style mobile game, lush vibrant",
            "size": 512,
        },
        {
            "name": "tex_train_tracks",
            "prompt": "seamless tileable train railroad tracks metal rails on wooden sleepers, top down view, stylized game art",
            "size": 512,
        },
        # === OBSTACLES ===
        {
            "name": "tex_barrier_red",
            "prompt": "red and white striped barrier obstacle texture, stylized game art, warning stripes, mobile game subway surfers style, flat colors",
            "size": 256,
        },
        {
            "name": "tex_train_side",
            "prompt": "colorful subway train side panel texture, stylized game art, graffiti art spray paint tags, cartoon mobile game style, vibrant",
            "size": 512,
        },
        {
            "name": "tex_cone_orange",
            "prompt": "orange traffic cone texture with white reflective stripes, stylized game art, flat cartoon mobile game style",
            "size": 256,
        },
        # === CHARACTER ===
        {
            "name": "tex_emersyn_shirt",
            "prompt": "bright blue hoodie sweatshirt fabric texture, stylized game art, simple clean color with subtle wrinkle detail, cartoon mobile game",
            "size": 256,
        },
        {
            "name": "tex_emersyn_pants",
            "prompt": "dark navy blue denim jeans fabric texture, stylized game art, simple clean texture, cartoon mobile game",
            "size": 256,
        },
        {
            "name": "tex_emersyn_shoes",
            "prompt": "bright red sneaker shoe texture, stylized game art, white sole details, cartoon mobile game style",
            "size": 256,
        },
        {
            "name": "tex_emersyn_skin",
            "prompt": "smooth warm beige skin texture, stylized game art, soft cartoon shading, mobile game character",
            "size": 256,
        },
        {
            "name": "tex_emersyn_hair",
            "prompt": "warm brown wavy hair texture, stylized game art, cartoon style highlights and shadows, mobile game character",
            "size": 256,
        },
        # === PICKUPS ===
        {
            "name": "tex_coin_gold",
            "prompt": "shiny gold coin texture with star emblem center, stylized game art, metallic reflective, mobile game collectible",
            "size": 256,
        },
        {
            "name": "tex_powerup_magnet",
            "prompt": "glowing blue magnet powerup icon texture, stylized game art, energy aura, mobile game pickup item",
            "size": 256,
        },
        {
            "name": "tex_powerup_shield",
            "prompt": "glowing green shield bubble texture, stylized game art, magical energy barrier, mobile game powerup",
            "size": 256,
        },
        {
            "name": "tex_powerup_multiplier",
            "prompt": "glowing purple 2x multiplier star texture, stylized game art, sparkling effects, mobile game powerup",
            "size": 256,
        },
        # === SKY / ATMOSPHERE ===
        {
            "name": "tex_sky_gradient",
            "prompt": "beautiful blue sky gradient with fluffy white clouds, stylized game art, vibrant colors, mobile game background, subway surfers style sky",
            "size": 512,
        },
        # === UI ===
        {
            "name": "tex_ui_panel",
            "prompt": "dark blue rounded rectangle UI panel with gold border, stylized game art, clean mobile game interface, semi transparent",
            "size": 256,
        },
        {
            "name": "tex_ui_button_green",
            "prompt": "bright green glossy button texture with subtle gradient, stylized game art, rounded rectangle, mobile game UI, play button",
            "size": 256,
        },
        {
            "name": "tex_ui_button_red",
            "prompt": "bright red glossy button texture with subtle gradient, stylized game art, rounded rectangle, mobile game UI",
            "size": 256,
        },
        # === PROPS ===
        {
            "name": "tex_streetlamp",
            "prompt": "dark metal street lamp pole texture, stylized game art, simple clean, mobile game prop",
            "size": 256,
        },
        {
            "name": "tex_fence_metal",
            "prompt": "chain link metal fence texture, stylized game art, seamless tileable, simple cartoon style",
            "size": 256,
        },
        {
            "name": "tex_graffiti_wall",
            "prompt": "colorful graffiti wall art texture, stylized game art, spray paint tags and characters, vibrant subway surfers style",
            "size": 512,
        },
        {
            "name": "tex_shop_sign",
            "prompt": "colorful neon shop sign texture, stylized game art, glowing letters and symbols, vibrant mobile game city",
            "size": 256,
        },
    ]

    results = {}
    for i, tex in enumerate(textures):
        name = tex["name"]
        prompt = tex["prompt"]
        size = tex.get("size", 512)

        print(f"[{i+1}/{len(textures)}] Generating: {name}")

        seed = int(hashlib.md5(name.encode()).hexdigest()[:8], 16)
        generator = torch.Generator("cuda").manual_seed(seed)

        image = pipe(
            prompt=prompt,
            negative_prompt=default_negative,
            width=size,
            height=size,
            num_inference_steps=25,
            guidance_scale=7.5,
            generator=generator,
        ).images[0]

        output_path = os.path.join(ASSET_MOUNT, f"textures/{name}.png")
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        image.save(output_path, "PNG")

        buf = io.BytesIO()
        image.save(buf, format="PNG")
        results[name] = len(buf.getvalue())
        print(f"  Saved: {name}.png ({size}x{size}, {len(buf.getvalue())} bytes)")

    asset_volume.commit()
    return results


@app.function(
    image=audio_image,
    timeout=300,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_improved_audio():
    """Generate higher-quality audio: music with richer harmony + more SFX."""
    import numpy as np
    from scipy.io import wavfile
    from scipy.signal import butter, filtfilt

    sample_rate = 44100
    results = {}

    def save_wav(filename, data, sr=sample_rate):
        if data.dtype == np.float64 or data.dtype == np.float32:
            data = np.clip(data, -1.0, 1.0)
            data = (data * 32767).astype(np.int16)
        output_path = os.path.join(ASSET_MOUNT, filename)
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        wavfile.write(output_path, sr, data)
        results[filename] = True

    def sine(freq, dur, vol=0.5):
        t = np.linspace(0, dur, int(sample_rate * dur), endpoint=False)
        return vol * np.sin(2 * np.pi * freq * t)

    def saw(freq, dur, vol=0.3):
        t = np.linspace(0, dur, int(sample_rate * dur), endpoint=False)
        return vol * (2 * (freq * t % 1) - 1)

    def noise(dur, vol=0.1):
        return vol * np.random.randn(int(sample_rate * dur))

    def env(signal, a=0.01, d=0.05, s=0.7, r=0.1):
        n = len(signal)
        e = np.ones(n)
        ai, di, ri = int(a * sample_rate), int(d * sample_rate), int(r * sample_rate)
        if ai > 0: e[:ai] = np.linspace(0, 1, ai)
        if di > 0 and ai + di < n: e[ai:ai+di] = np.linspace(1, s, di)
        if ai + di < n - ri: e[ai+di:n-ri] = s
        if ri > 0: e[n-ri:] = np.linspace(s, 0, ri)
        return signal * e

    def lowpass(sig, cutoff=0.1):
        b, a = butter(4, cutoff, btype='low')
        return filtfilt(b, a, sig)

    def reverb(sig, decay=0.3, delay_ms=50):
        delay_samples = int(sample_rate * delay_ms / 1000)
        out = sig.copy()
        for i in range(1, 4):
            offset = delay_samples * i
            gain = decay ** i
            if offset < len(out):
                end = min(len(out), len(sig) + offset)
                out[offset:end] += sig[:end-offset] * gain
        return out

    # === MENU MUSIC (16 bars, 110 BPM, warm & inviting) ===
    bpm = 110
    beat = 60.0 / bpm
    bars = 16
    duration = beat * 4 * bars

    # Chord progression: C-Am-F-G (I-vi-IV-V)
    chords = [
        (261.6, 329.6, 392.0),  # C major
        (220.0, 261.6, 329.6),  # A minor
        (174.6, 220.0, 261.6),  # F major
        (196.0, 246.9, 293.7),  # G major
    ]

    menu = np.zeros(int(sample_rate * duration))
    # Pad chords
    for bar in range(bars):
        chord_idx = bar % 4
        c = chords[chord_idx]
        start = int(bar * beat * 4 * sample_rate)
        for freq in c:
            pad_note = env(sine(freq, beat * 4, vol=0.08), a=0.2, s=0.6, r=0.3)
            end = min(start + len(pad_note), len(menu))
            menu[start:end] += pad_note[:end-start]

    # Melody (pentatonic, catchy)
    melody_freqs = [523, 587, 659, 784, 880, 784, 659, 587, 523, 659, 784, 880, 1047, 880, 784, 659]
    mel_dur = beat * 0.75
    for bar in range(bars):
        for note_i in range(4):
            freq = melody_freqs[(bar * 4 + note_i) % len(melody_freqs)]
            start = int((bar * 4 + note_i) * beat * sample_rate)
            note = env(sine(freq, mel_dur, vol=0.2), a=0.01, r=0.1)
            end = min(start + len(note), len(menu))
            menu[start:end] += note[:end-start]

    # Bass
    bass_freqs = [131, 110, 87, 98]
    for bar in range(bars):
        freq = bass_freqs[bar % 4]
        start = int(bar * beat * 4 * sample_rate)
        note = env(sine(freq, beat * 3.5, vol=0.25), a=0.01, r=0.2)
        end = min(start + len(note), len(menu))
        menu[start:end] += note[:end-start]

    menu = reverb(np.clip(menu, -0.9, 0.9), decay=0.2)
    save_wav("audio/menu_loop.wav", menu)

    # === GAMEPLAY MUSIC (16 bars, 140 BPM, driving & energetic) ===
    bpm2 = 140
    beat2 = 60.0 / bpm2
    dur2 = beat2 * 4 * 16

    game = np.zeros(int(sample_rate * dur2))

    # Driving bass (synth)
    bass_pattern = [65, 65, 87, 65, 98, 65, 87, 98] * 8
    for i, freq in enumerate(bass_pattern):
        start = int(i * beat2 * sample_rate / 2)
        note = env(saw(freq, beat2 * 0.35, vol=0.35), a=0.005, r=0.02)
        end = min(start + len(note), len(game))
        game[start:end] += note[:end-start]

    # Kick drum pattern
    for i in range(int(dur2 / beat2)):
        start = int(i * beat2 * sample_rate)
        t_k = np.linspace(0, 0.1, int(sample_rate * 0.1))
        kick = 0.4 * np.sin(2 * np.pi * (150 - 100 * t_k / 0.1) * t_k) * np.exp(-t_k * 30)
        end = min(start + len(kick), len(game))
        game[start:end] += kick[:end-start]

    # Hi-hat
    for i in range(int(dur2 / (beat2 / 2))):
        start = int(i * beat2 * sample_rate / 2)
        hit = env(noise(0.02, vol=0.12), a=0.001, r=0.01)
        end = min(start + len(hit), len(game))
        game[start:end] += hit[:end-start]

    # Synth lead melody
    lead_notes = [392, 440, 523, 587, 659, 523, 440, 392, 349, 392, 440, 523, 659, 784, 659, 523] * 4
    for i, freq in enumerate(lead_notes):
        start = int(i * beat2 * sample_rate / 2)
        note = env(sine(freq, beat2 * 0.3, vol=0.15), a=0.005, r=0.03)
        end = min(start + len(note), len(game))
        game[start:end] += note[:end-start]

    game = np.clip(game, -0.9, 0.9)
    save_wav("audio/gameplay_loop.wav", game)

    # === SFX ===
    # Whoosh (smooth lane change)
    t = np.linspace(0, 0.25, int(sample_rate * 0.25))
    whoosh = noise(0.25, vol=0.35) * np.exp(-t * 8) * np.sin(np.pi * t / 0.25)
    whoosh = lowpass(whoosh, 0.15)
    save_wav("audio/sfx_whoosh.wav", whoosh)

    # Jump (ascending pitch)
    t = np.linspace(0, 0.3, int(sample_rate * 0.3))
    jump = 0.45 * np.sin(2 * np.pi * (400 + 800 * t / 0.3) * t) * np.exp(-t * 6)
    save_wav("audio/sfx_jump.wav", jump)

    # Roll
    roll = env(lowpass(noise(0.35, vol=0.25), 0.08), a=0.05, s=0.7, r=0.1)
    save_wav("audio/sfx_roll.wav", roll)

    # Coin (bright ding)
    t = np.linspace(0, 0.15, int(sample_rate * 0.15))
    coin = 0.35 * np.sin(2 * np.pi * 1200 * t) * np.exp(-t * 25) + 0.25 * np.sin(2 * np.pi * 2400 * t) * np.exp(-t * 30)
    save_wav("audio/sfx_coin.wav", coin)

    # Powerup
    t = np.linspace(0, 0.5, int(sample_rate * 0.5))
    pu = 0.3 * np.sin(2 * np.pi * (300 + 700 * t / 0.5) * t) * np.exp(-t * 4)
    pu += 0.2 * np.sin(2 * np.pi * (600 + 400 * t / 0.5) * t) * np.exp(-t * 5)
    save_wav("audio/sfx_powerup.wav", pu)

    # Shield
    t = np.linspace(0, 0.6, int(sample_rate * 0.6))
    shield = 0.25 * np.sin(2 * np.pi * 200 * t) * np.exp(-t * 3)
    shield += 0.15 * np.sin(2 * np.pi * 400 * t) * np.exp(-t * 4)
    shield += lowpass(noise(0.6, vol=0.1) * np.exp(-t * 5), 0.05)
    save_wav("audio/sfx_shield.wav", shield)

    # Hit (impact)
    t = np.linspace(0, 0.4, int(sample_rate * 0.4))
    hit = noise(0.4, vol=0.45) * np.exp(-t * 10) + 0.35 * np.sin(2 * np.pi * 60 * t) * np.exp(-t * 8)
    save_wav("audio/sfx_hit.wav", hit)

    # Game over (descending notes)
    go = np.array([], dtype=np.float64)
    for freq, dur in [(523, 0.2), (440, 0.2), (349, 0.2), (262, 0.6)]:
        go = np.concatenate([go, env(sine(freq, dur, vol=0.35), a=0.01, r=0.08)])
    save_wav("audio/sfx_gameover.wav", go)

    # UI click
    t = np.linspace(0, 0.04, int(sample_rate * 0.04))
    click = 0.35 * np.sin(2 * np.pi * 900 * t) * np.exp(-t * 80)
    save_wav("audio/sfx_click.wav", click)

    # Combo stinger
    combo = np.array([], dtype=np.float64)
    for freq in [659, 784, 880, 1047]:
        combo = np.concatenate([combo, env(sine(freq, 0.08, vol=0.25), a=0.005, r=0.02)])
    save_wav("audio/sfx_combo.wav", combo)

    # Speed up whoosh
    t = np.linspace(0, 0.5, int(sample_rate * 0.5))
    speedup = lowpass(noise(0.5, vol=0.2) * np.linspace(0, 1, len(t)), 0.1)
    speedup += 0.15 * np.sin(2 * np.pi * (200 + 400 * t / 0.5) * t) * np.exp(-t * 3)
    save_wav("audio/sfx_speedup.wav", speedup)

    asset_volume.commit()
    return results


@app.local_entrypoint()
def main():
    import time

    print("=" * 60)
    print("EMERSYN RUNNER AAA ASSET GENERATION")
    print("=" * 60)

    # Phase 1: Generate textures
    print("\n[PHASE 1] Generating SDXL textures on A10G GPU...")
    start = time.time()
    tex_results = generate_all_textures.remote()
    tex_time = time.time() - start
    print(f"  Generated {len(tex_results)} textures in {tex_time:.1f}s")
    for name, size in tex_results.items():
        print(f"    {name}: {size} bytes")

    # Phase 2: Generate audio
    print("\n[PHASE 2] Generating improved audio...")
    start = time.time()
    audio_results = generate_improved_audio.remote()
    audio_time = time.time() - start
    print(f"  Generated {len(audio_results)} audio files in {audio_time:.1f}s")

    print("\n" + "=" * 60)
    print(f"TOTAL TIME: {tex_time + audio_time:.1f}s")
    print("=" * 60)

    # Download from volume
    print("\nDownloading assets from Modal volume...")
    local_base = "/home/ubuntu/repos/emersyn-runner/Assets/Resources"
    os.makedirs(f"{local_base}/Textures", exist_ok=True)
    os.makedirs(f"{local_base}/Audio", exist_ok=True)

    # We need to download files from the volume
    # Use modal volume get command
    print("\nAssets generated on Modal volume 'emersyn-aaa-assets'")
    print("Use 'modal volume get emersyn-aaa-assets' to download")
    print("Or access them from Modal functions")
