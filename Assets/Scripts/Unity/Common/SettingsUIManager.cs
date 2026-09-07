using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUIManager : SingletonMonoBehaviour<SettingsUIManager>
{
    private GameObject settingsPanel;
    private GameObject backdropRoot;
    private Image solidBackdrop;    // GameStart 씬용 solid black
    private RawImage blurBackdrop;  // 그 외 씬용 blur 결과 표시
    private Material blurBlitMaterial; // Graphics.Blit으로 blur 계산 (UI 자체엔 안 붙임)
    private RenderTexture blurRt;

    protected override void OnSingletonAwake()
    {
        var canvasGO = new GameObject("SettingsCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Backdrop root (raycast 차단 + 두 자식 스타일 담음)
        backdropRoot = new GameObject("Backdrop", typeof(RectTransform));
        backdropRoot.layer = LayerMask.NameToLayer("UI");
        backdropRoot.transform.SetParent(canvasGO.transform, false);
        StretchFullScreen((RectTransform)backdropRoot.transform);

        // 자식 1 — solid black (GameStart 씬용)
        var solidGo = new GameObject("Solid", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        solidGo.layer = backdropRoot.layer;
        solidGo.transform.SetParent(backdropRoot.transform, false);
        StretchFullScreen((RectTransform)solidGo.transform);
        solidBackdrop = solidGo.GetComponent<Image>();
        solidBackdrop.color = Color.black;
        solidBackdrop.raycastTarget = true;

        // 자식 2 — blur RawImage (그 외 씬용)
        var blurGo = new GameObject("Blur", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        blurGo.layer = backdropRoot.layer;
        blurGo.transform.SetParent(backdropRoot.transform, false);
        StretchFullScreen((RectTransform)blurGo.transform);
        blurBackdrop = blurGo.GetComponent<RawImage>();
        blurBackdrop.raycastTarget = true;

        // Blur material (Blit 오프스크린 계산용, UI 자체엔 안 붙임)
        var mat = Resources.Load<Material>("UI/UIBlurBackdrop");
        if (mat != null) blurBlitMaterial = new Material(mat);

        backdropRoot.SetActive(false);

        var prefab = CatalogProvider.Prefabs?.settings;
        settingsPanel = Instantiate(prefab, canvasGO.transform);
        settingsPanel.SetActive(false);
    }

    private static void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && ShouldHandleEscape(settingsPanel.activeSelf))
            Close();
    }

    /// <summary>
    /// Escape는 열린 설정 창만 닫는다. 아무 UI도 열려 있지 않다면 Unity가 입력을
    /// 소비하지 않아 WebGL 브라우저의 전체화면 해제에 사용할 수 있다.
    /// </summary>
    public static bool ShouldHandleEscape(bool isSettingsOpen) => isSettingsOpen;

    public void Open()
    {
        if (settingsPanel.activeSelf || !UILockManager.CanOpen(UILockManager.Owner.Settings)) return;

        ApplyBackdropStyle();
        backdropRoot.SetActive(true);
        settingsPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(settingsPanel.GetComponent<RectTransform>());
        UILockManager.Lock(UILockManager.Owner.Settings);
        TimeManager.Instance?.PauseTime();
    }

    public void Close()
    {
        backdropRoot.SetActive(false);
        settingsPanel.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Settings);
        TimeManager.Instance?.ResumeTime();
    }

    /// <summary>씬 컨텍스트에 따라 backdrop 결정.
    /// GameStart: solid black. 그 외: main camera를 RT에 렌더 → blur → RawImage.</summary>
    private void ApplyBackdropStyle()
    {
        bool onGameStart = SceneManager.GetActiveScene().name == SceneNames.GameStart;
        if (onGameStart || blurBlitMaterial == null || Camera.main == null)
        {
            solidBackdrop.enabled = true;
            blurBackdrop.enabled = false;
            return;
        }

        // Main camera view를 RT에 렌더 후 Dual Kawase Blur → RawImage.
        int fullW = Mathf.Max(64, Screen.width);
        int fullH = Mathf.Max(64, Screen.height);
        if (blurRt == null || blurRt.width != fullW || blurRt.height != fullH)
        {
            if (blurRt != null) blurRt.Release();
            blurRt = new RenderTexture(fullW, fullH, 0);
        }

        var srcRt = RenderTexture.GetTemporary(fullW, fullH, 16);
        var cam = Camera.main;
        var prevTarget = cam.targetTexture;
        cam.targetTexture = srcRt;
        cam.Render();
        cam.targetTexture = prevTarget;

        // Dual Kawase: 여러 단계 downsample(pass 0) → 여러 단계 upsample(pass 1) → tint(pass 2).
        // 반복 수 늘리면 더 넓은 블러.
        const int iterations = 4;
        var pyramid = new RenderTexture[iterations];
        int cw = fullW, ch = fullH;
        RenderTexture prev = srcRt;
        for (int i = 0; i < iterations; i++)
        {
            cw = Mathf.Max(2, cw / 2);
            ch = Mathf.Max(2, ch / 2);
            pyramid[i] = RenderTexture.GetTemporary(cw, ch, 0);
            Graphics.Blit(prev, pyramid[i], blurBlitMaterial, 0); // downsample
            prev = pyramid[i];
        }
        for (int i = iterations - 2; i >= 0; i--)
        {
            Graphics.Blit(prev, pyramid[i], blurBlitMaterial, 1); // upsample
            prev = pyramid[i];
        }
        // 마지막 upsample + tint → blurRt
        var beforeTint = RenderTexture.GetTemporary(fullW, fullH, 0);
        Graphics.Blit(prev, beforeTint, blurBlitMaterial, 1); // final upsample to full
        Graphics.Blit(beforeTint, blurRt, blurBlitMaterial, 2); // tint

        RenderTexture.ReleaseTemporary(srcRt);
        RenderTexture.ReleaseTemporary(beforeTint);
        for (int i = 0; i < iterations; i++)
            if (pyramid[i] != null) RenderTexture.ReleaseTemporary(pyramid[i]);

        blurBackdrop.texture = blurRt;
        blurBackdrop.enabled = true;
        solidBackdrop.enabled = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (blurRt != null) { blurRt.Release(); blurRt = null; }
    }
}
