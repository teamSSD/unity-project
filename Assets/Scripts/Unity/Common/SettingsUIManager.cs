using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUIManager : SingletonMonoBehaviour<SettingsUIManager>
{
    private GameObject settingsPanel;
    private GameObject backdrop;
    private Image backdropImage;
    private Material blurMaterial; // 씬별로 mat 스왑 (GameStart=solid, 그 외=blur)

    protected override void OnSingletonAwake()
    {
        var canvasGO = new GameObject("SettingsCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        // ScreenSpaceOverlay → Camera로 변경. Overlay에서는 GrabPass가 UI 자신을 못 캡처.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasGO.AddComponent<GraphicRaycaster>();

        backdrop = new GameObject("BlackBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.layer = LayerMask.NameToLayer("UI");
        backdrop.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)backdrop.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        backdropImage = backdrop.GetComponent<Image>();
        backdropImage.color = Color.white; // 색은 material에서 tint (blur mat 사용 시). solid 모드는 색 오버라이드.
        backdropImage.raycastTarget = true;
        backdrop.SetActive(false);

        blurMaterial = Resources.Load<Material>("UI/UIBlurBackdrop");

        var prefab = CatalogProvider.Prefabs?.settings;
        settingsPanel = Instantiate(prefab, canvasGO.transform);
        settingsPanel.SetActive(false);
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
        backdrop.SetActive(true);
        settingsPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(settingsPanel.GetComponent<RectTransform>());
        UILockManager.Lock(UILockManager.Owner.Settings);
        TimeManager.Instance?.PauseTime();
    }

    /// <summary>씬 컨텍스트에 따라 backdrop 스타일 결정.
    /// GameStart: 배경이 어차피 정적이라 solid 검정. 그 외: blur + 반투명 검정 tint.</summary>
    private void ApplyBackdropStyle()
    {
        bool onGameStart = SceneManager.GetActiveScene().name == SceneNames.GameStart;
        if (onGameStart || blurMaterial == null)
        {
            backdropImage.material = null;
            backdropImage.color = Color.black;
        }
        else
        {
            backdropImage.material = blurMaterial;
            backdropImage.color = Color.white; // material에서 tint 처리
        }
    }

    public void Close()
    {
        backdrop.SetActive(false);
        settingsPanel.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Settings);
        TimeManager.Instance?.ResumeTime();
    }
}
