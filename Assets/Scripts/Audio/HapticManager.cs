using UnityEngine;

/// <summary>
/// Manages haptic feedback on mobile devices. Rate-limited to prevent buzz fatigue.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    public enum HapticType { Light, Medium, Heavy }

    [Header("Settings")]
    [SerializeField] private float lightCooldown = 0.1f;
    [SerializeField] private float mediumCooldown = 0.2f;
    [SerializeField] private float heavyCooldown = 0.5f;
    [SerializeField] private bool hapticsEnabled = true;

    private float lastLightTime;
    private float lastMediumTime;
    private float lastHeavyTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerHaptic(HapticType type)
    {
        if (!hapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        switch (type)
        {
            case HapticType.Light:
                if (Time.time - lastLightTime < lightCooldown) return;
                lastLightTime = Time.time;
                VibrateAndroid(10);
                break;
            case HapticType.Medium:
                if (Time.time - lastMediumTime < mediumCooldown) return;
                lastMediumTime = Time.time;
                VibrateAndroid(30);
                break;
            case HapticType.Heavy:
                if (Time.time - lastHeavyTime < heavyCooldown) return;
                lastHeavyTime = Time.time;
                VibrateAndroid(80);
                break;
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void VibrateAndroid(long milliseconds)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                if (vibrator != null)
                {
                    // Android API 26+ uses VibrationEffect
                    if (GetAndroidAPILevel() >= 26)
                    {
                        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", milliseconds, -1);
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                    else
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticManager] Vibration failed: " + e.Message);
        }
    }

    private int GetAndroidAPILevel()
    {
        using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
    }
#endif

    public void SetEnabled(bool enabled)
    {
        hapticsEnabled = enabled;
        PlayerPrefs.SetInt("HapticsEnabled", enabled ? 1 : 0);
    }

    public bool IsEnabled() => hapticsEnabled;
}
