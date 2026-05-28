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

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsPanel.activeSelf)
        {
            Close();
        }
        else if (!UILockManager.IsLocked && SceneManager.GetActiveScene().name != SceneNames.GameStart)
        {
            Open();
        }
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
