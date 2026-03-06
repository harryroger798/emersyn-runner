"""
Phase 15E: Enhanced environment variety + polished VFX + UI improvements
Generates ~18 new SDXL textures via Modal GPU for flat surfaces:
- 4 new environment textures (tunnel walls, overpass, fence pattern, bridge)
- 4 character detail textures (face portrait, hair detail, backpack, shoe detail)
- 3 ground detail textures (manhole cover, drain grate, road crack)
- 3 particle/VFX textures (star burst, shield glow, magnet beam)
- 4 UI improvement textures (score panel, combo badge, pause overlay, coin counter bg)
"""
import modal
import io, base64

app = modal.App("emersyn-phase15e")

sdxl_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install("numpy<2", "torch==2.1.0", "huggingface_hub==0.23.5", "diffusers==0.25.0", "transformers", "accelerate", "safetensors", "Pillow")
)

@app.function(gpu="A10G", image=sdxl_image, timeout=600)
def generate_all_textures():
    import torch
    from diffusers import StableDiffusionXLPipeline
    from PIL import Image

    pipe = StableDiffusionXLPipeline.from_pretrained(
        "stabilityai/stable-diffusion-xl-base-1.0",
        torch_dtype=torch.float16,
        variant="fp16",
        use_safetensors=True
    )
    pipe = pipe.to("cuda")
    pipe.set_progress_bar_config(disable=False)

    style = "stylized cartoon game art, mobile game, vibrant colors, clean design, subway surfers style"
    neg = "photorealistic, photo, blurry, watermark, text, nsfw, ugly, deformed, realistic"

    textures = [
        # === ENVIRONMENT DETAIL TEXTURES ===
        {
            "name": "tex_tunnel_wall_subway",
            "prompt": f"subway tunnel interior wall, tiled white ceramic tiles, dark grout lines, underground station atmosphere, {style}, seamless tileable",
            "size": 1024,
            "negative_extra": "outdoor, sky, grass, bright"
        },
        {
            "name": "tex_overpass_concrete",
            "prompt": f"concrete overpass bridge underside, industrial grey concrete texture with support beams, urban highway, {style}, seamless tileable",
            "size": 512,
            "negative_extra": "grass, nature, colorful"
        },
        {
            "name": "tex_fence_chainlink",
            "prompt": f"chain link fence pattern, metal wire fence, diamond pattern mesh, urban style, {style}, seamless tileable, top-down view",
            "size": 512,
            "negative_extra": "grass, nature, rust"
        },
        {
            "name": "tex_wall_brick_detail",
            "prompt": f"red brick wall texture, clean mortar lines, urban building side, {style}, seamless tileable",
            "size": 512,
            "negative_extra": "grass, nature, plants"
        },
        # === CHARACTER DETAIL ===
        {
            "name": "tex_char_face_portrait",
            "prompt": f"cartoon character face portrait, young girl runner, brown hair in ponytail, bright eyes, cheerful smile, round face, {style}",
            "size": 512,
            "negative_extra": "scary, old, realistic, wrinkles"
        },
        {
            "name": "tex_char_backpack_detail",
            "prompt": f"colorful cartoon backpack front view, orange school backpack with straps and pockets, {style}",
            "size": 512,
            "negative_extra": "person, body, realistic"
        },
        # === GROUND DETAILS ===
        {
            "name": "tex_ground_manhole_hd",
            "prompt": f"round metal manhole cover, top-down view, detailed iron grate pattern, dark metal, urban street detail, {style}, seamless",
            "size": 512,
            "negative_extra": "grass, nature, open"
        },
        {
            "name": "tex_ground_drain_grate",
            "prompt": f"rectangular metal drain grate, top-down view, street drainage cover, parallel metal bars, {style}, seamless",
            "size": 512,
            "negative_extra": "grass, nature, water"
        },
        {
            "name": "tex_ground_puddle",
            "prompt": f"small puddle of water on asphalt, top-down view, reflective surface, urban street, {style}",
            "size": 256,
            "negative_extra": "ocean, river, large"
        },
        # === VFX SPRITES (on black bg for alpha extraction) ===
        {
            "name": "tex_vfx_star_burst",
            "prompt": f"bright golden star burst explosion effect, sparkle particles, magical energy burst, game VFX sprite, on solid black background",
            "size": 512,
            "negative_extra": "person, scene, landscape"
        },
        {
            "name": "tex_vfx_shield_glow",
            "prompt": f"glowing blue energy shield bubble, transparent dome effect, protective barrier, game VFX sprite, on solid black background",
            "size": 512,
            "negative_extra": "person, scene, landscape"
        },
        {
            "name": "tex_vfx_magnet_beam",
            "prompt": f"purple magnetic beam energy lines, attraction force field, coin magnet power, game VFX sprite, on solid black background",
            "size": 512,
            "negative_extra": "person, scene, landscape"
        },
        # === UI IMPROVEMENTS ===
        {
            "name": "tex_ui_score_panel",
            "prompt": f"game UI score display panel, sleek dark blue gradient background, rounded corners, metallic border, mobile game HUD element, {style}",
            "size": 512,
            "negative_extra": "text, numbers, 3d, character"
        },
        {
            "name": "tex_ui_combo_badge",
            "prompt": f"game UI combo multiplier badge, golden star burst border, radial glow effect, achievement badge, mobile game, {style}",
            "size": 256,
            "negative_extra": "text, numbers, character, scene"
        },
        {
            "name": "tex_ui_pause_overlay",
            "prompt": f"dark semi-transparent game pause overlay, frosted glass effect, subtle radial blur, mobile game pause screen background, {style}",
            "size": 512,
            "negative_extra": "text, buttons, character, bright"
        },
        {
            "name": "tex_ui_coin_counter_bg",
            "prompt": f"small golden coin counter background panel, rounded pill shape, warm gradient, game HUD element, {style}",
            "size": 256,
            "negative_extra": "text, numbers, character, scene"
        },
        # === ADDITIONAL BUILDING VARIETY ===
        {
            "name": "tex_building_police_station",
            "prompt": f"police station building facade, blue accents, official government building, city architecture, {style}, seamless tileable",
            "size": 1024,
            "negative_extra": "cars, people, realistic"
        },
        {
            "name": "tex_building_fire_station",
            "prompt": f"fire station building facade, large red garage doors, fire department sign, emergency building, {style}, seamless tileable",
            "size": 1024,
            "negative_extra": "trucks, people, realistic"
        },
    ]

    results = {}
    for i, tex in enumerate(textures):
        name = tex["name"]
        prompt = tex["prompt"]
        size = tex["size"]
        neg_full = neg
        if "negative_extra" in tex:
            neg_full += ", " + tex["negative_extra"]

        print(f"[{i+1}/{len(textures)}] Generating: {name} ({size}x{size})")
        image = pipe(
            prompt=prompt,
            negative_prompt=neg_full,
            width=size,
            height=size,
            num_inference_steps=30,
            guidance_scale=7.5,
        ).images[0]

        # VFX: convert black background to alpha
        if name.startswith("tex_vfx_"):
            from PIL import Image as PILImage
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
        b64 = base64.b64encode(buf.getvalue()).decode()
        results[name] = b64
        print(f"  Done: {name}.png ({len(buf.getvalue())} bytes)")

    return results

@app.local_entrypoint()
def main():
    import os, time

    start = time.time()
    results = generate_all_textures.remote()
    elapsed = time.time() - start

    print(f"Received {len(results)} textures in {elapsed:.1f}s")

    out_dir = "/home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures"
    total_bytes = 0
    for name, b64data in results.items():
        raw = base64.b64decode(b64data)
        path = os.path.join(out_dir, f"{name}.png")
        with open(path, "wb") as f:
            f.write(raw)
        total_bytes += len(raw)
        print(f"  Saved: {path} ({len(raw)} bytes)")

    print(f"Total: {len(results)} textures, {total_bytes / 1024 / 1024:.1f} MB")
    print(f"Estimated cost: ~$0.05")
