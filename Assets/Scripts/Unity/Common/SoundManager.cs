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

    // 씬 전환 순간 AudioListener 없으면 SFX 재생 시 Unity 경고 스팸.
    // OnSceneLoaded에서 갱신하고 SFX 재생 전 체크.
    private bool _hasAudioListener;

    protected override void OnSingletonAwake()
    {
        if (bgmSource == null)     bgmSource     = CreateAudioSource("BGMSpeaker",     loop: true);
        if (sfxSource == null)     sfxSource     = CreateAudioSource("SFXSpeaker",     loop: false);
        if (loopSfxSource == null) loopSfxSource = CreateAudioSource("LoopSFXSpeaker", loop: true);

        // 스폰 순간 listener 상태에 따라 소스 즉시 활성/비활성.
        _hasAudioListener = FindFirstObjectByType<AudioListener>() != null;
        SetSourcesEnabled(_hasAudioListener);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
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
        RefreshAudioListenerNextFrameAsync().Forget();
    }

    private async UniTaskVoid RefreshAudioListenerNextFrameAsync()
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        _hasAudioListener = FindFirstObjectByType<AudioListener>() != null;
        SetSourcesEnabled(_hasAudioListener);
        if (_hasAudioListener && bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    /// <summary>씬 unload로 카메라(AudioListener 소지자)가 사라지는 순간 즉시 AudioSource 비활성.
    /// AudioSource가 활성 상태로 남아있으면 native audio가 매 프레임 "no listeners" 경고 spam.</summary>
    private void OnSceneUnloaded(Scene scene)
    {
        _hasAudioListener = FindFirstObjectByType<AudioListener>() != null;
        if (!_hasAudioListener)
        {
            if (bgmSource != null && bgmSource.isPlaying) bgmSource.Pause();
            SetSourcesEnabled(false);
        }
    }

    /// <summary>Listener 없는 순간 AudioSource 컴포넌트 자체를 disable해 native audio 경고 완전 차단.</summary>
    private void SetSourcesEnabled(bool en)
    {
        if (bgmSource != null) bgmSource.enabled = en;
        if (sfxSource != null) sfxSource.enabled = en;
        if (loopSfxSource != null) loopSfxSource.enabled = en;
    }

    // 씬 전환 순간 이미 재생 중인 BGM이 listener 없는 프레임에 매 프레임 Unity 경고 유발.
    // 주기 체크로 listener 존재 여부 갱신 + BGM pause/unpause 자동화.
    private float _listenerCheckTimer;
    private void Update()
    {
        _listenerCheckTimer += Time.unscaledDeltaTime;
        if (_listenerCheckTimer < 0.3f) return;
        _listenerCheckTimer = 0f;

        _hasAudioListener = FindFirstObjectByType<AudioListener>() != null;
        if (bgmSource == null || bgmSource.clip == null) return;
        if (_hasAudioListener)
        {
            if (!bgmSource.isPlaying) bgmSource.UnPause();
        }
        else
        {
            if (bgmSource.isPlaying) bgmSource.Pause();
        }
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
        if (_hasAudioListener) bgmSource.Play();
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
        // 씬 전환 순간 리스너 없으면 skip (Unity 경고 스팸 방지).
        if (!_hasAudioListener) return;
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
        if (!_hasAudioListener) return;
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
