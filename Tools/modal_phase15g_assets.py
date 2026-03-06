"""Phase 15G: Higher-quality procedural textures for visual polish.
Focus: better road grain, improved sidewalk, stylized clouds, enhanced ground details,
cleaner building facades, improved HUD backgrounds, better character textures.
All procedural (PIL-based) to avoid UV distortion on 3D primitives.
"""
import modal

app = modal.App("emersyn-phase15g")

image = modal.Image.debian_slim(python_version="3.11").pip_install("Pillow==10.4.0")

@app.function(image=image, timeout=300)
def generate_phase15g_textures():
    from PIL import Image, ImageDraw, ImageFilter, ImageFont
    import random
    import math
    import os

    out_dir = "/tmp/phase15g_textures"
    os.makedirs(out_dir, exist_ok=True)
    results = []

    # 1. Road with visible asphalt grain (512x512, dark grey with noise grain)
    img = Image.new("RGB", (512, 512))
    draw = ImageDraw.Draw(img)
    random.seed(42)
    for y in range(512):
        for x in range(512):
            base = 55 + random.randint(-8, 8)
            img.putpixel((x, y), (base, base, base + 2))
    # Add subtle crack lines
    for _ in range(15):
        x1, y1 = random.randint(0, 511), random.randint(0, 511)
        x2, y2 = x1 + random.randint(-80, 80), y1 + random.randint(-80, 80)
        draw.line([(x1, y1), (x2, y2)], fill=(40, 40, 42), width=1)
    img = img.filter(ImageFilter.GaussianBlur(0.5))
    path = os.path.join(out_dir, "tex_road_asphalt_grain.png")
    img.save(path)
    results.append(("tex_road_asphalt_grain.png", os.path.getsize(path)))

    # 2. Sidewalk with clear brick/paver pattern (512x512)
    img = Image.new("RGB", (512, 512))
    draw = ImageDraw.Draw(img)
    brick_w, brick_h = 64, 32
    colors = [(170, 160, 150), (165, 155, 145), (175, 165, 155), (160, 150, 140)]
    for row in range(512 // brick_h + 1):
        offset = (brick_w // 2) if row % 2 else 0
        for col in range(-1, 512 // brick_w + 2):
            x = col * brick_w + offset
            y = row * brick_h
            c = colors[random.randint(0, len(colors)-1)]
            c = (c[0] + random.randint(-5, 5), c[1] + random.randint(-5, 5), c[2] + random.randint(-5, 5))
            draw.rectangle([x+1, y+1, x+brick_w-2, y+brick_h-2], fill=c)
            # Grout lines
            draw.rectangle([x, y, x+brick_w, y], fill=(130, 125, 118))
            draw.rectangle([x, y, x, y+brick_h], fill=(130, 125, 118))
    path = os.path.join(out_dir, "tex_sidewalk_brick_clear.png")
    img.save(path)
    results.append(("tex_sidewalk_brick_clear.png", os.path.getsize(path)))

    # 3. Stylized cloud texture (256x256, soft white with blue tint edges)
    img = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = 128, 128
    for r in range(120, 0, -1):
        alpha = int(255 * (1.0 - (r / 120.0) ** 0.5))
        brightness = 240 + int(15 * (1.0 - r / 120.0))
        brightness = min(255, brightness)
        draw.ellipse([cx-r, cy-int(r*0.6), cx+r, cy+int(r*0.6)],
                     fill=(brightness, brightness, brightness, alpha))
    # Add puff details
    for _ in range(8):
        px = cx + random.randint(-60, 60)
        py = cy + random.randint(-30, 30)
        pr = random.randint(20, 50)
        for r2 in range(pr, 0, -1):
            a2 = int(200 * (1.0 - (r2 / pr) ** 0.6))
            draw.ellipse([px-r2, py-int(r2*0.7), px+r2, py+int(r2*0.7)],
                         fill=(250, 250, 252, a2))
    path = os.path.join(out_dir, "tex_cloud_stylized.png")
    img.save(path)
    results.append(("tex_cloud_stylized.png", os.path.getsize(path)))

    # 4. Enhanced manhole cover (256x256, detailed circular pattern)
    img = Image.new("RGB", (256, 256), (80, 80, 82))
    draw = ImageDraw.Draw(img)
    cx, cy = 128, 128
    # Outer ring
    draw.ellipse([20, 20, 236, 236], outline=(60, 60, 62), width=4)
    draw.ellipse([30, 30, 226, 226], outline=(70, 70, 72), width=2)
    # Cross-hatch pattern
    for angle in range(0, 360, 15):
        rad = math.radians(angle)
        x1 = cx + int(90 * math.cos(rad))
        y1 = cy + int(90 * math.sin(rad))
        x2 = cx + int(30 * math.cos(rad))
        y2 = cy + int(30 * math.sin(rad))
        draw.line([(x1, y1), (x2, y2)], fill=(65, 65, 67), width=2)
    # Center circle
    draw.ellipse([108, 108, 148, 148], fill=(70, 70, 72), outline=(55, 55, 57), width=2)
    # Text
    draw.text((100, 118), "CITY", fill=(90, 90, 92))
    path = os.path.join(out_dir, "tex_manhole_detailed.png")
    img.save(path)
    results.append(("tex_manhole_detailed.png", os.path.getsize(path)))

    # 5. Better curb/edge texture (256x64, stone edge)
    img = Image.new("RGB", (256, 64))
    draw = ImageDraw.Draw(img)
    for y in range(64):
        for x in range(256):
            if y < 8:
                v = 160 + random.randint(-5, 5)
            elif y < 16:
                v = 140 + random.randint(-5, 5)
            elif y < 48:
                v = 120 + random.randint(-8, 8)
            else:
                v = 100 + random.randint(-5, 5)
            img.putpixel((x, y), (v, v, v-3))
    path = os.path.join(out_dir, "tex_curb_stone_edge.png")
    img.save(path)
    results.append(("tex_curb_stone_edge.png", os.path.getsize(path)))

    # 6. Character shirt texture (128x128, gradient with collar detail)
    img = Image.new("RGB", (128, 128))
    for y in range(128):
        for x in range(128):
            # Blue shirt with lighter top
            r = max(0, min(255, 60 + int(20 * (1 - y/128))))
            g = max(0, min(255, 120 + int(40 * (1 - y/128))))
            b = max(0, min(255, 200 + int(30 * (1 - y/128))))
            img.putpixel((x, y), (r, g, b))
    draw = ImageDraw.Draw(img)
    # Collar
    draw.polygon([(50, 0), (64, 15), (78, 0)], fill=(50, 100, 180))
    # Pocket
    draw.rectangle([85, 60, 110, 85], outline=(50, 100, 180), width=1)
    path = os.path.join(out_dir, "tex_char_shirt_detail.png")
    img.save(path)
    results.append(("tex_char_shirt_detail.png", os.path.getsize(path)))

    # 7. Character pants texture (128x64, dark denim pattern)
    img = Image.new("RGB", (128, 64))
    for y in range(64):
        for x in range(128):
            base_r = 35 + random.randint(-3, 3)
            base_g = 35 + random.randint(-3, 3)
            base_b = 55 + random.randint(-3, 3)
            # Denim diagonal weave
            if (x + y) % 4 < 2:
                base_b += 5
            img.putpixel((x, y), (base_r, base_g, base_b))
    path = os.path.join(out_dir, "tex_char_pants_denim.png")
    img.save(path)
    results.append(("tex_char_pants_denim.png", os.path.getsize(path)))

    # 8. Enhanced score panel background (512x128, dark gradient with border)
    img = Image.new("RGBA", (512, 128))
    draw = ImageDraw.Draw(img)
    for y in range(128):
        alpha = int(200 * (1 - abs(y - 64) / 64))
        for x in range(512):
            r = int(20 + 15 * (x / 512))
            g = int(25 + 15 * (x / 512))
            b = int(40 + 20 * (x / 512))
            img.putpixel((x, y), (r, g, b, alpha))
    # Border glow
    draw.rectangle([0, 0, 511, 127], outline=(100, 180, 255, 150), width=2)
    draw.rectangle([2, 2, 509, 125], outline=(60, 120, 200, 80), width=1)
    path = os.path.join(out_dir, "tex_hud_score_panel.png")
    img.save(path)
    results.append(("tex_hud_score_panel.png", os.path.getsize(path)))

    # 9. Road marking - crosswalk (256x128, white stripes)
    img = Image.new("RGBA", (256, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    stripe_width = 24
    gap = 12
    y = 10
    while y < 118:
        draw.rectangle([20, y, 236, y + stripe_width], fill=(240, 240, 240, 220))
        y += stripe_width + gap
    path = os.path.join(out_dir, "tex_road_crosswalk.png")
    img.save(path)
    results.append(("tex_road_crosswalk.png", os.path.getsize(path)))

    # 10. Fence/railing texture (256x128, metal railing pattern)
    img = Image.new("RGBA", (256, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Top and bottom rails
    draw.rectangle([0, 5, 255, 12], fill=(140, 140, 145, 230))
    draw.rectangle([0, 115, 255, 122], fill=(140, 140, 145, 230))
    # Vertical bars
    for x in range(0, 256, 20):
        draw.rectangle([x, 5, x+4, 122], fill=(130, 130, 135, 220))
    path = os.path.join(out_dir, "tex_fence_railing.png")
    img.save(path)
    results.append(("tex_fence_railing.png", os.path.getsize(path)))

    # 11. Improved sky gradient (512x512, smoother blue gradient with horizon glow)
    img = Image.new("RGB", (512, 512))
    for y in range(512):
        t = y / 511.0
        # Top = deep blue, middle = light blue, bottom = warm horizon
        if t < 0.6:
            t2 = t / 0.6
            r = int(40 + 100 * t2)
            g = int(100 + 120 * t2)
            b = int(200 + 40 * t2)
        else:
            t2 = (t - 0.6) / 0.4
            r = int(140 + 80 * t2)
            g = int(220 + 20 * t2)
            b = int(240 - 20 * t2)
        for x in range(512):
            img.putpixel((x, y), (min(255, r), min(255, g), min(255, b)))
    path = os.path.join(out_dir, "tex_sky_gradient_smooth.png")
    img.save(path)
    results.append(("tex_sky_gradient_smooth.png", os.path.getsize(path)))

    # 12. Grass strip texture (256x256, green with variation)
    img = Image.new("RGB", (256, 256))
    random.seed(99)
    for y in range(256):
        for x in range(256):
            r = 45 + random.randint(-8, 8)
            g = 100 + random.randint(-15, 15)
            b = 35 + random.randint(-5, 5)
            img.putpixel((x, y), (r, g, b))
    # Add grass blade lines
    draw = ImageDraw.Draw(img)
    for _ in range(200):
        x = random.randint(0, 255)
        y = random.randint(0, 255)
        h = random.randint(5, 15)
        shade = random.randint(70, 130)
        draw.line([(x, y), (x + random.randint(-2, 2), y - h)], fill=(40, shade, 30), width=1)
    img = img.filter(ImageFilter.GaussianBlur(0.3))
    path = os.path.join(out_dir, "tex_grass_detailed.png")
    img.save(path)
    results.append(("tex_grass_detailed.png", os.path.getsize(path)))

    # 13. Improved coin texture (128x128, metallic gold with embossed edge)
    img = Image.new("RGB", (128, 128))
    draw = ImageDraw.Draw(img)
    cx, cy = 64, 64
    for y_pos in range(128):
        for x_pos in range(128):
            dx = x_pos - cx
            dy = y_pos - cy
            dist = math.sqrt(dx*dx + dy*dy)
            if dist < 58:
                # Gold gradient based on angle for metallic sheen
                angle = math.atan2(dy, dx)
                sheen = 0.5 + 0.5 * math.sin(angle * 2 + dist * 0.05)
                r = int(220 + 35 * sheen)
                g = int(180 + 40 * sheen)
                b = int(40 + 30 * sheen)
                if dist > 50:  # Edge ring
                    r = int(r * 0.7)
                    g = int(g * 0.7)
                    b = int(b * 0.6)
                img.putpixel((x_pos, y_pos), (min(255, r), min(255, g), min(255, b)))
            else:
                img.putpixel((x_pos, y_pos), (200, 160, 30))
    # Center emboss
    draw.ellipse([44, 44, 84, 84], outline=(180, 140, 20), width=2)
    draw.text((52, 52), "$", fill=(240, 200, 60))
    path = os.path.join(out_dir, "tex_coin_embossed_gold.png")
    img.save(path)
    results.append(("tex_coin_embossed_gold.png", os.path.getsize(path)))

    # 14. Building facade clean (512x512, modern apartment without rainbow artifacts)
    img = Image.new("RGB", (512, 512))
    draw = ImageDraw.Draw(img)
    # Base wall color
    base_colors = [(180, 175, 165), (170, 165, 160), (190, 180, 170)]
    base = base_colors[0]
    draw.rectangle([0, 0, 511, 511], fill=base)
    # Windows grid
    win_w, win_h = 40, 50
    gap_x, gap_y = 20, 25
    start_x, start_y = 30, 30
    for row in range(6):
        for col in range(7):
            wx = start_x + col * (win_w + gap_x)
            wy = start_y + row * (win_h + gap_y)
            # Window frame
            draw.rectangle([wx-2, wy-2, wx+win_w+2, wy+win_h+2], fill=(100, 100, 105))
            # Glass - slightly different blue tints
            blue_var = random.randint(-15, 15)
            draw.rectangle([wx, wy, wx+win_w, wy+win_h],
                          fill=(120+blue_var, 150+blue_var, 190+blue_var))
            # Reflection highlight
            draw.rectangle([wx+2, wy+2, wx+15, wy+20],
                          fill=(160+blue_var, 190+blue_var, 220+blue_var))
    # Ground floor
    draw.rectangle([0, 440, 511, 511], fill=(140, 135, 130))
    # Door
    draw.rectangle([220, 450, 290, 511], fill=(100, 70, 50))
    draw.rectangle([225, 455, 285, 506], fill=(120, 85, 60))
    path = os.path.join(out_dir, "tex_building_apartment_clean.png")
    img.save(path)
    results.append(("tex_building_apartment_clean.png", os.path.getsize(path)))

    # 15. Rooftop texture (256x256, flat grey with vents)
    img = Image.new("RGB", (256, 256))
    draw = ImageDraw.Draw(img)
    for y in range(256):
        for x in range(256):
            v = 115 + random.randint(-5, 5)
            img.putpixel((x, y), (v, v, v+2))
    # Vent boxes
    draw.rectangle([40, 60, 80, 90], fill=(90, 90, 92), outline=(80, 80, 82))
    draw.rectangle([150, 120, 200, 160], fill=(85, 85, 88), outline=(75, 75, 78))
    # Pipes
    draw.rectangle([100, 30, 108, 100], fill=(100, 100, 105))
    path = os.path.join(out_dir, "tex_rooftop_detail.png")
    img.save(path)
    results.append(("tex_rooftop_detail.png", os.path.getsize(path)))

    # 16. Improved game over overlay (512x512, dramatic dark gradient with vignette)
    img = Image.new("RGBA", (512, 512))
    cx, cy = 256, 256
    for y in range(512):
        for x in range(512):
            dx = (x - cx) / 256.0
            dy = (y - cy) / 256.0
            dist = math.sqrt(dx*dx + dy*dy)
            # Vignette: dark at edges, slightly lighter center
            vignette = min(1.0, dist * 0.8)
            r = int(15 + 25 * (1 - vignette))
            g = int(10 + 20 * (1 - vignette))
            b = int(30 + 30 * (1 - vignette))
            a = int(180 + 75 * vignette)
            img.putpixel((x, y), (r, g, b, min(255, a)))
    path = os.path.join(out_dir, "tex_gameover_vignette.png")
    img.save(path)
    results.append(("tex_gameover_vignette.png", os.path.getsize(path)))

    return results

@app.local_entrypoint()
def main():
    import shutil
    results = generate_phase15g_textures.remote()
    print(f"Generated {len(results)} Phase 15G textures:")
    for name, size in results:
        print(f"  {name}: {size/1024:.1f} KB")

    # Download from Modal volume
    local_dir = "Assets/Resources/Textures"
    vol_dir = "/tmp/phase15g_textures"

    # The files are generated remotely, we need to get them via output
    print("\nTextures generated on remote. Downloading...")
