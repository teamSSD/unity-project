using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MenuCard Input 영역 동적 간격 조절. MenuCardController에서 추출.
/// AdjustSpacing(62줄) 분해 — 측정/계산/적용을 별도 메서드로 분리.
/// </summary>
public static class MenuCardLayoutHelper
{
    private const float RecipeDesiredSpacing = 10f;
    private const float FallbackItemWidth = 70f;
    private const float FallbackInputWidth = 520f;
    private const float MinReservedWidth = 120f;

    /// <summary>Input 컨테이너의 HorizontalLayoutGroup.spacing 을 가용 폭에 맞춰 조정.</summary>
    public static void AdjustSpacing(RectTransform container)
    {
        var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null) return;

        RebuildParentLayout(container);
        float availableWidth = MeasureAvailableWidth(container);

        if (!CountActiveItems(container, out int activeCount, out float itemWidth)
            || activeCount <= 1 || itemWidth <= 0)
        {
            layoutGroup.spacing = 0;
            return;
        }

        layoutGroup.spacing = ComputeSpacing(availableWidth, activeCount, itemWidth);
    }

    private static void RebuildParentLayout(RectTransform container)
    {
        RectTransform parentRT = container.parent as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT != null ? parentRT : container);
    }

    private static float MeasureAvailableWidth(RectTransform container)
    {
        float width = container.rect.width;
        if (width > 0) return width;
        return EstimateInputWidthFromRecipeLine(container, container.parent as RectTransform);
    }

    private static bool CountActiveItems(RectTransform container, out int activeCount, out float itemWidth)
    {
        activeCount = 0;
        itemWidth = 0;
        foreach (Transform child in container)
        {
            if (!child.gameObject.activeSelf) continue;
            activeCount++;
            if (itemWidth == 0)
            {
                var rt = child.GetComponent<RectTransform>();
                itemWidth = (rt != null && rt.rect.width > 0) ? rt.rect.width : FallbackItemWidth;
            }
        }
        return activeCount > 0;
    }

    private static float ComputeSpacing(float availableWidth, int activeCount, float itemWidth)
    {
        float totalChildWidth = activeCount * itemWidth;
        float spacingBudget = RecipeDesiredSpacing * (activeCount - 1);

        if (totalChildWidth + spacingBudget <= availableWidth) return RecipeDesiredSpacing;

        if (totalChildWidth > availableWidth)
        {
            float neededSpacing = (availableWidth - totalChildWidth) / (activeCount - 1);
            return Mathf.Min(0, neededSpacing);
        }
        return 0;
    }

    public static float EstimateInputWidthFromRecipeLine(RectTransform inputContainer, RectTransform recipeLine)
    {
        if (recipeLine == null || recipeLine.rect.width <= 0) return FallbackInputWidth;

        float reservedWidth = 0f;
        int activeSiblingCount = 0;
        var lineLayout = recipeLine.GetComponent<HorizontalLayoutGroup>();

        foreach (Transform sibling in recipeLine)
        {
            if (!sibling.gameObject.activeSelf) continue;
            activeSiblingCount++;
            if (sibling == inputContainer.transform) continue;

            float width = 0f;
            var layout = sibling.GetComponent<LayoutElement>();
            if (layout != null && layout.preferredWidth > 0)
                width = layout.preferredWidth;
            else if (sibling is RectTransform siblingRect && siblingRect.rect.width > 0)
                width = siblingRect.rect.width;

            reservedWidth += width;
        }

        if (lineLayout != null && activeSiblingCount > 1)
            reservedWidth += lineLayout.spacing * (activeSiblingCount - 1);

        return Mathf.Max(MinReservedWidth, recipeLine.rect.width - reservedWidth);
    }
}
