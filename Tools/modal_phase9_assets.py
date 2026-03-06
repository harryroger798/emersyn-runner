#!/usr/bin/env python3
"""
modal_phase9_assets.py - Phase 9: Character Detail + Environment Variety + VFX
Generates 25+ textures targeting remaining visual gaps vs Subway Surfers:
- Character detail textures (face, hair, shoes, accessories)
- More building variety (apartment, office tower, warehouse, diner)
- VFX sprite sheets (speed lines, impact stars, coin sparkle, trail)
- Ground detail (puddles, grates, painted road markings)
- Additional prop textures (vending machine, phone booth, fire escape)
Budget target: ~$3-5 on A10G
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-phase9")

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

asset_volume = modal.Volume.from_name("emersyn-phase9-assets", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=3600,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_phase9_textures():
    """Generate Phase 9 character, environment, and VFX textures."""
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
        # === CHARACTER DETAIL TEXTURES ===
        {"name": "tex_emersyn_face_detail", "prompt": f"cartoon kid face texture, large expressive eyes, small nose, friendly smile, light skin tone, front view, character texture map, {style}", "size": 512},
        {"name": "tex_emersyn_hair_brown", "prompt": f"cartoon kid messy brown hair texture, spiky fun hairstyle, top view, character texture map, {style}", "size": 256},
        {"name": "tex_emersyn_shoes_red", "prompt": f"pair of bright red cartoon sneakers, white soles, front view, character accessory, {style}", "size": 256},
        {"name": "tex_emersyn_backpack", "prompt": f"colorful cartoon school backpack, blue with yellow zipper, front view, character accessory, {style}", "size": 256},
        {"name": "tex_outfit_red_hoodie", "prompt": f"bright red hoodie sweatshirt front view, cartoon style, kid clothing texture, clean flat colors, {style}", "size": 512},
        {"name": "tex_outfit_green_jacket", "prompt": f"green bomber jacket front view, cartoon style, kid clothing texture, clean flat colors with patches, {style}", "size": 512},
        {"name": "tex_outfit_purple_tee", "prompt": f"purple t-shirt with cool star design front view, cartoon style, kid clothing texture, {style}", "size": 512},

        # === MORE BUILDING VARIETY ===
        {"name": "tex_building_apartment", "prompt": f"seamless tileable apartment building facade, multiple floors with balconies, warm brown brick with flower boxes, {style}", "size": 1024},
        {"name": "tex_building_office_tower", "prompt": f"seamless tileable modern office tower facade, blue glass curtain wall with steel frame, reflective windows, {style}", "size": 1024},
        {"name": "tex_building_warehouse", "prompt": f"seamless tileable old warehouse building facade, corrugated metal walls, large roller door, industrial, {style}", "size": 1024},
        {"name": "tex_building_diner", "prompt": f"seamless tileable retro diner building facade, chrome and red exterior, neon open sign, large windows, {style}", "size": 1024},
        {"name": "tex_building_bookstore", "prompt": f"seamless tileable cozy bookstore building facade, warm wood exterior, large display window with books, {style}", "size": 1024},

        # === VFX SPRITE TEXTURES ===
        {"name": "tex_vfx_speed_lines", "prompt": f"horizontal speed lines motion blur effect, white streaks on transparent background, game VFX sprite, {style}", "size": 256},
        {"name": "tex_vfx_impact_star", "prompt": f"yellow cartoon impact star burst effect, comic book style pow effect, game VFX sprite on black background, {style}", "size": 256},
        {"name": "tex_vfx_coin_sparkle", "prompt": f"golden sparkle glitter particle effect, shiny star particles, game collectible VFX on black background, {style}", "size": 256},
        {"name": "tex_vfx_trail_blue", "prompt": f"blue energy trail swoosh effect, glowing motion trail, game character running VFX on black background, {style}", "size": 256},
        {"name": "tex_vfx_dust_cloud", "prompt": f"cartoon dust cloud puff effect, light brown smoke cloud, landing impact VFX on black background, {style}", "size": 256},

        # === GROUND DETAIL TEXTURES ===
        {"name": "tex_ground_puddle", "prompt": f"small rain puddle reflection on asphalt ground, wet road surface, top down view, {style}", "size": 256},
        {"name": "tex_ground_grate", "prompt": f"metal street drainage grate on dark asphalt, rectangular grid pattern, top down view, urban detail, {style}", "size": 256},
        {"name": "tex_road_marking_arrow", "prompt": f"white painted arrow marking on dark road pointing forward, traffic lane marking, top down view, {style}", "size": 256},

        # === ADDITIONAL PROP TEXTURES ===
        {"name": "tex_prop_vending_machine", "prompt": f"colorful cartoon vending machine front view, drinks visible behind glass, coin slot, {style}", "size": 512},
        {"name": "tex_prop_phone_booth", "prompt": f"red telephone booth front view, classic British style, cartoon urban prop, {style}", "size": 512},
        {"name": "tex_prop_fire_escape", "prompt": f"metal fire escape ladder and platform side view, industrial urban building detail, {style}", "size": 512},
        {"name": "tex_prop_awning_striped", "prompt": f"red and white striped shop awning front view, cartoon style storefront canopy, {style}", "size": 256},
        {"name": "tex_prop_potted_plant", "prompt": f"green potted plant in terracotta pot front view, cartoon style decorative plant, {style}", "size": 256},
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
    print("EMERSYN RUNNER PHASE 9 - CHARACTER + ENVIRONMENT + VFX")
    print(f"Generating 25 high-quality SDXL textures")
    print("=" * 60)

    start = time.time()
    results = generate_phase9_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    print(f"\nTo download: modal volume get emersyn-phase9-assets textures/ /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
