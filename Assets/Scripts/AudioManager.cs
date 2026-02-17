using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    private const string KEY_MUSIC = "SET_MUSIC";
    private const string KEY_SFX = "SET_SFX";

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip mainTheme;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;

    [Header("SFX")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip burnedSfx;
    [SerializeField] private AudioClip uiClickSfx; // <-- ДОБАВЬ

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // дефолты
        if (!PlayerPrefs.HasKey(KEY_MUSIC)) PlayerPrefs.SetInt(KEY_MUSIC, 1);
        if (!PlayerPrefs.HasKey(KEY_SFX)) PlayerPrefs.SetInt(KEY_SFX, 1);

        ApplySettings();
        StartMusicIfNeeded();
    }

    public void PlayUiClickSfx()
    {
        PlaySfx(uiClickSfx, 1f);
    }

    public void ApplySettings()
    {
        bool musicOn = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.mute = !musicOn;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.mute = !sfxOn;
        }
    }

    public void StartMusicIfNeeded()
    {
        if (musicSource == null || mainTheme == null) return;

        if (musicSource.clip != mainTheme)
            musicSource.clip = mainTheme;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    // На будущее: проигрывание SFX
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null) return;
        if (sfxSource.mute) return;

        sfxSource.PlayOneShot(clip, volumeScale);
    }

    // вызов из настроек
    public void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KEY_MUSIC, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettings();
        if (enabled) StartMusicIfNeeded();
    }

    public void SetSfxEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KEY_SFX, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettings();
    }

    public void PlayBurnedSfx()
    {
        PlaySfx(burnedSfx, 1f);
    }



    public bool IsMusicEnabled() => PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
    public bool IsSfxEnabled() => PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
}