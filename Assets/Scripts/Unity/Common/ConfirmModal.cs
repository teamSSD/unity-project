using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경량 Yes/No 확인 모달. ConfirmModal.Show(...) 한 번 호출로 사용.
/// 최초 호출 시 Canvas + Panel을 코드로 동적 생성, 이후 재사용.
/// </summary>
public class ConfirmModal : SingletonMonoBehaviour<ConfirmModal>
{
    private Canvas canvas;
    private GameObject panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI messageText;
    private Button yesButton;
    private Button noButton;
    private TextMeshProUGUI yesLabel;
    private TextMeshProUGUI noLabel;

    private Action currentOnConfirm;
    private Action currentOnCancel;

    /// <summary>모달이 현재 열려 있는지. 다른 UI(레시피북 등)의 ESC 핸들러가 이 값으로 가드해야 함.</summary>
    public static bool IsOpen => Instance != null && Instance.canvas != null && Instance.canvas.enabled;

    protected override void OnSingletonAwake()
    {
        BuildUI();
        canvas.enabled = false;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) OnNo();
    }

    /// <summary>
    /// title/message 표시 후 Yes → onConfirm, No → onCancel 호출.
    /// </summary>
    public static void Show(string title, string message,
        Action onConfirm, Action onCancel = null,
        string yesText = "예", string noText = "아니요")
    {
        if (Instance == null) return;
        Instance.ShowInternal(title, message, onConfirm, onCancel, yesText, noText);
    }

    private void ShowInternal(string title, string message,
        Action onConfirm, Action onCancel, string yesText, string noText)
    {
        titleText.text = title;
        messageText.text = message;
        yesLabel.text = yesText;
        noLabel.text = noText;
        currentOnConfirm = onConfirm;
        currentOnCancel = onCancel;
        canvas.enabled = true;
    }

    private void OnYes()
    {
        canvas.enabled = false;
        var cb = currentOnConfirm;
        currentOnConfirm = null; currentOnCancel = null;
        cb?.Invoke();
    }

    private void OnNo()
    {
        canvas.enabled = false;
        var cb = currentOnCancel;
        currentOnConfirm = null; currentOnCancel = null;
        cb?.Invoke();
    }

    private void BuildUI()
    {
        var canvasObj = new GameObject("ConfirmModalCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // Shop UI(100)보다 위

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 반투명 dim 배경 (클릭 차단)
        var dim = new GameObject("Dim", typeof(Image));
        dim.transform.SetParent(canvasObj.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.55f);
        var dimRt = (RectTransform)dim.transform;
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;

        // 메인 패널 — RecipeBook 페이지 톤(아이보리)
        panel = new GameObject("Panel", typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(canvasObj.transform, false);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.996f, 0.996f, 0.972f, 1f);
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600f, 280f);

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 28, 24);
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        titleText = AddText(panel.transform, 36, FontStyles.Bold, new Color(0.20f, 0.16f, 0.12f));
        messageText = AddText(panel.transform, 26, FontStyles.Normal, new Color(0.30f, 0.25f, 0.20f));

        // 버튼 행
        var buttonRow = new GameObject("Buttons", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.transform.SetParent(panel.transform, false);
        var hlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        buttonRow.GetComponent<LayoutElement>().preferredHeight = 70;

        // RecipeBook bookmark sage(0.55,0.62,0.49) 변형으로 통일감
        (yesButton, yesLabel) = AddButton(buttonRow.transform, new Color(0.55f, 0.62f, 0.49f), Color.white);
        (noButton,  noLabel)  = AddButton(buttonRow.transform, new Color(0.85f, 0.83f, 0.78f), new Color(0.30f, 0.25f, 0.20f));

        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(OnNo);
    }

    private static TextMeshProUGUI AddText(Transform parent, int size, FontStyles style, Color color)
    {
        var go = new GameObject("Text", typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        return t;
    }

    private static (Button btn, TextMeshProUGUI label) AddButton(Transform parent, Color bg, Color text)
    {
        var go = new GameObject("Button", typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        go.GetComponent<LayoutElement>().preferredWidth = 180;
        go.GetComponent<LayoutElement>().preferredHeight = 64;

        var label = AddText(go.transform, 26, FontStyles.Bold, text);
        var labelRt = (RectTransform)label.transform;
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;

        return (go.GetComponent<Button>(), label);
    }
}
