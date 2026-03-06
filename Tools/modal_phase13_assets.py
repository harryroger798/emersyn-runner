import modal
import hashlib

app = modal.App("emersyn-phase13-assets")
volume = modal.Volume.from_name("emersyn-phase13-assets", create_if_missing=True)

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
        # === SUBWAY-STYLE CHARACTER (clean solid-color outfits) ===
        {"name": "tex_character_hoodie_purple", "prompt": f"flat solid purple hoodie sweatshirt fabric texture, uniform color, subtle wrinkle detail, game character outfit, {style}", "size": 256},
        {"name": "tex_character_sneakers_white", "prompt": f"flat white sneakers shoes texture, clean white with subtle grey accents, game character footwear, {style}", "size": 256},
        {"name": "tex_character_cap_red", "prompt": f"flat red baseball cap hat texture, solid red color, game character accessory, {style}", "size": 256},
        {"name": "tex_character_backpack_yellow", "prompt": f"flat yellow school backpack texture, bright yellow with zipper detail, game character accessory, {style}", "size": 256},
        
        # === SUBWAY SURFER STYLE ENVIRONMENT ===
        {"name": "tex_train_subway_blue", "prompt": f"subway train car side view, blue and silver metallic, windows, doors, graffiti tags, urban city train, {style}", "size": 1024},
        {"name": "tex_train_subway_red", "prompt": f"subway train car side view, red and white, windows, doors, destination sign, urban city train, {style}", "size": 1024},
        
        # === COLLECTIBLES ===
        {"name": "tex_powerup_jetpack", "prompt": f"cartoon jetpack icon with flames, metallic silver and orange, game powerup item, {style}", "size": 256},
        {"name": "tex_powerup_magnet", "prompt": f"cartoon horseshoe magnet icon, red and silver, magnetic field lines, game powerup item, {style}", "size": 256},
        {"name": "tex_powerup_multiplier", "prompt": f"cartoon 2x multiplier star icon, gold and glowing, game powerup item, {style}", "size": 256},
        
        # === ADDITIONAL BUILDINGS ===
        {"name": "tex_building_subway_station", "prompt": f"subway train station entrance facade, underground entrance with stairway, urban city, {style}", "size": 1024},
        {"name": "tex_building_convenience_store", "prompt": f"24/7 convenience store facade, neon open sign, snack displays, urban city shop, {style}", "size": 1024},
        {"name": "tex_building_electronics_shop", "prompt": f"electronics gadget shop storefront, TV displays in window, neon signs, urban city, {style}", "size": 1024},
        
        # === GROUND/TRACK DETAILS ===
        {"name": "tex_track_rail", "prompt": f"seamless tileable train rail track texture, metal rail on wood ties, top down view, {style}", "size": 512},
        {"name": "tex_ground_platform", "prompt": f"seamless tileable subway platform texture, yellow safety line edge, concrete floor, {style}", "size": 512},
        
        # === VFX ===
        {"name": "tex_vfx_dash_trail", "prompt": f"speed dash motion blur trail effect, blue white energy streak, transparent background, {style}", "size": 256},
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
    print(f"Generated {result} Phase 13 textures")
