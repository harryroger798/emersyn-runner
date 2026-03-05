import modal
import io
import hashlib

app = modal.App("emersyn-phase10-assets")
volume = modal.Volume.from_name("emersyn-phase10-assets", create_if_missing=True)

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
        # === IMPROVED CHARACTER TEXTURES ===
        {"name": "tex_emersyn_shirt_blue", "prompt": f"cartoon character blue hoodie texture, front view, wrinkle details, zipper, {style}", "size": 512},
        {"name": "tex_emersyn_pants_dark", "prompt": f"cartoon character dark blue jeans texture, front view, pocket details, {style}", "size": 256},
        
        # === IMPROVED OBSTACLE TEXTURES ===
        {"name": "tex_obstacle_taxi_side", "prompt": f"yellow New York taxi cab side view, checkered stripe, cartoon style taxi, {style}", "size": 512},
        {"name": "tex_obstacle_police_car", "prompt": f"police car side view, black and white with red blue lights, cartoon style, {style}", "size": 512},
        {"name": "tex_obstacle_truck_side", "prompt": f"delivery truck side view, white with company logo, cartoon style vehicle, {style}", "size": 512},
        
        # === IMPROVED ROAD TEXTURES ===
        {"name": "tex_sidewalk_hd", "prompt": f"seamless tileable concrete sidewalk texture, grey with subtle cracks, top down view, {style}", "size": 512},
        {"name": "tex_road_lane_marking", "prompt": f"white road lane marking dashed lines on dark asphalt, seamless tileable, top down, {style}", "size": 256},
        
        # === IMPROVED BUILDING TEXTURES ===
        {"name": "tex_building_skyscraper", "prompt": f"seamless tileable modern skyscraper facade, glass and steel, blue reflective windows, {style}", "size": 1024},
        {"name": "tex_building_brick_shop", "prompt": f"seamless tileable brick storefront, colorful awning, window display, warm tones, {style}", "size": 1024},
        {"name": "tex_building_hotel", "prompt": f"seamless tileable luxury hotel facade, grand entrance, balconies, elegant design, {style}", "size": 1024},
        {"name": "tex_building_gym", "prompt": f"seamless tileable modern gym facade, large windows, neon fitness sign, {style}", "size": 1024},
        
        # === UI TEXTURES ===
        {"name": "tex_ui_coin_counter", "prompt": f"golden coin icon with sparkle, game UI element, circular, {style}", "size": 256},
        {"name": "tex_ui_score_bg", "prompt": f"dark semi-transparent rounded rectangle, game HUD background panel, {style}", "size": 256},
        {"name": "tex_ui_pause_icon", "prompt": f"white pause button icon, two vertical bars, game UI, {style}", "size": 128},
        
        # === ENVIRONMENT POLISH ===
        {"name": "tex_streetlight_pole", "prompt": f"cartoon street lamp post texture, dark grey metal, ornate design, {style}", "size": 256},
        {"name": "tex_fence_chain_link", "prompt": f"seamless tileable chain link fence texture, silver metal wire, {style}", "size": 256},
        {"name": "tex_ground_grass_hd", "prompt": f"seamless tileable bright green grass texture, top down view, lush cartoon lawn, {style}", "size": 512},
        {"name": "tex_ground_dirt_path", "prompt": f"seamless tileable brown dirt path texture, top down view, {style}", "size": 256},
        
        # === POWERUP/COLLECTIBLE TEXTURES ===
        {"name": "tex_powerup_coin_spin", "prompt": f"golden coin with star emblem, spinning animation sprite sheet 4 frames, game collectible, {style}", "size": 512},
        {"name": "tex_powerup_magnet_icon", "prompt": f"horseshoe magnet icon, red and blue, game powerup, glowing, {style}", "size": 256},
        {"name": "tex_powerup_shield_bubble", "prompt": f"transparent blue energy shield bubble, game powerup effect, glowing edges, {style}", "size": 256},
        
        # === TRAIN/SUBWAY TEXTURES ===
        {"name": "tex_train_front", "prompt": f"cartoon subway train front view, headlights on, colorful, approaching view, {style}", "size": 512},
        {"name": "tex_train_graffiti_hd", "prompt": f"subway train car side with colorful graffiti art, urban street art, {style}", "size": 1024},
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
    print(f"Generated {result} Phase 10 textures")
