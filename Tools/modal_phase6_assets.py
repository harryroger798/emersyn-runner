#!/usr/bin/env python3
"""
modal_phase6_assets.py - Phase 6: Final Polish Textures
Generates remaining textures for visual completeness:
- Sky gradient texture for better sky rendering
- Improved coin texture (golden, detailed)
- Ground detail textures
- Weather/atmosphere textures
Budget target: ~$5 on A10G
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase6")

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

asset_volume = modal.Volume.from_name("emersyn-phase6-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=1800,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_phase6_textures():
    """Generate Phase 6 final polish textures."""
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
        # === SKY GRADIENT (for skybox replacement) ===
        {"name": "tex_sky_gradient", "prompt": f"beautiful blue sky gradient from light blue at bottom to deeper blue at top, wispy white clouds, bright sunny day atmosphere, game background, {style}", "size": 1024},
        {"name": "tex_sky_sunset", "prompt": f"beautiful sunset sky gradient from warm orange at bottom through pink to purple at top, dramatic golden clouds, game background, {style}", "size": 1024},

        # === IMPROVED COIN TEXTURE ===
        {"name": "tex_coin_detailed", "prompt": f"golden coin with embossed star design in center, shiny metallic surface with rim detail, front view, game collectible icon, {style}", "size": 256},

        # === GROUND DETAIL TEXTURES ===
        {"name": "tex_ground_dirt", "prompt": f"seamless tileable brown dirt ground texture with small pebbles, top down view, natural earth, {style}", "size": 512},
        {"name": "tex_ground_grass_patch", "prompt": f"seamless tileable lush green grass with small yellow wildflowers, top down view, natural ground, {style}", "size": 512},

        # === ADDITIONAL BUILDING VARIETY ===
        {"name": "tex_building_hospital", "prompt": f"seamless tileable hospital building facade, white walls with blue cross signs, large glass doors, clean modern, {style}", "size": 1024},
        {"name": "tex_building_school", "prompt": f"seamless tileable school building facade, brick walls with colorful bulletin boards, yellow entrance, {style}", "size": 1024},
        {"name": "tex_building_cinema", "prompt": f"seamless tileable movie cinema building facade, art deco style with neon marquee sign, red carpet entrance, {style}", "size": 1024},

        # === WEATHER/ATMOSPHERE ===
        {"name": "tex_fog_gradient", "prompt": f"smooth white to transparent fog mist gradient, atmospheric effect, soft edges, game VFX, {style}", "size": 256},

        # === ROAD MARKINGS ===
        {"name": "tex_road_arrow", "prompt": f"white arrow road marking pointing up on dark asphalt background, traffic directional marking, top down view, {style}", "size": 256},

        # === UI POLISH ===
        {"name": "tex_ui_coin_icon", "prompt": f"small golden coin icon with sparkle, game HUD element, pixel perfect, {style}", "size": 128},
        {"name": "tex_ui_heart_icon", "prompt": f"red heart life icon with glossy highlight, game HUD element, pixel perfect, {style}", "size": 128},
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
    print("EMERSYN RUNNER PHASE 6 - FINAL POLISH TEXTURES")
    print(f"Generating 12 high-quality textures")
    print("=" * 60)

    start = time.time()
    results = generate_phase6_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase6-assets textures/ --output /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
