using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }

        // Ensure AudioSources are assigned or created
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Apply initial volumes
        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        AudioListener.volume = masterVolume;
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void PlayBGM(string bgmName)
    {
        AudioClip clip = Resources.Load<AudioClip>("BGM/" + bgmName);
        if (clip == null)
        {
            Debug.LogWarning("BGM clip not found: " + bgmName);
            return;
        }

        if (bgmSource != null)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySFX(string sfxName)
    {
        PlaySFX(sfxName, sfxVolume); // Use default SFX volume
    }

    public void PlaySFX(string sfxName, float volumeScale)
    {
        AudioClip clip = Resources.Load<AudioClip>("SFX/" + sfxName);
        if (clip == null)
        {
            Debug.LogWarning("SFX clip not found: " + sfxName);
            return;
        }

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
}
