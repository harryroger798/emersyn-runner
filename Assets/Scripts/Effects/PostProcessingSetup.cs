using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EmersynRunner.Effects
{
    /// <summary>
    /// Sets up URP post-processing effects at runtime.
    /// Configures bloom, color grading, and vignette for polished visuals.
    /// </summary>
    public class PostProcessingSetup : MonoBehaviour
    {
        [Header("Bloom Settings")]
        [SerializeField] private float bloomThreshold = 0.9f;
        [SerializeField] private float bloomIntensity = 0.3f;
        [SerializeField] private float bloomScatter = 0.7f;

        [Header("Color Grading")]
        [SerializeField] private float colorTemperature = 0f;
        [SerializeField] private float colorTint = 0f;
        [SerializeField] private float saturation = 10f;
        [SerializeField] private float contrast = 10f;

        [Header("Vignette")]
        [SerializeField] private float vignetteIntensity = 0.25f;
        [SerializeField] private float vignetteSmoothness = 0.4f;

        [Header("Motion Blur (Optional)")]
        [SerializeField] private bool enableMotionBlur = false;
        [SerializeField] private float motionBlurIntensity = 0.15f;

        private Volume _volume;
        private VolumeProfile _profile;

        private void Start()
        {
            SetupPostProcessing();
        }

        private void SetupPostProcessing()
        {
            // Find or create volume
            _volume = GetComponent<Volume>();
            if (_volume == null)
            {
                _volume = gameObject.AddComponent<Volume>();
            }
            _volume.isGlobal = true;
            _volume.priority = 1;

            // Create new profile
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _profile;

            SetupBloom();
            SetupColorGrading();
            SetupVignette();

            if (enableMotionBlur)
            {
                SetupMotionBlur();
            }

            Debug.Log("[PostProcessingSetup] URP post-processing configured");
        }

        private void SetupBloom()
        {
            Bloom bloom = _profile.Add<Bloom>(true);
            bloom.threshold.value = bloomThreshold;
            bloom.intensity.value = bloomIntensity;
            bloom.scatter.value = bloomScatter;
            bloom.threshold.overrideState = true;
            bloom.intensity.overrideState = true;
            bloom.scatter.overrideState = true;
        }

        private void SetupColorGrading()
        {
            ColorAdjustments colorAdjustments = _profile.Add<ColorAdjustments>(true);
            colorAdjustments.colorFilter.value = Color.white;
            colorAdjustments.saturation.value = saturation;
            colorAdjustments.contrast.value = contrast;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.contrast.overrideState = true;

            WhiteBalance wb = _profile.Add<WhiteBalance>(true);
            wb.temperature.value = colorTemperature;
            wb.tint.value = colorTint;
            wb.temperature.overrideState = true;
            wb.tint.overrideState = true;
        }

        private void SetupVignette()
        {
            Vignette vignette = _profile.Add<Vignette>(true);
            vignette.intensity.value = vignetteIntensity;
            vignette.smoothness.value = vignetteSmoothness;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
        }

        private void SetupMotionBlur()
        {
            MotionBlur motionBlur = _profile.Add<MotionBlur>(true);
            motionBlur.intensity.value = motionBlurIntensity;
            motionBlur.intensity.overrideState = true;
        }

        /// <summary>
        /// Updates post-processing for biome transitions.
        /// </summary>
        public void ApplyBiomePostProcessing(float temperature, float tint, float bloom, float vignette)
        {
            if (_profile == null) return;

            if (_profile.TryGet<WhiteBalance>(out WhiteBalance wb))
            {
                wb.temperature.value = temperature;
                wb.tint.value = tint;
            }

            if (_profile.TryGet<Bloom>(out Bloom bloomEffect))
            {
                bloomEffect.intensity.value = bloom;
            }

            if (_profile.TryGet<Vignette>(out Vignette vignetteEffect))
            {
                vignetteEffect.intensity.value = vignette;
            }
        }
    }
}
