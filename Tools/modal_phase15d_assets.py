#!/usr/bin/env python3
"""
modal_phase15d_assets.py - Phase 15D: Higher-quality textures for Subway Surfers parity
Generates textures designed for FLAT surfaces (buildings, road, UI, sky) where SDXL works best.
Also generates improved VFX sprite sheets and UI elements.
Budget target: ~$3-5 of Modal credits (single A10G session, ~15 min)
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase15d")

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

asset_volume = modal.Volume.from_name("emersyn-phase15d", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=1800,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_phase15d_textures():
    """Generate Phase 15D textures - targeting biggest visual gaps vs Subway Surfers."""
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

    negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, ugly, deformed, realistic photograph"
    style = "stylized cartoon game art, mobile game, vibrant colors, clean design, subway surfers style"

    textures = [
        # === IMPROVED BUILDING FACADES (higher quality, more Subway Surfers style) ===
        {
            "name": "tex_building_neon_arcade",
            "prompt": f"colorful neon-lit arcade game center building facade, bright pink and blue neon signs, pixel art decorations, glass storefront, {style}, seamless tileable",
            "size": 1024,
        },
        {
            "name": "tex_building_sushi_bar",
            "prompt": f"Japanese sushi bar restaurant building facade, paper lanterns, wooden accents, red and white color scheme, {style}, seamless tileable",
            "size": 1024,
        },
        {
            "name": "tex_building_comic_store",
            "prompt": f"comic book store building facade, colorful superhero posters in windows, bright yellow and red awning, {style}, seamless tileable",
            "size": 1024,
        },
        {
            "name": "tex_building_ice_cream",
            "prompt": f"ice cream parlor building facade, pastel pink and mint green, waffle cone decorations, striped awning, {style}, seamless tileable",
            "size": 1024,
        },
        {
            "name": "tex_building_skate_shop",
            "prompt": f"skateboard shop building facade, graffiti art, urban street style, cool blue and orange color scheme, {style}, seamless tileable",
            "size": 1024,
        },

        # === IMPROVED GROUND TEXTURES ===
        {
            "name": "tex_road_subway_style",
            "prompt": f"dark grey asphalt road surface, smooth clean texture, subtle aggregate grain, top-down view, no markings, {style}, seamless tileable",
            "size": 1024,
            "negative_extra": "markings, lines, paint, yellow, orange, white lines, cracks, arrows"
        },
        {
            "name": "tex_sidewalk_subway_style",
            "prompt": f"clean light grey concrete sidewalk pavement, rectangular paving tiles, subtle grid pattern, top-down view, {style}, seamless tileable",
            "size": 512,
            "negative_extra": "grass, dirt, cracks, yellow, colored"
        },

        # === BETTER SKY ===
        {
            "name": "tex_sky_subway_blue",
            "prompt": f"beautiful bright blue sky with fluffy white cartoon clouds, vibrant blue gradient, mobile game sky background, {style}",
            "size": 1024,
            "negative_extra": "sunset, orange, dark, night, stars"
        },

        # === ENHANCED VFX SPRITES (on black bg for alpha extraction) ===
        {
            "name": "tex_vfx_coin_burst_hd",
            "prompt": "golden sparkle burst explosion VFX sprite, bright gold and yellow particles radiating outward, high contrast, centered, on pure black background, game particle effect",
            "size": 512,
            "negative_extra": "text, watermark, border, scene background"
        },
        {
            "name": "tex_vfx_speed_streak_hd",
            "prompt": "horizontal speed line motion blur VFX sprite, white and cyan streaks, strong motion blur effect, centered, on pure black background, game particle effect",
            "size": 512,
            "negative_extra": "text, watermark, border, scene background"
        },
        {
            "name": "tex_vfx_jump_splash",
            "prompt": "upward burst splash VFX sprite, white and light blue particles shooting upward, energy rings, high contrast, centered, on pure black background, game particle effect",
            "size": 512,
            "negative_extra": "text, watermark, border, scene background"
        },
        {
            "name": "tex_vfx_slide_dust",
            "prompt": "ground dust cloud VFX sprite, brown and grey dust puff spreading sideways, high contrast, centered, on pure black background, game particle effect",
            "size": 512,
            "negative_extra": "text, watermark, border, scene background"
        },

        # === UI TEXTURES ===
        {
            "name": "tex_ui_hud_panel_hd",
            "prompt": f"dark blue semi-transparent game HUD panel, rounded corners, subtle gradient, gold border accent, {style}, clean UI element on transparent background",
            "size": 512,
        },
        {
            "name": "tex_ui_coin_icon_hd",
            "prompt": f"shiny gold coin game icon, embossed star center, metallic highlight, {style}, game UI collectible icon on transparent background",
            "size": 256,
        },
        {
            "name": "tex_ui_play_btn_hd",
            "prompt": f"bright green glossy play button with white triangle arrow, rounded rectangle, gradient shine, {style}, game UI button on transparent background",
            "size": 256,
        },

        # === IMPROVED OBSTACLE/PROP TEXTURES (for flat-surface parts of composite objects) ===
        {
            "name": "tex_train_graffiti_side_hd",
            "prompt": f"subway train side panel with colorful graffiti art, spray paint tags and drips, urban street art, bright colors on silver metal, {style}, seamless tileable",
            "size": 1024,
        },
        {
            "name": "tex_car_paint_glossy",
            "prompt": f"smooth glossy car paint texture, bright yellow taxi color, subtle metallic sheen, {style}, seamless tileable",
            "size": 512,
            "negative_extra": "scratches, dents, rust, dirty"
        },
        {
            "name": "tex_bus_paint_red",
            "prompt": f"smooth glossy bus paint texture, bright red double-decker bus color, subtle metallic sheen, {style}, seamless tileable",
            "size": 512,
            "negative_extra": "scratches, dents, rust, dirty"
        },

        # === BILLBOARD/SIGN TEXTURES ===
        {
            "name": "tex_billboard_sneakers",
            "prompt": f"colorful sneaker advertisement billboard, cool sneaker shoes on bright gradient background, urban street fashion ad, {style}",
            "size": 512,
        },
        {
            "name": "tex_billboard_energy_drink",
            "prompt": f"neon energy drink advertisement billboard, glowing can with lightning bolts, extreme sports vibe, {style}",
            "size": 512,
        },

        # === ENHANCED PROP TEXTURES ===
        {
            "name": "tex_prop_food_cart_hd",
            "prompt": f"colorful street food cart vendor stand, hot dogs and pretzels, red and yellow striped awning, {style}, front view",
            "size": 512,
        },
        {
            "name": "tex_prop_vending_machine_hd",
            "prompt": f"bright blue soda vending machine front panel, coin slot, glowing display, colorful drink cans visible, {style}, front view",
            "size": 512,
        },
    ]

    results = {}
    total = len(textures)
    for i, tex in enumerate(textures):
        name = tex["name"]
        prompt = tex["prompt"]
        size = tex.get("size", 512)
        neg_extra = tex.get("negative_extra", "")

        full_negative = f"{negative}, {neg_extra}" if neg_extra else negative
        print(f"[{i+1}/{total}] Generating: {name} ({size}x{size})")

        seed = int(hashlib.md5(name.encode()).hexdigest()[:8], 16)
        generator = torch.Generator("cuda").manual_seed(seed)

        image = pipe(
            prompt=prompt,
            negative_prompt=full_negative,
            width=size,
            height=size,
            num_inference_steps=30,
            guidance_scale=8.5,
            generator=generator,
        ).images[0]

        # For VFX textures, convert black background to alpha
        if name.startswith("tex_vfx_"):
            rgba = image.convert("RGBA")
            px = rgba.load()
            for y in range(rgba.size[1]):
                for x in range(rgba.size[0]):
                    r, g, b, _ = px[x, y]
                    lum = int((r + g + b) / 3)
                    px[x, y] = (255, 255, 255, lum)
            image = rgba

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
    print("EMERSYN RUNNER PHASE 15D - SUBWAY SURFERS PARITY TEXTURES")
    print(f"Generating 23 high-quality textures")
    print("=" * 60)

    start = time.time()
    results = generate_phase15d_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase15d textures/ /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
