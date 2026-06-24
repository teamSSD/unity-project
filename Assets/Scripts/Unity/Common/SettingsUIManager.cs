using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUIManager : SingletonMonoBehaviour<SettingsUIManager>
{
    private GameObject settingsPanel;
    private GameObject backdrop;

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

        backdrop = new GameObject("BlackBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.layer = LayerMask.NameToLayer("UI");
        backdrop.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)backdrop.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        backdrop.GetComponent<Image>().color = Color.black;
        backdrop.GetComponent<Image>().raycastTarget = true;
        backdrop.SetActive(false);

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
        backdrop.SetActive(true);
        settingsPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(settingsPanel.GetComponent<RectTransform>());
        UILockManager.Lock(UILockManager.Owner.Settings);
        TimeManager.Instance?.PauseTime();
    }

    public void Close()
    {
        backdrop.SetActive(false);
        settingsPanel.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Settings);
        TimeManager.Instance?.ResumeTime();
    }
}
