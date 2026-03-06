#!/usr/bin/env python3
"""
modal_phase14_road_fix.py - Generate clean road/sidewalk/curb textures
The current road textures have orange/yellow lane markings baked in,
causing visible colored streaks when rendered at perspective angles.
These new textures are uniformly dark grey with only subtle asphalt grain.
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase14-road")

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

asset_volume = modal.Volume.from_name("emersyn-phase14-road", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=1800,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_road_textures():
    """Generate clean road, sidewalk, and curb textures without colored markings."""
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

    negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, lane markings, yellow lines, orange lines, colored stripes, road markings, crosswalk, arrows, painted lines, white lines, vehicles, cars, trees, people"
    style = "seamless tileable texture, game asset, top-down view, uniform pattern"

    textures = [
        # Clean uniform dark asphalt - NO markings at all
        {
            "name": "tex_road_clean_asphalt",
            "prompt": f"dark grey asphalt road surface texture, uniform dark charcoal grey color, subtle asphalt grain and tiny pebbles, no markings no lines no paint, {style}",
            "size": 1024,
            "negative_extra": "markings, lines, paint, stripes, arrows, yellow, orange, white lines, crosswalk, cracks"
        },
        # Secondary clean road texture variant
        {
            "name": "tex_road_plain_grey",
            "prompt": f"plain dark grey concrete road surface, uniform medium grey, subtle concrete texture grain, no markings no lines, {style}",
            "size": 1024,
            "negative_extra": "markings, lines, paint, stripes, arrows, yellow, orange, cracks"
        },
        # Clean sidewalk - uniform grey stone
        {
            "name": "tex_sidewalk_clean_grey",
            "prompt": f"light grey concrete sidewalk pavement texture, uniform grey stone slabs, subtle rectangular tile pattern, no cracks, {style}",
            "size": 512,
            "negative_extra": "grass, dirt, markings, yellow, orange, colored"
        },
        # Clean curb texture
        {
            "name": "tex_curb_dark_grey",
            "prompt": f"dark grey concrete curb stone texture, uniform dark granite grey, subtle stone grain, {style}",
            "size": 256,
            "negative_extra": "yellow, orange, paint, colored, markings"
        },
        # Clean grass strip - dark green
        {
            "name": "tex_grass_dark_green",
            "prompt": f"dark green grass lawn texture, uniform short cut grass, deep green color, no flowers, {style}",
            "size": 512,
            "negative_extra": "yellow, orange, brown, dead grass, flowers, dirt"
        },
        # Better building facade - neutral grey modern
        {
            "name": "tex_building_modern_grey",
            "prompt": f"modern grey building facade with glass windows, neutral grey concrete and steel, blue-tinted glass windows arranged in grid, {style}, stylized cartoon game art",
            "size": 1024,
            "negative_extra": "orange, warm colors, brick, red"
        },
        # Better building facade - cool blue glass
        {
            "name": "tex_building_glass_blue",
            "prompt": f"modern glass office building facade, reflective blue-green glass curtain wall, steel frame grid pattern, cool tones, {style}, stylized cartoon game art",
            "size": 1024,
            "negative_extra": "orange, warm colors, brick, red, yellow"
        },
        # Better building facade - white concrete
        {
            "name": "tex_building_white_modern",
            "prompt": f"white modern apartment building facade, clean white concrete with rectangular windows, minimalist architecture, {style}, stylized cartoon game art",
            "size": 1024,
            "negative_extra": "orange, warm colors, old, dirty, brick"
        },
        # Dark fill texture for edges
        {
            "name": "tex_edge_dark_fill",
            "prompt": f"uniform very dark grey almost black surface texture, dark charcoal black, subtle grain, {style}",
            "size": 256,
            "negative_extra": "colored, bright, orange, yellow, markings, lines"
        },
        # Character outfit - clean blue hoodie
        {
            "name": "tex_outfit_blue_hoodie",
            "prompt": f"bright blue hoodie sweatshirt front view, cartoon style, kid clothing texture, clean flat blue color with white drawstrings, stylized cartoon mobile game art",
            "size": 512,
            "negative_extra": "realistic, photo, wrinkles"
        },
        # Character outfit - white sneakers
        {
            "name": "tex_outfit_white_sneakers",
            "prompt": f"pair of clean white cartoon sneakers with blue accents, front view, character accessory, stylized cartoon mobile game art",
            "size": 256,
            "negative_extra": "realistic, photo, dirty"
        },
        # Better sky gradient
        {
            "name": "tex_sky_blue_gradient",
            "prompt": f"clear blue sky gradient, light blue at top fading to pale blue-white at horizon, no clouds, clean gradient, game background, {style}",
            "size": 1024,
            "negative_extra": "clouds, sun, stars, orange, sunset"
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
    print("EMERSYN RUNNER PHASE 14 - CLEAN ROAD TEXTURES")
    print(f"Generating 12 clean road/sidewalk/building textures")
    print("=" * 60)

    start = time.time()
    results = generate_road_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase14-road textures/ /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
