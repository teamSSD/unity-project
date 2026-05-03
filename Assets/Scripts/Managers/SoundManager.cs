using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    private AudioSource loopSfxSource;

    private Coroutine _loopFadeCoroutine;

    private static readonly string BGM_COOKING = "Sound/bgm/bgm_preperation_theme";
    private static readonly string BGM_MALL    = "Sound/bgm/bgm_mall_theme";
    private static readonly string BGM_NIGHT   = "Sound/bgm/bgm_night_theme";

    protected override void OnSingletonAwake()
    {
        if (bgmSource == null)     bgmSource     = CreateAudioSource("BGMSpeaker",     loop: true);
        if (sfxSource == null)     sfxSource     = CreateAudioSource("SFXSpeaker",     loop: false);
        if (loopSfxSource == null) loopSfxSource = CreateAudioSource("LoopSFXSpeaker", loop: true);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void Start()
    {
        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.OnPhaseChanged += OnPhaseChanged;
        UpdateBGM();
    }

    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        if (ProgressSystem.Instance != null)
        {
            ProgressSystem.Instance.OnPhaseChanged -= OnPhaseChanged;
            ProgressSystem.Instance.OnPhaseChanged += OnPhaseChanged;
        }
        UpdateBGM();
    }

    private void OnPhaseChanged(PhaseType phase) => UpdateBGM();

    private void UpdateBGM()
    {
        string path = SelectBGMPath();
        if (path == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private string SelectBGMPath()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Boot" || scene == "GameStart") return null;
        if (ProgressSystem.Instance?.phaseData?.Phase == PhaseType.Night) return BGM_NIGHT;
        return scene == "Cooking" ? BGM_COOKING : BGM_MALL;
    }

    // ── BGM 볼륨 ──

    public void SetBGMVolume(float volume) => bgmSource.volume = Mathf.Clamp01(volume);
    public void SetSFXVolume(float volume) => sfxSource.volume = Mathf.Clamp01(volume);

    // ── 일회성 SFX ──

    public void Play2DSFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ── 루프 SFX (미니게임 조리음) ──

    public void PlayLoopSFX(AudioClip clip, float fadeIn = 0.2f)
    {
        if (clip == null) return;
        if (_loopFadeCoroutine != null) StopCoroutine(_loopFadeCoroutine);
        loopSfxSource.clip = clip;
        loopSfxSource.volume = 0f;
        loopSfxSource.Play();
        _loopFadeCoroutine = StartCoroutine(FadeLoopSFX(0f, 1f, fadeIn));
    }

    public void StopLoopSFX(float fadeOut = 0.2f)
    {
        if (_loopFadeCoroutine != null) StopCoroutine(_loopFadeCoroutine);
        _loopFadeCoroutine = StartCoroutine(FadeAndStopLoopSFX(fadeOut));
    }

    public void SetLoopSFXVolume(float volume)
    {
        loopSfxSource.volume = Mathf.Clamp01(volume);
    }

    private IEnumerator FadeLoopSFX(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            loopSfxSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        loopSfxSource.volume = to;
    }

    private IEnumerator FadeAndStopLoopSFX(float duration)
    {
        float start = loopSfxSource.volume;
        yield return StartCoroutine(FadeLoopSFX(start, 0f, duration));
        loopSfxSource.Stop();
        loopSfxSource.clip = null;
    }

    // ── 유틸 ──

    private AudioSource CreateAudioSource(string goName, bool loop)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }
}
