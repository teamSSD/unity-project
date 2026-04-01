using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    [Header("스피커 설정")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    public void Play2DSFX(AudioClip resource, float volume = 1f)
    {
        sfxSource.PlayOneShot(resource, volume);
    }
}