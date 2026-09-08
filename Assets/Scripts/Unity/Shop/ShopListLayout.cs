using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-populated shop list layout owner. Unity's deferred layout pass can leave a
/// ScrollRect with its prefab height for a frame (or indefinitely on WebGL), so the
/// scrollable extent is derived synchronously from the actual rows.
/// </summary>
public static class ShopListLayout
{
    public static float Rebuild(RectTransform content)
    {
        if (content == null) return 0f;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        float requiredHeight = CalculateRequiredHeight(content, layout);

        var scrollRect = content.GetComponentInParent<ScrollRect>();
        float viewportHeight = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport.rect.height
            : 0f;
        float previousHeight = content.rect.height;

        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(requiredHeight, viewportHeight));

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

#if AFTERTASTE_E2E
        Debug.Log($"[AftertasteE2E] shop-layout rows={content.childCount} " +
                  $"required={requiredHeight:F1} previous={previousHeight:F1} " +
                  $"content={content.rect.height:F1} viewport={viewportHeight:F1}");
#endif

        return requiredHeight;
    }

    private static float CalculateRequiredHeight(RectTransform content, VerticalLayoutGroup layout)
    {
        float height = layout != null ? layout.padding.vertical : 0f;
        float spacing = layout != null ? layout.spacing : 0f;
        int includedRows = 0;

        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i) is not RectTransform row || !row.gameObject.activeSelf)
                continue;

            var element = row.GetComponent<LayoutElement>();
            if (element != null && element.ignoreLayout) continue;

            if (includedRows > 0) height += spacing;
            height += Mathf.Max(LayoutUtility.GetMinHeight(row), LayoutUtility.GetPreferredHeight(row));
            includedRows++;
        }

        return height;
    }
}
