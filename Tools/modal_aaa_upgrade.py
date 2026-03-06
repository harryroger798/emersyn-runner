#!/usr/bin/env python3
"""
modal_aaa_upgrade.py - Generate AAA-upgrade assets for Emersyn Runner.
Generates additional textures for richer visuals: more props, character outfits,
hoverboard skins, VFX sprites, and UI elements.
All in ONE GPU session for cost efficiency (~$8-12 on A10G).
"""

import modal
import os
import io
import hashlib

app = modal.App("emersyn-aaa-upgrade")

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

asset_volume = modal.Volume.from_name("emersyn-aaa-upgrade", create_if_missing=True)
ASSET_MOUNT = "/assets"


@app.function(
    image=gpu_image,
    gpu="A10G",
    timeout=3600,
    volumes={ASSET_MOUNT: asset_volume},
)
def generate_all_upgrade_textures():
    """Generate all AAA upgrade textures in one GPU session."""
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

    default_negative = "photorealistic, photo, blurry, low quality, watermark, text, nsfw, violence, ugly, deformed, realistic"
    style = "stylized cartoon mobile game art, vibrant colors, hand-painted look, subway surfers style"

    textures = [
        # === ADDITIONAL BUILDINGS (more variety) ===
        {"name": "tex_building_modern_glass", "prompt": f"seamless tileable modern glass skyscraper facade texture, reflective blue glass windows with steel frames, {style}", "size": 512},
        {"name": "tex_building_brownstone", "prompt": f"seamless tileable brownstone apartment building facade, warm brown bricks with fire escapes and windows, {style}", "size": 512},
        {"name": "tex_building_shop_front", "prompt": f"seamless tileable colorful shop storefront texture, awning and display window, bright signage, {style}", "size": 512},
        {"name": "tex_building_neon", "prompt": f"seamless tileable neon-lit nightclub building facade, glowing purple and blue neon signs, {style}", "size": 512},
        {"name": "tex_building_graffiti", "prompt": f"seamless tileable urban wall covered in colorful graffiti art, spray paint tags and murals, {style}", "size": 512},

        # === GROUND VARIANTS ===
        {"name": "tex_ground_cobblestone", "prompt": f"seamless tileable cobblestone street texture, round grey stones with mortar, top down view, {style}", "size": 512},
        {"name": "tex_ground_crosswalk", "prompt": f"seamless tileable pedestrian crosswalk white stripes on asphalt, top down view, {style}", "size": 512},
        {"name": "tex_ground_manhole", "prompt": f"round metal manhole cover on asphalt texture, circular pattern, top down view, {style}", "size": 256},

        # === PROP TEXTURES ===
        {"name": "tex_prop_trashcan", "prompt": f"green trash can garbage bin texture, metal cylindrical container, front view, {style}", "size": 256},
        {"name": "tex_prop_bench", "prompt": f"wooden park bench texture, brown wood planks with metal armrests, side view, {style}", "size": 256},
        {"name": "tex_prop_mailbox", "prompt": f"blue mailbox post box texture, classic urban mail collection box, front view, {style}", "size": 256},
        {"name": "tex_prop_hydrant", "prompt": f"red fire hydrant texture, classic city fire hydrant, front view, {style}", "size": 256},
        {"name": "tex_prop_newspaper", "prompt": f"newspaper vending machine texture, yellow box with glass front, {style}", "size": 256},
        {"name": "tex_prop_bollard", "prompt": f"metal street bollard post texture, chrome steel cylindrical post, {style}", "size": 256},
        {"name": "tex_prop_planter", "prompt": f"concrete street planter with colorful flowers, flower box, {style}", "size": 256},
        {"name": "tex_prop_streetlight", "prompt": f"ornate city street light lamp post texture, vintage iron design with glowing lantern, {style}", "size": 256},

        # === CHARACTER OUTFIT VARIANTS (for character selection) ===
        {"name": "tex_outfit_red_hoodie", "prompt": f"bright red hoodie sweatshirt fabric texture, clean color with subtle wrinkle detail, {style}", "size": 256},
        {"name": "tex_outfit_green_jacket", "prompt": f"forest green zip jacket fabric texture, sporty casual style, {style}", "size": 256},
        {"name": "tex_outfit_purple_sweater", "prompt": f"vibrant purple sweater knit fabric texture, warm cozy style, {style}", "size": 256},
        {"name": "tex_outfit_orange_vest", "prompt": f"bright orange safety vest fabric texture with reflective stripes, sporty, {style}", "size": 256},
        {"name": "tex_outfit_pink_tshirt", "prompt": f"hot pink t-shirt fabric texture, clean cotton material, {style}", "size": 256},
        {"name": "tex_pants_grey_joggers", "prompt": f"grey jogger sweatpants fabric texture, athletic style, {style}", "size": 256},
        {"name": "tex_pants_camo", "prompt": f"green camouflage pants fabric texture, military camo pattern, {style}", "size": 256},
        {"name": "tex_shoes_gold", "prompt": f"shiny gold metallic sneaker shoe texture, premium glowing, {style}", "size": 256},
        {"name": "tex_shoes_neon_green", "prompt": f"neon green bright sneaker shoe texture with white sole, sporty, {style}", "size": 256},

        # === HOVERBOARD / SKATEBOARD TEXTURES ===
        {"name": "tex_board_blue_flame", "prompt": f"skateboard deck top view with blue flame design, cool graffiti art, {style}", "size": 256},
        {"name": "tex_board_galaxy", "prompt": f"skateboard deck top view with galaxy space nebula design, purple stars, {style}", "size": 256},
        {"name": "tex_board_lightning", "prompt": f"skateboard deck top view with yellow lightning bolt design on black, {style}", "size": 256},
        {"name": "tex_board_pixel", "prompt": f"skateboard deck top view with retro pixel art design, colorful 8-bit pattern, {style}", "size": 256},
        {"name": "tex_board_rainbow", "prompt": f"skateboard deck top view with rainbow gradient stripes design, vibrant, {style}", "size": 256},

        # === OBSTACLE VARIANTS ===
        {"name": "tex_obstacle_dumpster", "prompt": f"green dumpster waste container texture, industrial metal bin, front view, {style}", "size": 256},
        {"name": "tex_obstacle_construction", "prompt": f"orange and white construction barrier barricade texture, warning stripes, {style}", "size": 256},
        {"name": "tex_obstacle_car_side", "prompt": f"colorful cartoon taxi cab car side panel texture, yellow with checkered stripe, {style}", "size": 512},
        {"name": "tex_obstacle_bus", "prompt": f"colorful city bus side panel texture, blue and white with windows, {style}", "size": 512},

        # === TRAIN VARIANTS ===
        {"name": "tex_train_graffiti_2", "prompt": f"subway train side panel covered in colorful graffiti art, spray paint tags and wild style letters, {style}", "size": 512},
        {"name": "tex_train_clean", "prompt": f"clean modern subway train side panel, silver metallic with blue stripe and windows, {style}", "size": 512},

        # === UI ELEMENTS ===
        {"name": "tex_ui_coin_icon", "prompt": f"golden coin icon with dollar sign, shiny metallic, game UI element, transparent background, {style}", "size": 128},
        {"name": "tex_ui_heart", "prompt": f"red heart icon, glossy 3D look, game UI health element, {style}", "size": 128},
        {"name": "tex_ui_star", "prompt": f"golden star icon, shiny glowing, game UI reward element, {style}", "size": 128},
        {"name": "tex_ui_shield_icon", "prompt": f"blue glowing shield icon, energy barrier, game UI powerup element, {style}", "size": 128},
        {"name": "tex_ui_magnet_icon", "prompt": f"red horseshoe magnet icon with blue energy, game UI powerup element, {style}", "size": 128},
        {"name": "tex_ui_multiplier_icon", "prompt": f"purple 2x multiplier icon with sparkles, game UI bonus element, {style}", "size": 128},
        {"name": "tex_ui_menu_bg", "prompt": f"colorful blurred city street background for game menu, bokeh lights, evening atmosphere, {style}", "size": 512},
        {"name": "tex_ui_game_logo", "prompt": f"3D text logo EMERSYN RUNNER in bold chrome letters with speed lines and motion blur effect, game title, {style}", "size": 512},

        # === SKY/ATMOSPHERE VARIANTS ===
        {"name": "tex_sky_sunset", "prompt": f"beautiful sunset sky gradient with orange purple clouds, dramatic game background, {style}", "size": 512},
        {"name": "tex_sky_night", "prompt": f"dark night sky with stars and city glow, deep blue purple gradient, game background, {style}", "size": 512},

        # === VFX SPRITE ELEMENTS ===
        {"name": "tex_vfx_sparkle", "prompt": f"golden sparkle star burst effect sprite, particle effect, transparent look, bright glow, {style}", "size": 128},
        {"name": "tex_vfx_speed_line", "prompt": f"horizontal speed lines motion blur effect sprite, white streaks on dark, motion effect, {style}", "size": 256},
        {"name": "tex_vfx_dust_cloud", "prompt": f"small dust puff cloud effect sprite, light brown smoke, particle effect, {style}", "size": 128},
        {"name": "tex_vfx_coin_burst", "prompt": f"golden coin explosion burst effect sprite, coins flying outward, collect effect, {style}", "size": 256},
        {"name": "tex_vfx_shield_bubble", "prompt": f"blue translucent energy shield bubble sphere effect, glowing barrier, {style}", "size": 256},
        {"name": "tex_vfx_boost_trail", "prompt": f"rainbow color speed boost trail effect, horizontal motion lines with glow, {style}", "size": 256},
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
        print(f"  Saved: {name}.png ({len(buf.getvalue())} bytes)")

    asset_volume.commit()
    print(f"\nGenerated {len(results)} textures total")
    return results


@app.local_entrypoint()
def main():
    import time

    print("=" * 60)
    print("EMERSYN RUNNER AAA UPGRADE - TEXTURE GENERATION")
    print(f"Generating additional textures for richer visuals")
    print("=" * 60)

    start = time.time()
    results = generate_all_upgrade_textures.remote()
    elapsed = time.time() - start

    print(f"\n{'=' * 60}")
    print(f"Generated {len(results)} textures in {elapsed:.1f}s")
    total_bytes = sum(results.values())
    print(f"Total size: {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~${elapsed / 3600 * 1.10:.2f} (A10G @ $1.10/hr)")
    print(f"{'=' * 60}")

    # Download from volume to local
    local_base = "/home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures"
    os.makedirs(local_base, exist_ok=True)

    print(f"\nDownloading textures to {local_base}...")
    # We'll download via volume in a separate step
    print("Use: modal volume get emersyn-aaa-upgrade textures/ --output /home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures/")
