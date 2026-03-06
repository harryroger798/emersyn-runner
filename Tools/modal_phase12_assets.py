import modal
import hashlib

app = modal.App("emersyn-phase12-assets")
volume = modal.Volume.from_name("emersyn-phase12-assets", create_if_missing=True)

@app.function(
    gpu="A10G",
    image=modal.Image.debian_slim(python_version="3.10").pip_install(
        "torch==2.1.0", "diffusers==0.27.2", "transformers==4.38.2",
        "accelerate==0.27.2", "safetensors==0.4.2", "Pillow==10.2.0",
        "numpy<2", "huggingface_hub==0.21.4"
    ),
    timeout=600,
    volumes={"/data": volume},
)
def generate_textures():
    import torch
    from diffusers import StableDiffusionXLPipeline
    from PIL import Image
    import os, time

    start = time.time()
    pipe = StableDiffusionXLPipeline.from_pretrained(
        "stabilityai/stable-diffusion-xl-base-1.0",
        torch_dtype=torch.float16, variant="fp16", use_safetensors=True
    )
    pipe = pipe.to("cuda")
    pipe.set_progress_bar_config(disable=True)

    style = "clean cartoon style, cel shaded, bright saturated colors, mobile game art, Subway Surfers style"

    textures = [
        # === CHARACTER UNIFORM (clean single-color outfits) ===
        {"name": "tex_emersyn_uniform_blue", "prompt": f"flat blue hoodie fabric texture, solid blue color, slight wrinkle detail, uniform texture map, clean simple, {style}", "size": 256},
        {"name": "tex_emersyn_uniform_red", "prompt": f"flat red jacket fabric texture, solid red color, slight wrinkle detail, uniform texture map, clean simple, {style}", "size": 256},
        {"name": "tex_emersyn_uniform_green", "prompt": f"flat green bomber jacket fabric texture, solid green color, slight wrinkle detail, uniform texture map, clean simple, {style}", "size": 256},
        {"name": "tex_emersyn_jeans_blue", "prompt": f"flat blue denim jeans fabric texture, solid dark blue, slight crease detail, uniform texture map, {style}", "size": 256},

        # === ROAD SURFACE (darker, cleaner) ===
        {"name": "tex_road_dark_asphalt", "prompt": f"seamless tileable very dark grey asphalt road texture, clean smooth surface, top down view, no markings, {style}", "size": 512},
        {"name": "tex_road_concrete_grey", "prompt": f"seamless tileable light grey concrete road texture, clean smooth surface, top down view, no cracks, {style}", "size": 512},

        # === GROUND FILL (to replace orange streaks) ===
        {"name": "tex_ground_dark_fill", "prompt": f"seamless tileable very dark grey ground texture, asphalt-like, neutral color, no pattern, flat, {style}", "size": 256},
        {"name": "tex_sidewalk_wide", "prompt": f"seamless tileable wide sidewalk pavement texture, light grey stone tiles, clean, top down view, {style}", "size": 512},

        # === MORE ENVIRONMENT VARIETY ===
        {"name": "tex_building_toy_store", "prompt": f"seamless tileable colorful toy store facade, bright window displays, fun signage, cartoon storefront, {style}", "size": 1024},
        {"name": "tex_building_coffee_shop", "prompt": f"seamless tileable cozy coffee shop cafe facade, warm lighting, menu board, brown tones, {style}", "size": 1024},
        {"name": "tex_building_pet_shop", "prompt": f"seamless tileable pet shop storefront, paw print decals, aquarium in window, colorful, {style}", "size": 1024},

        # === IMPROVED OBSTACLE TEXTURES ===
        {"name": "tex_obstacle_yellow_taxi", "prompt": f"New York yellow taxi cab side view, checkered stripe, bright yellow, {style}", "size": 512},
        {"name": "tex_obstacle_red_bus", "prompt": f"red double decker city bus side view, windows, route number, {style}", "size": 512},

        # === UI POLISH ===
        {"name": "tex_ui_gem_icon", "prompt": f"blue diamond gem icon with sparkle, game UI premium currency, {style}", "size": 128},
        {"name": "tex_ui_settings_gear", "prompt": f"grey settings gear cog icon, game UI menu button, {style}", "size": 128},
    ]

    os.makedirs("/data/textures", exist_ok=True)
    generated = 0

    for tex in textures:
        seed = int(hashlib.md5(tex["name"].encode()).hexdigest()[:8], 16)
        gen = torch.Generator(device="cuda").manual_seed(seed)
        img = pipe(
            prompt=tex["prompt"],
            width=tex["size"], height=tex["size"],
            num_inference_steps=25,
            guidance_scale=7.5,
            generator=gen,
        ).images[0]

        path = f"/data/textures/{tex['name']}.png"
        img.save(path, "PNG")
        generated += 1
        print(f"[{generated}/{len(textures)}] {tex['name']} ({tex['size']}px)")

    volume.commit()
    elapsed = time.time() - start
    total_size = sum(os.path.getsize(f"/data/textures/{t['name']}.png") for t in textures)
    print(f"\nDone! {generated} textures, {total_size/1024/1024:.1f}MB, {elapsed:.1f}s")
    return generated

@app.local_entrypoint()
def main():
    result = generate_textures.remote()
    print(f"Generated {result} Phase 12 textures")
