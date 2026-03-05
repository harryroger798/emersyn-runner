#!/usr/bin/env python3
"""
modal_phase3_assets.py - Phase 3: Core Visual Leap
Generates higher-quality textures for the AAA upgrade:
- 1024px building textures with baked detail
- Character portrait illustrations for selection screen
- UI gradient backgrounds and styled buttons
- Normal map textures for depth/lighting
- Better road texture with integrated lane markings
- Enhanced VFX sprite sheets
Budget target: ~$15 on A10G
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase3")

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

asset_volume = modal.Volume.from_name("emersyn-phase3-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=3600,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_phase3_textures():
    """Generate all Phase 3 textures in one GPU session."""
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

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence, ugly, deformed, realistic, 3d render"
    style = "stylized cartoon mobile game art, vibrant saturated colors, hand-painted look, clean lines, subway surfers art style"

    textures = [
        # === HIGH-RES BUILDING TEXTURES (1024px for closer viewing) ===
        {"name": "tex_building_highrise_1", "prompt": f"seamless tileable modern city highrise building facade, blue glass windows with white concrete floors, balconies, detailed architectural elements, {style}", "size": 1024},
        {"name": "tex_building_highrise_2", "prompt": f"seamless tileable luxury apartment building facade, warm stone walls with ornate windows and flower boxes, golden trim, {style}", "size": 1024},
        {"name": "tex_building_industrial", "prompt": f"seamless tileable industrial warehouse building facade, corrugated metal walls with loading dock doors and pipes, {style}", "size": 1024},
        {"name": "tex_building_restaurant", "prompt": f"seamless tileable restaurant building facade, brick wall with large window and awning, neon OPEN sign, warm inviting, {style}", "size": 1024},
        {"name": "tex_building_arcade", "prompt": f"seamless tileable arcade game center building facade, neon lights and colorful signage, retro gaming feel, {style}", "size": 1024},

        # === ENHANCED ROAD TEXTURE (with baked lane markings) ===
        {"name": "tex_road_hd", "prompt": f"top down view seamless tileable asphalt road texture with white dashed lane markings and yellow edge lines, urban road, clean, {style}", "size": 1024},
        {"name": "tex_sidewalk_hd", "prompt": f"top down view seamless tileable concrete sidewalk pavement texture, light grey with subtle cracks, clean urban, {style}", "size": 512},

        # === CHARACTER PORTRAITS (for selection screen) ===
        {"name": "tex_portrait_classic", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing blue hoodie with red sneakers, confident pose, white circular background, mobile game character art, {style}", "size": 512},
        {"name": "tex_portrait_red_hoodie", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing red hoodie with gold sneakers, energetic pose, white circular background, mobile game character art, {style}", "size": 512},
        {"name": "tex_portrait_green_jacket", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing green zip jacket with neon sneakers, athletic pose, white circular background, mobile game character art, {style}", "size": 512},
        {"name": "tex_portrait_orange_vest", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing orange safety vest, adventurous pose, white circular background, mobile game character art, {style}", "size": 512},
        {"name": "tex_portrait_purple", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing purple sweater with gold sneakers, cool pose, white circular background, mobile game character art, {style}", "size": 512},
        {"name": "tex_portrait_pink", "prompt": f"character portrait of a cool stylized cartoon kid runner wearing pink t-shirt with neon sneakers, happy pose, white circular background, mobile game character art, {style}", "size": 512},

        # === UI KIT ===
        {"name": "tex_ui_bg_gradient_purple", "prompt": f"smooth purple pink radial gradient background with subtle light rays and sparkles, game menu background, {style}", "size": 1024},
        {"name": "tex_ui_bg_gradient_blue", "prompt": f"smooth blue cyan radial gradient background with bokeh light circles, futuristic game UI background, {style}", "size": 1024},
        {"name": "tex_ui_bg_city_blur", "prompt": f"blurred colorful city street at sunset with bokeh lights, warm orange glow, game menu background, {style}", "size": 1024},
        {"name": "tex_ui_btn_play", "prompt": f"green glossy 3D button with PLAY text, rounded rectangle, game UI element, drop shadow, {style}", "size": 256},
        {"name": "tex_ui_btn_retry", "prompt": f"orange glossy 3D button with circular arrow retry icon, rounded rectangle, game UI element, {style}", "size": 256},
        {"name": "tex_ui_panel_dark", "prompt": f"dark semi-transparent rounded rectangle panel with subtle glow border, game UI overlay, {style}", "size": 512},
        {"name": "tex_ui_title_banner", "prompt": f"golden metallic 3D banner header decoration, ornate scroll design with sparkles, game UI element, {style}", "size": 512},

        # === HOVERBOARD DETAILED TEXTURES ===
        {"name": "tex_board_fire", "prompt": f"skateboard deck top view with red orange fire flames design, aggressive energy, {style}", "size": 512},
        {"name": "tex_board_ocean", "prompt": f"skateboard deck top view with ocean wave water design, tropical blue green, {style}", "size": 512},
        {"name": "tex_board_neon_city", "prompt": f"skateboard deck top view with neon city skyline silhouette design, cyberpunk purple blue, {style}", "size": 512},

        # === ENHANCED VFX SPRITES (larger, more detailed) ===
        {"name": "tex_vfx_coin_collect", "prompt": f"golden coin collection sparkle burst effect sprite sheet, 4 frames in grid, bright magical glow, transparent look, {style}", "size": 512},
        {"name": "tex_vfx_jump_ring", "prompt": f"circular shockwave ring effect sprite, blue energy expanding ring, impact effect, {style}", "size": 256},
        {"name": "tex_vfx_magnet_pull", "prompt": f"blue magnetic field lines pulling effect sprite, glowing energy lines converging, {style}", "size": 256},
        {"name": "tex_vfx_trail_fire", "prompt": f"horizontal fire trail effect sprite, orange red flames streaming backward, speed boost, {style}", "size": 512},
        {"name": "tex_vfx_trail_rainbow", "prompt": f"horizontal rainbow color trail effect sprite, flowing prismatic colors streaming backward, {style}", "size": 512},
        {"name": "tex_vfx_stumble_stars", "prompt": f"cartoon yellow stars circling dizziness effect sprite, stun effect, comic style, {style}", "size": 256},

        # === ENVIRONMENT DETAIL TEXTURES ===
        {"name": "tex_env_billboard_1", "prompt": f"colorful city billboard advertisement for fictional soda drink, bright pop art style, {style}", "size": 512},
        {"name": "tex_env_billboard_2", "prompt": f"colorful city billboard advertisement for fictional sneaker brand, dynamic action pose, {style}", "size": 512},
        {"name": "tex_env_rooftop", "prompt": f"seamless tileable flat rooftop texture with vents and satellite dishes, top down view, urban, {style}", "size": 512},
        {"name": "tex_env_tunnel_interior", "prompt": f"seamless tileable tunnel interior wall texture, concrete with fluorescent light strips, underground subway, {style}", "size": 512},

        # === POWERUP TEXTURES (detailed 3D look) ===
        {"name": "tex_powerup_jetpack", "prompt": f"cartoon jetpack item icon, silver metallic with blue flame exhaust, game collectible, {style}", "size": 256},
        {"name": "tex_powerup_super_sneakers", "prompt": f"cartoon spring-loaded super sneakers boots icon, glowing green springs, game collectible, {style}", "size": 256},
        {"name": "tex_powerup_coin_magnet", "prompt": f"cartoon horseshoe magnet with golden coins attracted to it icon, red magnet with gold, game collectible, {style}", "size": 256},
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
    print("EMERSYN RUNNER PHASE 3 - CORE VISUAL LEAP")
    print(f"Generating {35} high-quality textures")
    print("=" * 60)

    start = time.time()
    results = generate_phase3_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase3-assets textures/ --output /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
