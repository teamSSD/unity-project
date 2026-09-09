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
    public const int CanvasSortingOrder = 2000;

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

    protected override void OnDestroy()
    {
        UILockManager.Unlock(UILockManager.Owner.ConfirmModal);
        base.OnDestroy();
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
        Instance.ShowInternal(title, message, onConfirm, onCancel, yesText, noText, singleButton: false);
    }

    /// <summary>단일 버튼 알림. Yes만 노출, 클릭 시 닫힘 + 선택적 onOk.</summary>
    public static void Alert(string title, string message,
        Action onOk = null, string okText = "확인")
    {
        if (Instance == null) return;
        Instance.ShowInternal(title, message, onOk, null, okText, "", singleButton: true);
    }

    /// <summary>씬 전환용 강제 종료. 콜백은 실행하지 않는다.</summary>
    public static void Dismiss()
    {
        if (Instance == null || !IsOpen) return;
        Instance.canvas.enabled = false;
        Instance.currentOnConfirm = null;
        Instance.currentOnCancel = null;
        UILockManager.Unlock(UILockManager.Owner.ConfirmModal);
    }

    private void ShowInternal(string title, string message,
        Action onConfirm, Action onCancel, string yesText, string noText, bool singleButton)
    {
        titleText.text = title;
        messageText.text = message;
        yesLabel.text = yesText;
        noLabel.text = noText;
        currentOnConfirm = onConfirm;
        currentOnCancel = onCancel;
        if (noButton != null) noButton.gameObject.SetActive(!singleButton);
        canvas.enabled = true;
        UILockManager.Lock(UILockManager.Owner.ConfirmModal);
    }

    private void OnYes()
    {
        canvas.enabled = false;
        UILockManager.Unlock(UILockManager.Owner.ConfirmModal);
        var cb = currentOnConfirm;
        currentOnConfirm = null; currentOnCancel = null;
        cb?.Invoke();
    }

    private void OnNo()
    {
        canvas.enabled = false;
        UILockManager.Unlock(UILockManager.Owner.ConfirmModal);
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
        // 확인 모달은 현재 열린 UI의 작업을 확정/취소하는 전역 최상단 UI다.
        // RecipeBook(1100), 그 위의 튜토리얼 말풍선(1200)보다도 항상 앞에 둔다.
        canvas.sortingOrder = CanvasSortingOrder;

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

        // 메인 패널 — UIColors 시스템 통일 + 자식 크기에 맞춰 자동 height
        panel = new GameObject("Panel", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(canvasObj.transform, false);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = UIColors.PanelBg;
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600f, 0f); // height는 ContentSizeFitter가 결정

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 28, 28);
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = AddText(panel.transform, 36, FontStyles.Bold, UIColors.TextPrimary);
        messageText = AddText(panel.transform, 26, FontStyles.Normal, UIColors.TextSecondary);

        // 버튼 행
        var buttonRow = new GameObject("Buttons", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.transform.SetParent(panel.transform, false);
        var hlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        buttonRow.GetComponent<LayoutElement>().preferredHeight = 70;

        (yesButton, yesLabel) = AddButton(buttonRow.transform, UIColors.ButtonAccent, UIColors.OnAccent);
        (noButton,  noLabel)  = AddButton(buttonRow.transform, UIColors.ButtonNeutral, UIColors.PanelBg);

        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(OnNo);
#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Register("confirm.yes", yesButton);
        E2EUiTargetRegistry.Register("confirm.no", noButton);
#endif
    }

    private static TextMeshProUGUI AddText(Transform parent, int size, FontStyles style, Color color)
    {
        var go = new GameObject("Text", typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
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
