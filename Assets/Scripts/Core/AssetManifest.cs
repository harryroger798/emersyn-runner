using UnityEngine;

namespace EmersynRunner.Core
{
    /// <summary>
    /// Central manifest that maps all Modal-generated assets to Unity references.
    /// Attach to a persistent GameObject in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetManifest", menuName = "Emersyn Runner/Asset Manifest")]
    public class AssetManifest : ScriptableObject
    {
        [Header("Player Textures")]
        public Texture2D playerDiffuse;
        public Texture2D playerNormal;

        [Header("Environment Textures - City")]
        public Texture2D cityGround;
        public Texture2D cityWall;
        public Texture2D cityRail;

        [Header("Environment Textures - Jungle")]
        public Texture2D jungleGround;
        public Texture2D jungleWall;
        public Texture2D jungleRuins;

        [Header("Environment Textures - Candy")]
        public Texture2D candyGround;
        public Texture2D candyWall;

        [Header("Obstacle Textures")]
        public Texture2D barrierTexture;
        public Texture2D crateTexture;
        public Texture2D trainTexture;

        [Header("Pickup Textures")]
        public Texture2D coinTexture;
        public Texture2D magnetTexture;
        public Texture2D multiplierTexture;
        public Texture2D shieldTexture;

        [Header("Skybox Textures")]
        public Texture2D citySkybox;
        public Texture2D jungleSkybox;
        public Texture2D candySkybox;

        [Header("UI Textures")]
        public Texture2D titleLogo;
        public Texture2D buttonPrimary;
        public Texture2D pauseIcon;
        public Texture2D heartIcon;
        public Texture2D menuBackground;

        [Header("Audio - Music")]
        public AudioClip menuMusicLoop;
        public AudioClip gameplayMusicLoop;

        [Header("Audio - SFX")]
        public AudioClip sfxWhoosh;
        public AudioClip sfxJump;
        public AudioClip sfxRoll;
        public AudioClip sfxCoinPickup;
        public AudioClip sfxPowerupPickup;
        public AudioClip sfxShieldActivate;
        public AudioClip sfxHit;
        public AudioClip sfxGameOver;
        public AudioClip sfxUIClick;
        public AudioClip sfxComboStinger;

        [Header("3D Models")]
        public Mesh barrierMesh;
        public Mesh overheadMesh;
        public Mesh lowObstacleMesh;
        public Mesh trainMesh;
        public Mesh laneBlockerMesh;
        public Mesh coinMesh;
        public Mesh powerupCapsuleMesh;
        public Mesh trackSegmentMesh;
        public Mesh cityBuildingMesh;
        public Mesh jungleTreeMesh;
        public Mesh candyCaneMesh;
        public Mesh emersynPlaceholderMesh;
    }
}
