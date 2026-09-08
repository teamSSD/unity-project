using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    private AudioSource loopSfxSource;

    // UI SFX는 Unity AudioClip으로 import하지 않는다. WebGL은 브라우저의 Audio API가
    // StreamingAssets 원본을 재생하고, 다른 플랫폼만 같은 원본을 Unity AudioClip으로 preload한다.
    private const string UiBookStreamingPath = "Audio/UI/sfx_ui_book.mp3";
    private const string ButtonClickStreamingPath = "Audio/UI/sfx_ui_button_click.mp3";
    private const string BgmMallStreamingPath = "Audio/BGM/bgm_mall_theme.mp3";
    private const string BgmCookingStreamingPath = "Audio/BGM/bgm_preperation_theme.mp3";
    private const string BgmNightStreamingPath = "Audio/BGM/bgm_night_theme.mp3";
    private const string BgmGardenStreamingPath = "Audio/BGM/bgm_garden_dawn.mp3";
    private AudioClip _uiBookSfx;
    private AudioClip _buttonClickSfx;
    private bool _uiSfxPreloadStarted;
    private readonly Dictionary<string, AudioClip> _streamingAudioClips = new Dictionary<string, AudioClip>();
    private string _activeBgmPath;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AftertastePlayUiSfx(string relativePath, float volume);

    [DllImport("__Internal")]
    private static extern void AftertastePlayBgm(string relativePath, float volume);

    [DllImport("__Internal")]
    private static extern void AftertasteStopBgm();
#endif

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
        PreloadUiSfxAsync().Forget();
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
        // Listener 없어서 UpdateBGM에서 Play가 gated된 경우 재시도. Play는 stopped/paused 둘 다 처리.
        if (_hasAudioListener && bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.Play();
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
            // Play는 stopped(처음)/paused(중간) 둘 다 커버. UnPause는 paused만.
            if (!bgmSource.isPlaying) bgmSource.Play();
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
        string path = SelectBgmStreamingPath();
        if (string.IsNullOrEmpty(path))
        {
            _activeBgmPath = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            AftertasteStopBgm();
#else
            bgmSource.Stop();
            bgmSource.clip = null;
#endif
            return;
        }

        if (_activeBgmPath == path) return;
        _activeBgmPath = path;
#if UNITY_WEBGL && !UNITY_EDITOR
        AftertastePlayBgm(path, bgmSource != null ? bgmSource.volume : 1f);
#else
        PlayStreamingBgmAsync(path).Forget();
#endif
    }

    private string SelectBgmStreamingPath()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Boot" || scene == "GameStart") return null;
        // Garden은 페이즈 무관 항상 dawn BGM 유지.
        if (scene == "Garden") return BgmGardenStreamingPath;
        if (GameSessionRoot.Instance?.Progress?.PhaseData?.Phase == PhaseType.Night) return BgmNightStreamingPath;
        return scene == "Cooking" ? BgmCookingStreamingPath : BgmMallStreamingPath;
    }

    // ── BGM 볼륨 ──

    public void SetBGMVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        bgmSource.volume = clamped;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(_activeBgmPath)) AftertastePlayBgm(_activeBgmPath, clamped);
#endif
    }
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

    public void PlayUIBook() => PlayUiSfx(UiBookStreamingPath, ref _uiBookSfx);
    public void PlayButtonClick() => PlayUiSfx(ButtonClickStreamingPath, ref _buttonClickSfx);

    private void PlayUiSfx(string streamingPath, ref AudioClip clip)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL의 Unity AudioClip(AAC/FSB) 디코더는 이 두 짧은 UI 효과음에서 실패한다.
        // 실제 브라우저 사용자 입력에서 호출되므로 Audio.play()의 autoplay 정책도 충족한다.
        AftertastePlayUiSfx(streamingPath, sfxSource != null ? sfxSource.volume : 1f);
#else
        if (clip == null) PreloadUiSfxAsync().Forget();
        Play2DSFX(clip);
#endif
    }

    private async UniTaskVoid PreloadUiSfxAsync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        await UniTask.CompletedTask;
#else
        if (_uiSfxPreloadStarted) return;
        _uiSfxPreloadStarted = true;
        _uiBookSfx = await LoadStreamingAudioClipAsync(UiBookStreamingPath);
        _buttonClickSfx = await LoadStreamingAudioClipAsync(ButtonClickStreamingPath);
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private async UniTask<AudioClip> LoadStreamingAudioClipAsync(string relativePath)
    {
        if (_streamingAudioClips.TryGetValue(relativePath, out var cached)) return cached;
        var path = Path.Combine(Application.streamingAssetsPath, relativePath);
        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path), AudioType.MPEG);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SoundManager] UI SFX preload failed: {relativePath} ({request.error})");
            return null;
        }

        var clip = DownloadHandlerAudioClip.GetContent(request);
        _streamingAudioClips[relativePath] = clip;
        return clip;
    }

    private async UniTaskVoid PlayStreamingBgmAsync(string path)
    {
        var clip = await LoadStreamingAudioClipAsync(path);
        if (clip == null || _activeBgmPath != path) return;
        bgmSource.clip = clip;
        if (_hasAudioListener) bgmSource.Play();
    }
#endif

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
