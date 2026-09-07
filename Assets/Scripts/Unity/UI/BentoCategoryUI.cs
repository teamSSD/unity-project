using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles a single category (Main or Side) within a Bento Slot.
/// Manages scrolling and the list of menu items.
/// </summary>
public class BentoCategoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform container;
    public ScrollRect scrollRect;
    public Button leftArrow;
    public Button rightArrow;

    private List<MenuSelectionItem> items = new List<MenuSelectionItem>();
    private const float SCROLL_STEP = 0.2f;

    public List<MenuSelectionItem> Items => items;

    public void Initialize()
    {
        if (leftArrow != null)
        {
            leftArrow.onClick.RemoveAllListeners();
            leftArrow.onClick.AddListener(() => Scroll(-SCROLL_STEP));
        }

        if (rightArrow != null)
        {
            rightArrow.onClick.RemoveAllListeners();
            rightArrow.onClick.AddListener(() => Scroll(SCROLL_STEP));
        }
    }

    private void Scroll(float step)
    {
        if (scrollRect == null) return;
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + step);
    }

    public void Clear()
    {
        foreach (var item in items)
        {
            if (item != null)
                item.OnClicked = null;
        }
        items.Clear();

        if (container == null) return;
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    public MenuSelectionItem AddItem(GameObject itemPrefab, FoodData data, System.Action<MenuSelectionItem> onClicked)
    {
        if (container == null || itemPrefab == null) return null;

        var go = Instantiate(itemPrefab, container);
        var item = go.GetComponent<MenuSelectionItem>();
        if (item != null)
        {
            item.SetData(data);
            items.Add(item);
            item.OnClicked += onClicked;
        }
        return item;
    }
}
