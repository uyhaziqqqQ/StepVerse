using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    public bool isMusicEnabled = true;
    public bool isSoundEnabled = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load preferences
        isMusicEnabled = PlayerPrefs.GetInt("Setting_Music", 1) == 1;
        isSoundEnabled = PlayerPrefs.GetInt("Setting_Sound", 1) == 1;

        bgmSource.mute = !isMusicEnabled;
        sfxSource.mute = !isSoundEnabled;

        // Play main theme by default
        PlayBGM("MAINTHEME-bgm");
    }

    private void InitializeAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.5f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 1f;
        }
    }

    public void PlaySFX(string clipName)
    {
        if (!isSoundEnabled) return;

        AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"AudioManager: SFX '{clipName}' not found in Resources/Audio!");
        }
    }

    public void PlayBGM(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
        if (clip != null)
        {
            bgmSource.clip = clip;
            if (isMusicEnabled)
            {
                bgmSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"AudioManager: BGM '{clipName}' not found in Resources/Audio!");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        bgmSource.mute = !isMusicEnabled;
        PlayerPrefs.SetInt("Setting_Music", enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (enabled && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.Play();
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;
        sfxSource.mute = !isSoundEnabled;
        PlayerPrefs.SetInt("Setting_Sound", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}

public static class AudioBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeAudio()
    {
        GameObject audioManagerObj = new GameObject("AudioManager");
        audioManagerObj.AddComponent<AudioManager>();

        // Auto-attach UI sound hooks when a new scene loads
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            AttachSoundHooks();
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AttachSoundHooks()
    {
        var docs = Object.FindObjectsOfType<UnityEngine.UIElements.UIDocument>();
        foreach (var doc in docs)
        {
            if (doc.GetComponent<UIEventSoundHook>() == null)
            {
                doc.gameObject.AddComponent<UIEventSoundHook>();
            }
        }
    }
}
