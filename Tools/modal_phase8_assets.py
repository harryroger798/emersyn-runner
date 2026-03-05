#!/usr/bin/env python3
"""
modal_phase8_assets.py - Phase 8: Major Visual Upgrade via Modal GPU
Generates 20+ high-quality SDXL textures targeting specific visual gaps:
- Blue sky gradient (replacing warm yellow one)
- Road edge/curb textures (replacing thick yellow lines)
- Better sidewalk and ground textures
- Improved obstacle detail textures
- Enhanced UI panel/button backgrounds
- Better character outfit textures
Budget target: ~$2-5 on A10G
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase8")

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

asset_volume = modal.Volume.from_name("emersyn-phase8-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=3600,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_phase8_textures():
    """Generate Phase 8 visual upgrade textures."""
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

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence, ugly, deformed, realistic person"
    style = "stylized cartoon mobile game art, vibrant saturated colors, hand-painted look, clean lines, subway surfers inspired art style, bright cheerful"

    textures = [
        # === SKY - Replace warm yellow with proper blue ===
        {"name": "tex_sky_blue_gradient", "prompt": f"seamless bright blue sky gradient, light cyan at bottom transitioning to deeper sky blue at top, small white fluffy cartoon clouds scattered, sunny cheerful game background, {style}", "size": 1024},
        {"name": "tex_sky_dusk", "prompt": f"seamless dusk sky gradient, soft lavender at bottom through light purple to deep indigo at top, first stars appearing, game background, {style}", "size": 1024},

        # === ROAD - Better asphalt and edges ===
        {"name": "tex_road_asphalt_hd", "prompt": f"seamless tileable dark grey asphalt road surface texture, subtle crack details, clean urban road, top down view, {style}", "size": 1024},
        {"name": "tex_road_curb", "prompt": f"seamless tileable concrete curb edge texture, grey stone curb with subtle weathering, side view, urban road edge, {style}", "size": 512},
        {"name": "tex_road_crosswalk_hd", "prompt": f"seamless white crosswalk zebra stripes on dark asphalt road, clean crisp lines, top down view, urban, {style}", "size": 512},

        # === SIDEWALK - Replace flat grey ===
        {"name": "tex_sidewalk_stone", "prompt": f"seamless tileable stone paving sidewalk texture, grey rectangular tiles with subtle gaps, urban city walkway, top down view, {style}", "size": 512},

        # === OBSTACLE DETAIL TEXTURES ===
        {"name": "tex_obstacle_barrier_hd", "prompt": f"red and white striped construction barrier, reflective warning stripes, front view, urban obstacle, {style}", "size": 512},
        {"name": "tex_obstacle_car_side", "prompt": f"colorful cartoon taxi cab side view, yellow body with checkered pattern, wheels visible, mobile game vehicle, {style}", "size": 512},
        {"name": "tex_obstacle_bus_side", "prompt": f"colorful cartoon city bus side view, blue and white with route number, windows with passengers, mobile game vehicle, {style}", "size": 512},
        {"name": "tex_obstacle_dumpster", "prompt": f"green metal dumpster front view, industrial waste bin with handles, urban prop, {style}", "size": 256},
        {"name": "tex_obstacle_cone_hd", "prompt": f"orange traffic cone with white reflective stripes, front view, road safety equipment, {style}", "size": 256},

        # === TRAIN TEXTURE - Better detail ===
        {"name": "tex_train_side_hd", "prompt": f"colorful subway train car side view, silver metallic body with blue stripe, windows with passengers silhouettes, graffiti art, mobile game, {style}", "size": 1024},

        # === UI PANEL BACKGROUNDS ===
        {"name": "tex_ui_panel_game", "prompt": f"semi-transparent dark blue game UI panel background with subtle glow border, rounded corners, sleek modern mobile game HUD, {style}", "size": 256},
        {"name": "tex_ui_panel_menu", "prompt": f"vibrant gradient purple to pink game menu background panel, subtle sparkle effects, mobile game UI, {style}", "size": 512},
        {"name": "tex_ui_button_green", "prompt": f"glossy green button with bright highlight on top edge, rounded rectangle, mobile game play button, {style}", "size": 256},
        {"name": "tex_ui_button_orange", "prompt": f"glossy orange button with bright highlight on top edge, rounded rectangle, mobile game retry button, {style}", "size": 256},

        # === POWERUP GLOW EFFECTS ===
        {"name": "tex_powerup_magnet_glow", "prompt": f"purple magnetic energy glow effect, radial burst with electric sparks, game VFX sprite on black background, {style}", "size": 256},
        {"name": "tex_powerup_shield_glow", "prompt": f"blue shield force field bubble effect, translucent protective dome with hexagonal pattern, game VFX on black background, {style}", "size": 256},

        # === ENVIRONMENT PROPS ===
        {"name": "tex_prop_mailbox", "prompt": f"blue US postal mailbox front view, cartoon style, urban street prop, {style}", "size": 256},
        {"name": "tex_prop_bench", "prompt": f"wooden park bench front view, green metal frame with wooden slats, cartoon style park furniture, {style}", "size": 256},
        {"name": "tex_prop_tree", "prompt": f"green cartoon tree with round fluffy canopy, brown trunk, front view, mobile game environment prop, {style}", "size": 512},
    ]

    results = {}
    total = len(textures)
    for i, tex in enumerate(textures):
        name = tex["name"]
        prompt = tex["prompt"]
        size = tex.get("size", 512)

        print(f"[{i+1}/{total}] Generating: {name} ({size}x{size})")

        seed = int(hashlib.md5(name.encode()).hexdigest()[:8], 16)
        generator = torch.Generator("cuda").manual_seed(seed)

        image = pipe(
            prompt=prompt,
            negative_prompt=default_negative,
            width=size,
            height=size,
            num_inference_steps=30,
            guidance_scale=8.0,
            generator=generator,
        ).images[0]

        output_path = os.path.join(ASSET_MOUNT, f"textures/{name}.png")
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        image.save(output_path, "PNG")

        buf = io.BytesIO()
        image.save(buf, format="PNG")
        results[name] = len(buf.getvalue())
        print(f"  Saved: {name}.png ({len(buf.getvalue())} bytes)")

    asset_volume.commit()
    print(f"\nGenerated {len(results)} textures total")
    return results


@app.local_entrypoint()
def main():
    import time

    print("=" * 60)
    print("EMERSYN RUNNER PHASE 8 - MAJOR VISUAL UPGRADE")
    print(f"Generating 21 high-quality SDXL textures")
    print("=" * 60)

    start = time.time()
    results = generate_phase8_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase8-assets textures/ --output /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
