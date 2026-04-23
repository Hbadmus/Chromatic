using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Mixer Parameters")]
    [SerializeField] private string master = "MasterVolume";
    [SerializeField] private string music = "MusicVolume";
    [SerializeField] private string sfx = "SFXVolume";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Level Music")]
    [Tooltip("Index matches level index set by LevelMusicStarter")]
    [SerializeField] private AudioClip[] levelMusicClips;
    [SerializeField] private AudioClip[] bossMusicClips;

    private AudioClip defaultMusic;
    private AudioClip bossRoomMusic;

    private const float MuteDb = -80f;
    private AudioClip currentMusicClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Music and ambient keep playing through pause menus
            if (musicSource != null) musicSource.ignoreListenerPause = true;
            if (ambientSource != null) ambientSource.ignoreListenerPause = true;

            LoadVolumes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentLevel(int index)
    {
        if (levelMusicClips != null && index < levelMusicClips.Length)
            defaultMusic = levelMusicClips[index];

        if (bossMusicClips != null && index < bossMusicClips.Length)
            bossRoomMusic = bossMusicClips[index];
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
        currentMusicClip = clip;
    }

    public void PlayDefaultMusic()
    {
        PlayMusicIfDifferent(defaultMusic);
    }

    public void PlayBossRoomMusic()
    {
        PlayMusicIfDifferent(bossRoomMusic);
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        currentMusicClip = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXAt(AudioClip clip, Vector3 position)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position);
    }

    public void PlaySFXLoop(AudioClip clip)
    {
        if (clip == null || ambientSource == null)
        {
            return;
        }

        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void StopSFX()
    {
        if (sfxSource == null)
        {
            return;
        }

        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }

    public void StopSFXLoop()
    {
        if (ambientSource == null)
        {
            return;
        }

        ambientSource.loop = false;
        if (ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }

    public void PlayAmbient(AudioClip clip)
    {
        PlaySFXLoop(clip);
    }

    public void StopAmbient()
    {
        StopSFXLoop();
    }

    public void SetVolume(string parameter, float normalized)
    {
        float clamped = Mathf.Clamp01(normalized);
        PlayerPrefs.SetFloat(parameter, clamped);
        PlayerPrefs.Save();

        ApplyVolume(parameter, clamped);
    }

    public float GetVolume(string parameter)
    {
        if (PlayerPrefs.HasKey(parameter))
        {
            return PlayerPrefs.GetFloat(parameter);
        }

        return 1f;
    }

    public void MuteAll(bool isMuted)
    {
        SetMixerMute(master, isMuted);
    }

    public void MuteMusic(bool isMuted)
    {
        SetMixerMute(music, isMuted);
    }

    public void MuteSFX(bool isMuted)
    {
        SetMixerMute(sfx, isMuted);
    }

    public void PauseAll()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }

        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Pause();
        }

        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.Pause();
        }
    }

    public void ResumeAll()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }

        if (sfxSource != null)
        {
            sfxSource.UnPause();
        }

        if (ambientSource != null)
        {
            ambientSource.UnPause();
        }
    }

    private void LoadVolumes()
    {
        ApplyVolume(master, GetVolume(master));
        ApplyVolume(music, GetVolume(music));
        ApplyVolume(sfx, GetVolume(sfx));
    }

    private void ApplyVolume(string parameter, float normalized)
    {
        if (mainMixer == null)
        {
            return;
        }

        float clamped = Mathf.Clamp(normalized, 0.0001f, 1f);
        float decibel = Mathf.Log10(clamped) * 20f;
        mainMixer.SetFloat(parameter, decibel);
    }

    private void SetMixerMute(string parameter, bool isMuted)
    {
        if (mainMixer == null)
        {
            return;
        }

        if (isMuted)
        {
            mainMixer.SetFloat(parameter, MuteDb);
            return;
        }

        ApplyVolume(parameter, GetVolume(parameter));
    }

    private void PlayMusicIfDifferent(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (musicSource != null && musicSource.isPlaying && currentMusicClip == clip)
        {
            return;
        }

        PlayMusic(clip);
    }
}
