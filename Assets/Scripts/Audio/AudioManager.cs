using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all audio: music playback with crossfade, SFX, UI sounds.
/// Uses AudioMixer groups for volume control.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioMixer masterMixer;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private float musicCrossfadeDuration = 1.5f;

    [Header("SFX")]
    [SerializeField] private SFXEntry[] sfxEntries;

    [Header("Settings")]
    [SerializeField] private int sfxPoolSize = 10;

    [System.Serializable]
    public class SFXEntry
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;
    }

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isMusicAActive;

    private List<AudioSource> sfxPool;
    private int sfxPoolIndex;
    private Dictionary<string, SFXEntry> sfxLookup;

    private const string MusicVolKey = "MusicVolume";
    private const string SFXVolKey = "SFXVolume";
    private const string UIVolKey = "UIVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupMusicSources();
        SetupSFXPool();
        BuildSFXLookup();
        LoadVolumeSettings();
    }

    private void Start()
    {
        GameManager.Instance.OnGameStart += OnGameStart;
        GameManager.Instance.OnGameOver += OnGameOver;
        GameManager.Instance.OnGamePause += OnGamePause;
        GameManager.Instance.OnGameResume += OnGameResume;

        // Start with menu music
        PlayMusic(menuMusic);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnGameOver -= OnGameOver;
            GameManager.Instance.OnGamePause -= OnGamePause;
            GameManager.Instance.OnGameResume -= OnGameResume;
        }
    }

    #region Music

    private void SetupMusicSources()
    {
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.loop = true;
        musicSourceA.playOnAwake = false;
        if (musicMixerGroup != null) musicSourceA.outputAudioMixerGroup = musicMixerGroup;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.loop = true;
        musicSourceB.playOnAwake = false;
        if (musicMixerGroup != null) musicSourceB.outputAudioMixerGroup = musicMixerGroup;

        musicSourceB.volume = 0f;
        isMusicAActive = true;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(CrossfadeMusic(clip));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        AudioSource fadeIn = isMusicAActive ? musicSourceB : musicSourceA;
        AudioSource fadeOut = isMusicAActive ? musicSourceA : musicSourceB;

        fadeIn.clip = newClip;
        fadeIn.Play();

        float timer = 0f;
        while (timer < musicCrossfadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / musicCrossfadeDuration;
            fadeIn.volume = Mathf.Lerp(0f, 1f, t);
            fadeOut.volume = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume = 1f;

        isMusicAActive = !isMusicAActive;
    }

    public void DuckMusic(float duckAmount, float duration)
    {
        StartCoroutine(DuckMusicCoroutine(duckAmount, duration));
    }

    private IEnumerator DuckMusicCoroutine(float duckAmount, float duration)
    {
        if (masterMixer == null) yield break;

        float originalVol;
        masterMixer.GetFloat("MusicVolume", out originalVol);

        masterMixer.SetFloat("MusicVolume", originalVol - duckAmount);
        yield return new WaitForSecondsRealtime(duration);
        masterMixer.SetFloat("MusicVolume", originalVol);
    }

    #endregion

    #region SFX

    private void SetupSFXPool()
    {
        sfxPool = new List<AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;
            sfxPool.Add(source);
        }
        sfxPoolIndex = 0;
    }

    private void BuildSFXLookup()
    {
        sfxLookup = new Dictionary<string, SFXEntry>();
        if (sfxEntries == null) return;
        foreach (SFXEntry entry in sfxEntries)
        {
            if (!string.IsNullOrEmpty(entry.name))
            {
                sfxLookup[entry.name] = entry;
            }
        }
    }

    public void PlaySFX(string sfxName)
    {
        if (!sfxLookup.TryGetValue(sfxName, out SFXEntry entry)) return;
        if (entry.clip == null) return;

        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;

        source.clip = entry.clip;
        source.volume = entry.volume;
        source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        source.Play();
    }

    public void PlayUISFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;

        source.clip = clip;
        source.volume = 1f;
        source.pitch = 1f;
        if (uiMixerGroup != null) source.outputAudioMixerGroup = uiMixerGroup;
        source.Play();
    }

    #endregion

    #region Volume Settings

    public void SetMusicVolume(float normalized)
    {
        float db = normalized > 0.001f ? Mathf.Log10(normalized) * 20f : -80f;
        if (masterMixer != null) masterMixer.SetFloat("MusicVolume", db);
        PlayerPrefs.SetFloat(MusicVolKey, normalized);
    }

    public void SetSFXVolume(float normalized)
    {
        float db = normalized > 0.001f ? Mathf.Log10(normalized) * 20f : -80f;
        if (masterMixer != null) masterMixer.SetFloat("SFXVolume", db);
        PlayerPrefs.SetFloat(SFXVolKey, normalized);
    }

    public void SetUIVolume(float normalized)
    {
        float db = normalized > 0.001f ? Mathf.Log10(normalized) * 20f : -80f;
        if (masterMixer != null) masterMixer.SetFloat("UIVolume", db);
        PlayerPrefs.SetFloat(UIVolKey, normalized);
    }

    public float GetMusicVolume() => PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFXVolKey, 1.0f);
    public float GetUIVolume() => PlayerPrefs.GetFloat(UIVolKey, 1.0f);

    private void LoadVolumeSettings()
    {
        SetMusicVolume(GetMusicVolume());
        SetSFXVolume(GetSFXVolume());
        SetUIVolume(GetUIVolume());
    }

    #endregion

    #region Game Events

    private void OnGameStart()
    {
        PlayMusic(gameplayMusic);
    }

    private void OnGameOver()
    {
        PlaySFX("GameOver");
        DuckMusic(10f, 2f);
    }

    private void OnGamePause()
    {
        AudioListener.pause = true;
    }

    private void OnGameResume()
    {
        AudioListener.pause = false;
    }

    #endregion
}
