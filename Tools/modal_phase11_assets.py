import modal
import hashlib

app = modal.App("emersyn-phase11-assets")
volume = modal.Volume.from_name("emersyn-phase11-assets", create_if_missing=True)

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
        # === CHARACTER BODY ATLAS (single coherent texture) ===
        {"name": "tex_emersyn_torso_blue", "prompt": f"cartoon character blue hoodie torso texture map, front view, zipper detail, wrinkles, bright blue fabric, character model UV map, {style}", "size": 512},
        {"name": "tex_emersyn_torso_red", "prompt": f"cartoon character red hoodie torso texture map, front view, logo detail, bright red fabric, character model UV map, {style}", "size": 512},
        {"name": "tex_emersyn_torso_green", "prompt": f"cartoon character green bomber jacket torso texture map, front view, patches, character model UV map, {style}", "size": 512},
        
        # === IMPROVED GROUND/ENVIRONMENT ===
        {"name": "tex_ground_concrete", "prompt": f"seamless tileable concrete pavement texture, light grey, clean urban sidewalk, top down view, {style}", "size": 512},
        {"name": "tex_road_asphalt_clean", "prompt": f"seamless tileable clean dark asphalt road texture, smooth surface, top down view, {style}", "size": 512},
        
        # === MORE BUILDING VARIETY ===
        {"name": "tex_building_pizzeria", "prompt": f"seamless tileable pizzeria restaurant facade, red awning, pizza sign, warm lighting, Italian style, {style}", "size": 1024},
        {"name": "tex_building_bank", "prompt": f"seamless tileable bank building facade, marble columns, large windows, prestigious, {style}", "size": 1024},
        {"name": "tex_building_laundromat", "prompt": f"seamless tileable laundromat storefront, glass windows showing washers, neon open sign, {style}", "size": 1024},
        {"name": "tex_building_music_shop", "prompt": f"seamless tileable music shop facade, guitars in window, neon notes, colorful, {style}", "size": 1024},
        
        # === PARTICLE/VFX TEXTURES ===
        {"name": "tex_vfx_sparkle", "prompt": f"golden sparkle particle effect sprite, glowing star burst, transparent background, game VFX, {style}", "size": 256},
        {"name": "tex_vfx_speed_line", "prompt": f"white speed motion blur line, horizontal streak, transparent background, game VFX, {style}", "size": 128},
        {"name": "tex_vfx_coin_collect", "prompt": f"golden coin burst explosion effect, sparkles flying outward, game collectible VFX, {style}", "size": 256},
        
        # === IMPROVED OBSTACLE TEXTURES ===
        {"name": "tex_obstacle_roadblock", "prompt": f"orange and white striped road barrier barricade, construction safety, front view, {style}", "size": 512},
        {"name": "tex_obstacle_trash_pile", "prompt": f"pile of colorful trash bags and boxes, urban debris, front view, {style}", "size": 512},
        
        # === IMPROVED PROP TEXTURES ===  
        {"name": "tex_prop_food_cart", "prompt": f"colorful street food cart vendor, hot dog stand, umbrella, wheels, side view, {style}", "size": 512},
        {"name": "tex_prop_bus_stop", "prompt": f"glass bus stop shelter with bench, advertisement poster, side view, {style}", "size": 512},
        {"name": "tex_prop_traffic_light", "prompt": f"traffic signal light, three colored circles red yellow green, front view, {style}", "size": 256},
        
        # === UI POLISH ===
        {"name": "tex_ui_heart_icon", "prompt": f"red heart icon with white outline, game UI life indicator, {style}", "size": 128},
        {"name": "tex_ui_star_icon", "prompt": f"golden star icon with sparkle, game UI rating element, {style}", "size": 128},
        {"name": "tex_ui_play_button", "prompt": f"green play button with white triangle arrow, rounded rectangle, game UI, {style}", "size": 256},
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
    print(f"Generated {result} Phase 11 textures")
