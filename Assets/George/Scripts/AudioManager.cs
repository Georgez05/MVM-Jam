using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region Variables
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Mixer")]
    public AudioMixer mixer;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    private Coroutine musicFadeCoroutine;

    [Header("Music References")]
    public AudioClip mainMenuMusic;
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);

        PlayMusic(mainMenuMusic, true);
    }

    #region Play & Stop Functions
    public void PlayMusic(AudioClip newClip, bool loop = true)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeToNewMusic(newClip, 1f, loop));
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip audioClip)
    {
        sfxSource.PlayOneShot(audioClip, sfxVolume);
    }
    #endregion


    #region Helper Functions
    private IEnumerator FadeToNewMusic(AudioClip newClip, float duration, bool loop)
    {
        // get current mixer volume in dB and convert to linear
        mixer.GetFloat("MusicVolume", out float currentDb);
        float startLinear = Mathf.Pow(10f, currentDb / 20f);
        float targetLinear = musicVolume;

        // fade out
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float newLinear = Mathf.Lerp(startLinear, 0.0001f, t / duration);
            SetMusicVolume(newLinear);
            yield return null;
        }

        SetMusicVolume(0.0001f);
        musicSource.Stop();

        // switch clip and play
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // fade in
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float newLinear = Mathf.Lerp(0.0001f, targetLinear, t / duration);
            SetMusicVolume(newLinear);
            yield return null;
        }

        SetMusicVolume(targetLinear);
    }
    #endregion


    #region Volume Control
    public void SetMasterVolume(float volume)
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
    }
    #endregion
}
