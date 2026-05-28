using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 탭 좌/우 페이지 관리.
/// 좌: 카테고리별 고정 격자(업그레이드 슬롯 수 기준) / 우: 선택 아이템 상세(배치 목록)
/// </summary>
public class InventoryPageController : MonoBehaviour
{
    [Header("Left — Slot Containers (상단선반 / 하단선반 / 냉장고)")]
    [SerializeField] private Transform upperShelfContainer;
    [SerializeField] private Transform lowerShelfContainer;
    [SerializeField] private Transform refrigeratorContainer;

    [Header("Left — Slot Prefab")]
    [SerializeField] private InventorySlot slotPrefab;

    [Header("Right — Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailTotalQty;
    [SerializeField] private Transform batchListContainer;
    [SerializeField] private GameObject batchRowPrefab;
    [SerializeField] private TextMeshProUGUI emptyHint;

    [Header("Bar Colors (남은일수 비율 기준)")]
    [SerializeField] private Color barFresh   = new Color(0.18f, 0.70f, 0.35f);
    [SerializeField] private Color barWarning = new Color(1.00f, 0.75f, 0.00f);
    [SerializeField] private Color barExpired = new Color(0.85f, 0.25f, 0.25f);

    [Header("Bar Thresholds (0~1, 이 비율 이하일 때 색 적용)")]
    [SerializeField, Range(0f, 1f)] private float barExpiredRatio = 0.3f;
    [SerializeField, Range(0f, 1f)] private float barWarningRatio = 0.6f;

    private InventorySlot selectedSlot;

    private static readonly (IngredientDisplayCategory category, Transform container, string upgradeType)[] CategoryMap
        = default; // 런타임에 세팅

    public void Refresh()
    {
        PopulateCategory(upperShelfContainer,   IngredientDisplayCategory.UpperShelf,   "upperShelf");
        PopulateCategory(lowerShelfContainer,   IngredientDisplayCategory.LowerShelf,   "lowerShelf");
        PopulateCategory(refrigeratorContainer, IngredientDisplayCategory.Refrigerator, "refrigerator");
        ClearDetail();

        RebuildNestedLayout();
        // 첫 활성화 직후엔 CSF 체인이 0으로 잡힐 수 있어, 한 프레임 뒤 재실행
        if (isActiveAndEnabled) RebuildNextFrameAsync().Forget();
    }

    private async UniTaskVoid RebuildNextFrameAsync()
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        RebuildNestedLayout();
    }

    // 안쪽(SlotGrid)에서 시작해 Page_L까지 모든 조상을 안→밖 순서로 강제 재빌드.
    // ScrollRect Viewport·Scroll 등 새 중간 계층이 생겨도 작동.
    private void RebuildNestedLayout()
    {
        RebuildToPageL(upperShelfContainer);
        RebuildToPageL(lowerShelfContainer);
        RebuildToPageL(refrigeratorContainer);
    }

    private static void RebuildToPageL(Transform leaf)
    {
        var t = leaf;
        while (t != null)
        {
            ForceRebuild(t);
            if (t.name == "Page_L") break;
            t = t.parent;
        }
    }

    private static void ForceRebuild(Transform t)
    {
        if (t is RectTransform rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // ─── 왼쪽: 격자 채우기 ─────────────────────────────────────────────

    private void PopulateCategory(Transform container, IngredientDisplayCategory category, string upgradeType)
    {
        if (container == null) return;

        int slotCount = GetSlotCount(upgradeType);
        var items = InventoryManager.Instance != null
            ? InventoryManager.Instance.LoadIngredientsByCategory(category)
            : new List<(FoodData, IngredientData)>();

        // 기존 슬롯 재활용 또는 생성
        EnsureSlots(container, slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            var slot = container.GetChild(i).GetComponent<InventorySlot>();
            if (i < items.Count)
            {
                var (food, _) = items[i];
                int total = InventoryManager.Instance.CheckStockAmount(food);
                slot.SetData(food, total);
                slot.OnClicked = OnSlotClicked;
            }
            else
            {
                slot.SetEmpty();
                slot.OnClicked = null;
            }
        }
    }

    private void EnsureSlots(Transform container, int count)
    {
        // 부족하면 추가
        while (container.childCount < count)
        {
            var go = Instantiate(slotPrefab, container);
            go.gameObject.SetActive(true);
        }
        // 초과하면 비활성
        for (int i = 0; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(i < count);
    }

    private int GetSlotCount(string upgradeType)
    {
        var data = StorageUpgradeManager.Instance?.GetCurrentData(upgradeType);
        return data != null ? data.value : 0;
    }

    // ─── 슬롯 클릭 ─────────────────────────────────────────────────────

    private void OnSlotClicked(InventorySlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedSlot = slot;
        slot.SetSelected(true);
        ShowDetail(slot.CurrentFood);
    }

    // ─── 오른쪽: 상세 패널 ─────────────────────────────────────────────

    private void ShowDetail(FoodData food)
    {
        if (food == null) { ClearDetail(); return; }
        if (detailPanel != null) detailPanel.SetActive(true);
        if (emptyHint != null) emptyHint.gameObject.SetActive(false);

        if (detailIcon != null)
        {
            detailIcon.sprite = food.image;
            detailIcon.preserveAspect = true;
        }
        if (detailName != null)  detailName.text = food.ingredientName;

        int total = InventoryManager.Instance?.CheckStockAmount(food) ?? 0;
        if (detailTotalQty != null) detailTotalQty.text = $"총 {total}개";

        PopulateBatchList(food);
    }

    private void PopulateBatchList(FoodData food)
    {
        if (batchListContainer == null) return;

        // 기존 행 제거
        for (int i = batchListContainer.childCount - 1; i >= 0; i--)
            Destroy(batchListContainer.GetChild(i).gameObject);

        var batches = InventoryManager.Instance?.GetBatches(food);
        if (batches == null) return;

        // 임박한 순(daysRemaining 오름차순) 정렬
        batches.Sort((a, b) => a.daysRemaining.CompareTo(b.daysRemaining));

        int maxDays = food.ingredient != null ? food.ingredient.expirationDay : 10;
        if (maxDays <= 0) maxDays = 10;

        foreach (var batch in batches)
        {
            var row = Instantiate(batchRowPrefab, batchListContainer);
            row.SetActive(true);
            SetupBatchRow(row, batch.quantity, batch.daysRemaining, maxDays);
        }
    }

    private void SetupBatchRow(GameObject row, int qty, int daysRemaining, int maxDays)
    {
        // 수량 레이블
        var qtyLabel = row.transform.Find("QtyLabel")?.GetComponent<TextMeshProUGUI>();
        if (qtyLabel != null) qtyLabel.text = $"● {qty}개";

        float ratio = maxDays > 0 ? Mathf.Clamp01((float)daysRemaining / maxDays) : 0f;

        // 진행 바 (Bar는 BarBg의 자식)
        var bar = row.transform.Find("BarBg/Bar")?.GetComponent<Image>();
        if (bar != null)
        {
            bar.fillAmount = ratio;
            bar.color = GetBarColor(ratio);
        }

        // 남은 일수 레이블: 만료일 때만 빨강 강조, 그 외엔 본문 텍스트 색 유지
        var daysLabel = row.transform.Find("DaysLabel")?.GetComponent<TextMeshProUGUI>();
        if (daysLabel != null)
        {
            daysLabel.text = daysRemaining > 0 ? $"{daysRemaining}/{maxDays}" : "만료";
            if (daysRemaining <= 0) daysLabel.color = barExpired;
        }
    }

    private void ClearDetail()
    {
        if (selectedSlot != null) { selectedSlot.SetSelected(false); selectedSlot = null; }
        if (detailPanel != null) detailPanel.SetActive(false);
        if (emptyHint != null) emptyHint.gameObject.SetActive(true);
        if (batchListContainer != null)
            for (int i = batchListContainer.childCount - 1; i >= 0; i--)
                Destroy(batchListContainer.GetChild(i).gameObject);
    }

    private Color GetBarColor(float ratio)
    {
        if (ratio <= barExpiredRatio) return barExpired;
        if (ratio <= barWarningRatio) return barWarning;
        return barFresh;
    }
}
