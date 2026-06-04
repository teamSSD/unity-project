using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    private AudioSource loopSfxSource;

    [Header("UI SFX")]
    [SerializeField] private AudioClip uiBookSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    private CancellationTokenSource _loopFadeCts;
    private readonly HashSet<int> _registeredButtons = new HashSet<int>();

    protected override void OnSingletonAwake()
    {
        if (bgmSource == null)     bgmSource     = CreateAudioSource("BGMSpeaker",     loop: true);
        if (sfxSource == null)     sfxSource     = CreateAudioSource("SFXSpeaker",     loop: false);
        if (loopSfxSource == null) loopSfxSource = CreateAudioSource("LoopSFXSpeaker", loop: true);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null) progress.OnPhaseChanged -= OnPhaseChanged;
    }

    private void Start()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null) progress.OnPhaseChanged += OnPhaseChanged;
        UpdateBGM();
        RegisterButtons(null);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanButtonsNextFrameAsync().Forget();
    }

    private async UniTaskVoid ScanButtonsNextFrameAsync()
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        RegisterButtons(null);
    }

    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null)
        {
            progress.OnPhaseChanged -= OnPhaseChanged;
            progress.OnPhaseChanged += OnPhaseChanged;
        }
        UpdateBGM();
    }

    private void OnPhaseChanged(PhaseType phase) => UpdateBGM();

    private void UpdateBGM()
    {
        AudioClip clip = SelectBGMClip();
        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }
        if (bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private AudioClip SelectBGMClip()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Boot" || scene == "GameStart") return null;
        if (GameSessionRoot.Instance?.Progress?.PhaseData?.Phase == PhaseType.Night) return CatalogProvider.BgmNight;
        return scene == "Cooking" ? CatalogProvider.BgmCooking : CatalogProvider.BgmMall;
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

    // ── UI SFX (UISoundManager에서 흡수) ──

    public void PlayUIBook()      => Play2DSFX(uiBookSfx);
    public void PlayButtonClick() => Play2DSFX(buttonClickSfx);

    // ── 버튼 자동 등록 (GlobalButtonSfxManager에서 흡수) ──

    public void RegisterButtons(Transform root)
    {
        Button[] buttons = root != null
            ? root.GetComponentsInChildren<Button>(true)
            : FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var btn in buttons)
        {
            int id = btn.GetInstanceID();
            if (_registeredButtons.Contains(id)) continue;
            if (btn.GetComponentInParent<Slider>() != null) continue;

            _registeredButtons.Add(id);
            btn.onClick.AddListener(PlayButtonClick);
        }
    }

    // ── 루프 SFX (미니게임 조리음) ──

    public void PlayLoopSFX(AudioClip clip, float fadeIn = 0.2f)
    {
        if (clip == null) return;
        RestartLoopFade();
        loopSfxSource.clip = clip;
        loopSfxSource.volume = 0f;
        loopSfxSource.Play();
        FadeLoopSFXAsync(0f, 1f, fadeIn, _loopFadeCts.Token).Forget();
    }

    public void StopLoopSFX(float fadeOut = 0.2f)
    {
        RestartLoopFade();
        FadeAndStopLoopSFXAsync(fadeOut, _loopFadeCts.Token).Forget();
    }

    public void SetLoopSFXVolume(float volume)
    {
        loopSfxSource.volume = Mathf.Clamp01(volume);
    }

    private void RestartLoopFade()
    {
        _loopFadeCts?.Cancel();
        _loopFadeCts?.Dispose();
        _loopFadeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    private async UniTask FadeLoopSFXAsync(float from, float to, float duration, CancellationToken ct)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            loopSfxSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            await UniTask.Yield(cancellationToken: ct);
        }
        loopSfxSource.volume = to;
    }

    private async UniTaskVoid FadeAndStopLoopSFXAsync(float duration, CancellationToken ct)
    {
        float start = loopSfxSource.volume;
        await FadeLoopSFXAsync(start, 0f, duration, ct);
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
