using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MenuCard 오버레이 표시/닫기 전담.
/// RecipeBookManager에서 분리된 UI 컴포넌트.
/// </summary>
public class MenuCardOverlay : MonoBehaviour
{
    private GameObject overlay;
    private string lastCardFoodId;

    public string LastCardFoodId => lastCardFoodId;

    public void Open(string foodId, Transform bookRoot, GameObject menuCardPrefab)
    {
        lastCardFoodId = foodId;

        if (MenuCardController.Instance != null)
        {
            MenuCardController.Instance.ClearMenuCard();
            MenuCardController.Instance.InitSlot(foodId);
            return;
        }

        overlay = new GameObject("MenuCardOverlay");
        overlay.transform.SetParent(bookRoot, false);
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        var overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(Close);
        overlay.transform.SetAsLastSibling();

        var cardGO = Instantiate(menuCardPrefab, overlay.transform);
        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;

        cardGO.GetComponent<MenuCardController>().InitSlot(foodId);
        CreateCloseButton(overlay.transform, cardRect);
    }

    public void Close()
    {
        lastCardFoodId = null;

        if (MenuCardController.Instance != null)
            MenuCardController.Instance.CloseMenuCard();

        if (overlay != null)
        {
            Destroy(overlay);
            overlay = null;
        }
    }

    private void CreateCloseButton(Transform parent, RectTransform cardRect)
    {
        var btnGO = new GameObject("Button_Close");
        btnGO.transform.SetParent(parent, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        float cardW = cardRect.sizeDelta.x;
        float cardH = cardRect.sizeDelta.y;
        rect.anchoredPosition = new Vector2(cardW / 2 - 15, cardH / 2 - 15);
        rect.sizeDelta = new Vector2(50, 50);

        var btn = btnGO.AddComponent<Button>();
        btnGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = "X";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.2f, 0.2f, 0.2f);
        tmp.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(Close);
    }
}
