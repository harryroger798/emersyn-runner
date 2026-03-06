"""
Phase 15F: Visual gap fixes — better road, train tracks, obstacle details, improved lighting
Targeting the biggest visual gaps vs Subway Surfers:
1. Better road texture (less wavy, cleaner asphalt)
2. Train track texture for ground
3. Better obstacle textures (subway train car, taxi cab)
4. Improved sidewalk with pattern
5. Better sky gradient (deeper blue, sunset option)
6. Gravel/ballast for train tracks
7. Warning stripe texture for barriers
8. Improved lamp post detail
"""
import modal
import io
import base64

app = modal.App("phase15f-assets")

image = (
    modal.Image.debian_slim(python_version="3.10")
    .pip_install(
        "Pillow>=9.0",
        "numpy<2",
    )
)

@app.function(image=image, timeout=120)
def generate_textures():
    from PIL import Image, ImageDraw, ImageFilter, ImageFont
    import numpy as np
    import random
    
    results = {}
    
    # 1. Clean dark asphalt road (less wavy, smoother)
    def make_road_smooth():
        img = Image.new("RGB", (512, 512))
        pixels = np.zeros((512, 512, 3), dtype=np.uint8)
        for y in range(512):
            for x in range(512):
                base = 55 + random.randint(-3, 3)
                pixels[y, x] = [base, base, base + 2]
        img = Image.fromarray(pixels)
        img = img.filter(ImageFilter.GaussianBlur(2))
        draw = ImageDraw.Draw(img)
        # Subtle aggregate spots
        for _ in range(200):
            sx, sy = random.randint(0, 511), random.randint(0, 511)
            c = random.randint(45, 65)
            draw.ellipse([sx-1, sy-1, sx+1, sy+1], fill=(c, c, c))
        return img
    results["tex_road_smooth_dark"] = make_road_smooth()
    
    # 2. Train track rails texture (for flat quad on ground)
    def make_train_tracks():
        img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        # Gravel/ballast background
        for y in range(512):
            for x in range(512):
                if random.random() < 0.3:
                    c = random.randint(90, 130)
                    draw.point((x, y), fill=(c, c-5, c-10, 255))
        # Wooden sleepers (ties)
        for i in range(0, 512, 40):
            c = random.randint(80, 100)
            draw.rectangle([100, i, 412, i+15], fill=(c+20, c, c-15, 255))
        # Steel rails
        for rail_x in [150, 360]:
            draw.rectangle([rail_x, 0, rail_x+12, 512], fill=(160, 160, 165, 255))
            # Rail highlight
            draw.rectangle([rail_x+2, 0, rail_x+4, 512], fill=(190, 190, 200, 255))
        return img.convert("RGB")
    results["tex_ground_train_tracks"] = make_train_tracks()
    
    # 3. Subway train car side (for obstacle flat quad overlay)
    def make_subway_train():
        img = Image.new("RGB", (1024, 512), (50, 80, 140))
        draw = ImageDraw.Draw(img)
        # Body stripe
        draw.rectangle([0, 180, 1024, 320], fill=(220, 220, 230))
        # Windows
        for wx in range(40, 1000, 120):
            draw.rectangle([wx, 200, wx+80, 290], fill=(150, 200, 240))
            # Window frame
            draw.rectangle([wx-2, 198, wx+82, 200], fill=(40, 40, 50))
            draw.rectangle([wx-2, 290, wx+82, 292], fill=(40, 40, 50))
        # Door
        draw.rectangle([480, 180, 544, 380], fill=(180, 180, 190))
        draw.line([512, 180, 512, 380], fill=(100, 100, 110), width=2)
        # Bottom stripe
        draw.rectangle([0, 400, 1024, 512], fill=(60, 60, 70))
        # Top stripe
        draw.rectangle([0, 0, 1024, 30], fill=(60, 60, 70))
        # Route indicator
        draw.rectangle([60, 50, 160, 150], fill=(230, 60, 40))
        draw.rectangle([70, 60, 150, 140], fill=(230, 60, 40))
        return img
    results["tex_obstacle_subway_train"] = make_subway_train()
    
    # 4. Yellow taxi cab side texture
    def make_taxi():
        img = Image.new("RGB", (512, 256), (240, 200, 40))
        draw = ImageDraw.Draw(img)
        # Windows
        draw.rectangle([80, 30, 200, 100], fill=(150, 200, 240))
        draw.rectangle([220, 30, 380, 100], fill=(150, 200, 240))
        # Checker stripe
        for cx in range(0, 512, 20):
            if (cx // 20) % 2 == 0:
                draw.rectangle([cx, 120, cx+20, 140], fill=(30, 30, 30))
        # Wheels
        draw.ellipse([60, 180, 130, 250], fill=(30, 30, 35))
        draw.ellipse([75, 195, 115, 235], fill=(100, 100, 105))
        draw.ellipse([350, 180, 420, 250], fill=(30, 30, 35))
        draw.ellipse([365, 195, 405, 235], fill=(100, 100, 105))
        # Bottom
        draw.rectangle([0, 250, 512, 256], fill=(30, 30, 30))
        # Roof light
        draw.rectangle([230, 0, 280, 20], fill=(255, 255, 200))
        return img
    results["tex_obstacle_taxi"] = make_taxi()
    
    # 5. Patterned sidewalk (brick/paver pattern)
    def make_sidewalk_paver():
        img = Image.new("RGB", (512, 512))
        pixels = np.zeros((512, 512, 3), dtype=np.uint8)
        for y in range(512):
            for x in range(512):
                # Brick-like pattern
                bx = x % 64
                by = y % 32
                offset = 32 if (y // 32) % 2 == 1 else 0
                bx = (x + offset) % 64
                is_grout = bx < 2 or by < 2
                if is_grout:
                    pixels[y, x] = [140, 135, 130]
                else:
                    base = 165 + random.randint(-5, 5)
                    pixels[y, x] = [base, base-2, base-5]
        img = Image.fromarray(pixels)
        img = img.filter(ImageFilter.GaussianBlur(1))
        return img
    results["tex_sidewalk_paver"] = make_sidewalk_paver()
    
    # 6. Warning stripe barrier texture
    def make_warning_barrier():
        img = Image.new("RGB", (512, 256), (230, 180, 30))
        draw = ImageDraw.Draw(img)
        # Diagonal warning stripes
        for i in range(-512, 1024, 60):
            draw.polygon([(i, 0), (i+30, 0), (i+30+256, 256), (i+256, 256)], fill=(30, 30, 30))
        return img
    results["tex_obstacle_warning_barrier"] = make_warning_barrier()
    
    # 7. Better sky gradient (deeper blue with soft clouds)
    def make_sky_deep():
        img = Image.new("RGB", (512, 1024))
        pixels = np.zeros((1024, 512, 3), dtype=np.uint8)
        for y in range(1024):
            t = y / 1024.0
            r = int(80 + t * 140)
            g = int(150 + t * 80)
            b = int(230 - t * 30)
            for x in range(512):
                pixels[y, x] = [r + random.randint(-2, 2), g + random.randint(-2, 2), b + random.randint(-2, 2)]
        img = Image.fromarray(pixels)
        img = img.filter(ImageFilter.GaussianBlur(3))
        return img
    results["tex_sky_deep_blue"] = make_sky_deep()
    
    # 8. Gravel/ballast texture for track bed
    def make_gravel():
        img = Image.new("RGB", (512, 512))
        pixels = np.zeros((512, 512, 3), dtype=np.uint8)
        for y in range(512):
            for x in range(512):
                c = random.randint(100, 150)
                pixels[y, x] = [c, c-5, c-10]
        img = Image.fromarray(pixels)
        img = img.filter(ImageFilter.GaussianBlur(1.5))
        # Add larger stones
        draw = ImageDraw.Draw(img)
        for _ in range(500):
            sx, sy = random.randint(0, 511), random.randint(0, 511)
            size = random.randint(3, 8)
            c = random.randint(80, 160)
            draw.ellipse([sx, sy, sx+size, sy+size], fill=(c, c-3, c-8))
        return img
    results["tex_ground_gravel"] = make_gravel()
    
    # 9. Lamp post detail texture (metallic)
    def make_lamp_post():
        img = Image.new("RGB", (128, 512))
        pixels = np.zeros((512, 128, 3), dtype=np.uint8)
        for y in range(512):
            for x in range(128):
                # Cylindrical shading
                cx = abs(x - 64) / 64.0
                shade = int(120 - cx * 60)
                pixels[y, x] = [shade, shade, shade + 5]
        img = Image.fromarray(pixels)
        # Light at top
        draw = ImageDraw.Draw(img)
        draw.ellipse([20, 0, 108, 50], fill=(255, 240, 180))
        draw.ellipse([30, 5, 98, 40], fill=(255, 255, 220))
        return img
    results["tex_prop_lamp_detail"] = make_lamp_post()
    
    # 10. Road lane divider (dashed white line)
    def make_lane_divider():
        img = Image.new("RGBA", (64, 512), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        for y in range(0, 512, 80):
            draw.rectangle([20, y, 44, y+40], fill=(240, 240, 240, 255))
        return img.convert("RGB")
    results["tex_road_lane_divider"] = make_lane_divider()
    
    # 11. Coin shine effect (golden with highlights)
    def make_coin_shine():
        img = Image.new("RGB", (256, 256), (200, 160, 30))
        draw = ImageDraw.Draw(img)
        # Circular coin
        draw.ellipse([10, 10, 246, 246], fill=(220, 180, 40))
        draw.ellipse([20, 20, 236, 236], fill=(240, 200, 50))
        # Inner ring
        draw.ellipse([40, 40, 216, 216], outline=(200, 160, 30), width=3)
        # Dollar sign or star
        draw.ellipse([80, 80, 176, 176], fill=(250, 210, 60))
        # Shine highlight
        draw.ellipse([60, 40, 140, 100], fill=(255, 240, 140))
        return img
    results["tex_coin_golden_shine"] = make_coin_shine()
    
    # 12. Bus side texture
    def make_bus():
        img = Image.new("RGB", (1024, 512), (180, 30, 30))
        draw = ImageDraw.Draw(img)
        # Windows
        for wx in range(80, 950, 100):
            draw.rectangle([wx, 100, wx+70, 250], fill=(150, 200, 240))
            draw.rectangle([wx, 100, wx+70, 110], fill=(40, 40, 50))
        # White stripe
        draw.rectangle([0, 280, 1024, 320], fill=(240, 240, 240))
        # Bottom
        draw.rectangle([0, 420, 1024, 512], fill=(40, 40, 45))
        # Wheels
        draw.ellipse([100, 400, 200, 500], fill=(30, 30, 35))
        draw.ellipse([120, 420, 180, 480], fill=(90, 90, 95))
        draw.ellipse([800, 400, 900, 500], fill=(30, 30, 35))
        draw.ellipse([820, 420, 880, 480], fill=(90, 90, 95))
        # Route number
        draw.rectangle([20, 60, 70, 90], fill=(255, 200, 40))
        return img
    results["tex_obstacle_city_bus"] = make_bus()
    
    # 13. Construction barrier with lights
    def make_construction_barrier():
        img = Image.new("RGB", (512, 256), (255, 140, 0))
        draw = ImageDraw.Draw(img)
        # Diagonal stripes
        for i in range(-256, 768, 50):
            draw.polygon([(i, 0), (i+25, 0), (i+25+256, 256), (i+256, 256)], fill=(255, 255, 255))
        # Warning lights
        draw.ellipse([30, 20, 80, 70], fill=(255, 50, 30))
        draw.ellipse([430, 20, 480, 70], fill=(255, 50, 30))
        return img
    results["tex_obstacle_construction_barrier"] = make_construction_barrier()
    
    # 14. Powerup magnet icon
    def make_magnet_icon():
        img = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        # U-shaped magnet
        draw.arc([40, 40, 216, 250], 0, 180, fill=(220, 30, 30, 255), width=30)
        draw.rectangle([40, 40, 70, 140], fill=(220, 30, 30, 255))
        draw.rectangle([186, 40, 216, 140], fill=(220, 30, 30, 255))
        # Silver tips
        draw.rectangle([40, 40, 70, 70], fill=(200, 200, 210, 255))
        draw.rectangle([186, 40, 216, 70], fill=(200, 200, 210, 255))
        # Glow
        draw.ellipse([80, 80, 176, 200], fill=(255, 100, 80, 80))
        return img.convert("RGB")
    results["tex_powerup_magnet"] = make_magnet_icon()
    
    # 15. Speed boost icon
    def make_speed_icon():
        img = Image.new("RGB", (256, 256), (20, 20, 30))
        draw = ImageDraw.Draw(img)
        # Lightning bolt
        points = [(140, 20), (80, 120), (130, 120), (100, 240), (180, 110), (130, 110)]
        draw.polygon(points, fill=(255, 220, 40))
        # Glow
        draw.ellipse([60, 40, 200, 220], fill=(255, 220, 40), outline=None)
        # Re-draw bolt on top
        draw.polygon(points, fill=(255, 240, 80))
        return img
    results["tex_powerup_speed"] = make_speed_icon()
    
    # 16. Game over screen background
    def make_game_over_bg():
        img = Image.new("RGB", (512, 1024))
        pixels = np.zeros((1024, 512, 3), dtype=np.uint8)
        for y in range(1024):
            t = y / 1024.0
            r = int(40 + t * 60)
            g = int(10 + t * 20)
            b = int(60 + t * 40)
            for x in range(512):
                pixels[y, x] = [r, g, b]
        img = Image.fromarray(pixels)
        img = img.filter(ImageFilter.GaussianBlur(5))
        return img
    results["tex_ui_gameover_bg"] = make_game_over_bg()

    # Convert all to bytes
    output = {}
    for name, img in results.items():
        buf = io.BytesIO()
        img.save(buf, format="PNG", optimize=True)
        output[name] = base64.b64encode(buf.getvalue()).decode()
    
    return output


@app.local_entrypoint()
def main():
    import os
    
    print("Phase 15F: Generating 16 visual-gap-fix textures...")
    result = generate_textures.remote()
    
    out_dir = "/home/ubuntu/repos/emersyn-runner/Assets/Resources/Textures"
    os.makedirs(out_dir, exist_ok=True)
    
    total_size = 0
    for name, b64data in result.items():
        data = base64.b64decode(b64data)
        path = os.path.join(out_dir, f"{name}.png")
        with open(path, "wb") as f:
            f.write(data)
        total_size += len(data)
        print(f"  Saved: {name}.png ({len(data)/1024:.1f} KB)")
    
    print(f"\nTotal: {len(result)} textures, {total_size/1024/1024:.1f} MB")
    print("Phase 15F asset generation complete!")
