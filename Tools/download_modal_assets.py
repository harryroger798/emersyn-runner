#!/usr/bin/env python3
"""Download generated assets from Modal Volume to local project."""
import modal
import os
import shutil

ASSET_MOUNT = "/assets"
VOLUME_NAME = "emersyn-runner-assets"

app = modal.App("emersyn-download-assets")
asset_volume = modal.Volume.from_name(VOLUME_NAME)

@app.function(
    volumes={ASSET_MOUNT: asset_volume},
    timeout=300,
)
def list_and_read_assets():
    """List all assets in the volume and return their contents."""
    import os
    assets = {}
    for root, dirs, files in os.walk(ASSET_MOUNT):
        for f in files:
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, ASSET_MOUNT)
            with open(full_path, "rb") as fh:
                assets[rel_path] = fh.read()
            print(f"  Found: {rel_path} ({len(assets[rel_path])} bytes)")
    return assets

@app.local_entrypoint()
def main():
    """Download all assets from Modal Volume."""
    print("=== Downloading assets from Modal Volume ===")
    assets = list_and_read_assets.remote()
    
    if not assets:
        print("No assets found in volume!")
        return
    
    # Project root
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    
    # Map Modal paths to Unity project paths
    path_mapping = {
        "textures/": "Assets/Textures/Generated/",
        "audio/music/": "Assets/Audio/Music/",
        "audio/sfx/": "Assets/Audio/SFX/",
        "models/": "Assets/Models/Generated/",
    }
    
    downloaded = 0
    for rel_path, data in assets.items():
        # Determine destination
        dest_dir = None
        for prefix, unity_dir in path_mapping.items():
            if rel_path.startswith(prefix):
                sub = rel_path[len(prefix):]
                dest_path = os.path.join(project_root, unity_dir, sub)
                break
        else:
            dest_path = os.path.join(project_root, "Assets/Generated/", rel_path)
        
        os.makedirs(os.path.dirname(dest_path), exist_ok=True)
        with open(dest_path, "wb") as f:
            f.write(data)
        print(f"  Saved: {dest_path} ({len(data)} bytes)")
        downloaded += 1
    
    print(f"\n=== Downloaded {downloaded} assets ===")

if __name__ == "__main__":
    main()
