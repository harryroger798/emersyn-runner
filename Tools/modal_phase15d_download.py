#!/usr/bin/env python3
"""
Download Phase 15D textures from Modal by generating them and returning bytes directly.
This avoids the Modal volume download 404 issue.
"""

import modal
import os
import io
import hashlib
import base64
import json

app = modal.App("emersyn-phase15d-dl")

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


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=1800,
)
def generate_and_return_textures():
    """Generate Phase 15D textures and return them as base64-encoded bytes."""
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
        # BUILDING FACADES
        {"name": "tex_building_neon_arcade", "prompt": f"colorful neon-lit arcade game center building facade, bright pink and blue neon signs, pixel art decorations, glass storefront, {style}, seamless tileable", "size": 1024},
        {"name": "tex_building_sushi_bar", "prompt": f"Japanese sushi bar restaurant building facade, paper lanterns, wooden accents, red and white color scheme, {style}, seamless tileable", "size": 1024},
        {"name": "tex_building_comic_store", "prompt": f"comic book store building facade, colorful superhero posters in windows, bright yellow and red awning, {style}, seamless tileable", "size": 1024},
        {"name": "tex_building_ice_cream", "prompt": f"ice cream parlor building facade, pastel pink and mint green, waffle cone decorations, striped awning, {style}, seamless tileable", "size": 1024},
        {"name": "tex_building_skate_shop", "prompt": f"skateboard shop building facade, graffiti art, urban street style, cool blue and orange color scheme, {style}, seamless tileable", "size": 1024},
        # GROUND
        {"name": "tex_road_subway_style", "prompt": f"dark grey asphalt road surface, smooth clean texture, subtle aggregate grain, top-down view, no markings, {style}, seamless tileable", "size": 1024, "negative_extra": "markings, lines, paint, yellow, orange, white lines, cracks, arrows"},
        {"name": "tex_sidewalk_subway_style", "prompt": f"clean light grey concrete sidewalk pavement, rectangular paving tiles, subtle grid pattern, top-down view, {style}, seamless tileable", "size": 512, "negative_extra": "grass, dirt, cracks, yellow, colored"},
        # SKY
        {"name": "tex_sky_subway_blue", "prompt": f"beautiful bright blue sky with fluffy white cartoon clouds, vibrant blue gradient, mobile game sky background, {style}", "size": 1024, "negative_extra": "sunset, orange, dark, night, stars"},
        # VFX
        {"name": "tex_vfx_coin_burst_hd", "prompt": "golden sparkle burst explosion VFX sprite, bright gold and yellow particles radiating outward, high contrast, centered, on pure black background, game particle effect", "size": 512, "negative_extra": "text, watermark, border, scene background"},
        {"name": "tex_vfx_speed_streak_hd", "prompt": "horizontal speed line motion blur VFX sprite, white and cyan streaks, strong motion blur effect, centered, on pure black background, game particle effect", "size": 512, "negative_extra": "text, watermark, border, scene background"},
        {"name": "tex_vfx_jump_splash", "prompt": "upward burst splash VFX sprite, white and light blue particles shooting upward, energy rings, high contrast, centered, on pure black background, game particle effect", "size": 512, "negative_extra": "text, watermark, border, scene background"},
        {"name": "tex_vfx_slide_dust", "prompt": "ground dust cloud VFX sprite, brown and grey dust puff spreading sideways, high contrast, centered, on pure black background, game particle effect", "size": 512, "negative_extra": "text, watermark, border, scene background"},
        # UI
        {"name": "tex_ui_hud_panel_hd", "prompt": f"dark blue semi-transparent game HUD panel, rounded corners, subtle gradient, gold border accent, {style}, clean UI element on transparent background", "size": 512},
        {"name": "tex_ui_coin_icon_hd", "prompt": f"shiny gold coin game icon, embossed star center, metallic highlight, {style}, game UI collectible icon on transparent background", "size": 256},
        {"name": "tex_ui_play_btn_hd", "prompt": f"bright green glossy play button with white triangle arrow, rounded rectangle, gradient shine, {style}, game UI button on transparent background", "size": 256},
        # OBSTACLES/PROPS
        {"name": "tex_train_graffiti_side_hd", "prompt": f"subway train side panel with colorful graffiti art, spray paint tags and drips, urban street art, bright colors on silver metal, {style}, seamless tileable", "size": 1024},
        {"name": "tex_car_paint_glossy", "prompt": f"smooth glossy car paint texture, bright yellow taxi color, subtle metallic sheen, {style}, seamless tileable", "size": 512, "negative_extra": "scratches, dents, rust, dirty"},
        {"name": "tex_bus_paint_red", "prompt": f"smooth glossy bus paint texture, bright red double-decker bus color, subtle metallic sheen, {style}, seamless tileable", "size": 512, "negative_extra": "scratches, dents, rust, dirty"},
        # BILLBOARDS
        {"name": "tex_billboard_sneakers", "prompt": f"colorful sneaker advertisement billboard, cool sneaker shoes on bright gradient background, urban street fashion ad, {style}", "size": 512},
        {"name": "tex_billboard_energy_drink", "prompt": f"neon energy drink advertisement billboard, glowing can with lightning bolts, extreme sports vibe, {style}", "size": 512},
        # PROPS
        {"name": "tex_prop_food_cart_hd", "prompt": f"colorful street food cart vendor stand, hot dogs and pretzels, red and yellow striped awning, {style}, front view", "size": 512},
        {"name": "tex_prop_vending_machine_hd", "prompt": f"bright blue soda vending machine front panel, coin slot, glowing display, colorful drink cans visible, {style}, front view", "size": 512},
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

        buf = io.BytesIO()
        image.save(buf, format="PNG")
        b64 = base64.b64encode(buf.getvalue()).decode("ascii")
        results[name] = b64
        print(f"  Done: {name}.png ({len(buf.getvalue())} bytes)")

    return results


@app.local_entrypoint()
def main():
    import time
    import base64

    output_dir = "/home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures"
    os.makedirs(output_dir, exist_ok=True)

    print("=" * 60)
    print("PHASE 15D - Generating and downloading 22 textures")
    print("=" * 60)

    start = time.time()
    results = generate_and_return_textures.remote()
    elapsed = time.time() - start

    print(f"\nReceived {len(results)} textures in {elapsed:.1f}s")

    for name, b64data in results.items():
        png_bytes = base64.b64decode(b64data)
        path = os.path.join(output_dir, f"{name}.png")
        with open(path, "wb") as f:
            f.write(png_bytes)
        print(f"  Saved: {path} ({len(png_bytes)} bytes)")

    total_bytes = sum(len(base64.b64decode(v)) for v in results.values())
    print(f"\nTotal: {len(results)} textures, {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f}")
