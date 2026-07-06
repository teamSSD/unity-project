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

    private bool prevLocked;

    void Update()
    {
        // 직전 프레임 lock 상태를 매 프레임 캡처. ESC로 다른 modal이 같은 프레임에 닫혀
        // Unlock해도, 이 캡처 덕에 "방금 닫힌 modal이 있던 상태"를 판별할 수 있다.
        // (Update 순서가 modal보다 늦으면 IsLocked=false인데, 사실 그 ESC는 modal이 소비한 것)
        bool wasLocked = prevLocked;
        prevLocked = UILockManager.IsLocked;

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsPanel.activeSelf)
        {
            Close();
            return;
        }
        if (UILockManager.IsLocked) return;
        if (wasLocked) return;
        if (SceneManager.GetActiveScene().name == SceneNames.GameStart) return;
        Open();
    }

    public void Open()
    {
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

        // 뒷 화면(main camera view) 캡처 후 블러 → RawImage
        int w = Mathf.Max(64, Screen.width / 2);
        int h = Mathf.Max(64, Screen.height / 2);
        if (blurRt == null || blurRt.width != w || blurRt.height != h)
        {
            if (blurRt != null) blurRt.Release();
            blurRt = new RenderTexture(w, h, 0);
        }

        var tempRt = RenderTexture.GetTemporary(w, h, 16);
        var cam = Camera.main;
        var prevTarget = cam.targetTexture;
        cam.targetTexture = tempRt;
        cam.Render();
        cam.targetTexture = prevTarget;

        Graphics.Blit(tempRt, blurRt, blurBlitMaterial);
        RenderTexture.ReleaseTemporary(tempRt);

        blurBackdrop.texture = blurRt;
        blurBackdrop.enabled = true;
        solidBackdrop.enabled = false;
    }

    private void OnDestroy()
    {
        if (blurRt != null) { blurRt.Release(); blurRt = null; }
    }
}
