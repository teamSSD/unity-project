using Game.Domain.Garden;
using Game.Domain.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopBook 우측 상세 + 액션 패널.
/// 재료/업그레이드 두 종류 액션을 자식 토글로 전환.
/// 구매/업그레이드 로직은 GameSessionRoot의 POCO Service에 위임.
/// </summary>
public class ShopDetailPanel : MonoBehaviour
{
    public enum UpgradeKind { Tool, Storage, Farm }

    [Header("Common")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private GameObject divider;    // 선택 전엔 숨김
    [SerializeField] private GameObject actionArea; // 선택 전엔 숨김 (ItemAction/UpgradeAction 부모)

    [Header("Item Action")]
    [SerializeField] private GameObject itemAction;
    [SerializeField] private Button minusBtn;
    [SerializeField] private Button plusBtn;
    [SerializeField] private Button buyBtn;
    [SerializeField] private TextMeshProUGUI qtyLabel;
    [SerializeField] private TextMeshProUGUI priceLabel;

    [Header("Upgrade Action")]
    [SerializeField] private GameObject upgradeAction;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private Button upgradeBtn;

    // 재료 상태
    private FoodData itemFood;
    private int itemUnitPrice;
    private int itemRemainingStock;
    private int itemQty;

    // 업그레이드 상태
    private UpgradeKind upgradeKind;
    private string upgradeId;

    // ADR-008 — 도메인 서비스는 Inject. .Instance 회귀 금지.
    private PurchaseService purchase;
    private ToolUpgradeService toolUpgrade;
    private StorageUpgradeService storageUpgrade;
    private FarmUpgradeService farmUpgrade;

    public void Inject(PurchaseService purchase, ToolUpgradeService toolUpgrade,
        StorageUpgradeService storageUpgrade, FarmUpgradeService farmUpgrade)
    {
        this.purchase = purchase;
        this.toolUpgrade = toolUpgrade;
        this.storageUpgrade = storageUpgrade;
        this.farmUpgrade = farmUpgrade;
    }

    private void Awake()
    {
        if (plusBtn  != null) plusBtn.onClick.AddListener(OnPlusClicked);
        if (minusBtn != null) minusBtn.onClick.AddListener(OnMinusClicked);
        if (buyBtn   != null) buyBtn.onClick.AddListener(OnBuyClicked);
        if (upgradeBtn != null) upgradeBtn.onClick.AddListener(OnUpgradeClicked);
    }

    // ─── 표시 분기 ─────────────────────────────────────────────────────

    public void ShowEmpty()
    {
        if (detailIcon != null) detailIcon.enabled = false;
        if (detailName != null) detailName.text = "";
        if (detailDescription != null) detailDescription.text = "";
        if (divider != null) divider.SetActive(false);
        if (actionArea != null) actionArea.SetActive(false);
    }

    public void ShowItem(FoodData food, int unitPrice, int remainingStock)
    {
        itemFood = food;
        itemUnitPrice = unitPrice;
        itemRemainingStock = remainingStock;
        itemQty = 0;

        if (detailIcon != null) { detailIcon.sprite = food.image; detailIcon.preserveAspect = true; detailIcon.enabled = true; }
        if (detailName != null) detailName.text = food.ingredientName;
        if (detailDescription != null) detailDescription.text = food.description;
        if (divider != null) divider.SetActive(true);
        if (actionArea != null) actionArea.SetActive(true);

        if (itemAction != null) itemAction.SetActive(true);
        if (upgradeAction != null) upgradeAction.SetActive(false);
        UpdateQtyDisplay();
    }

    public void ShowUpgrade(UpgradeKind kind, string id, Sprite icon, string title, string desc,
        string levelText, string costText, bool canUpgrade)
    {
        upgradeKind = kind;
        upgradeId = id;

        if (detailIcon != null)
        {
            detailIcon.sprite = icon;
            detailIcon.preserveAspect = true;
            detailIcon.enabled = icon != null;
        }
        if (detailName != null) detailName.text = title;
        if (detailDescription != null) detailDescription.text = desc;
        if (divider != null) divider.SetActive(true);
        if (actionArea != null) actionArea.SetActive(true);

        if (itemAction != null) itemAction.SetActive(false);
        if (upgradeAction != null) upgradeAction.SetActive(true);
        if (levelLabel != null) levelLabel.text = levelText;
        if (costLabel != null) costLabel.text = costText;
        if (upgradeBtn != null) upgradeBtn.interactable = canUpgrade;
    }

    // ─── 재료 액션 ─────────────────────────────────────────────────────

    private void OnPlusClicked()
    {
        if (itemQty >= itemRemainingStock) return;
        itemQty++;
        UpdateQtyDisplay();
    }

    private void OnMinusClicked()
    {
        if (itemQty <= 0) return;
        itemQty--;
        UpdateQtyDisplay();
    }

    private void OnBuyClicked()
    {
        if (purchase == null) return;
        int total = itemQty * itemUnitPrice;
        // 실패 사유는 disabled 대신 클릭 후 모달로 안내 — 유저가 왜 안 되는지 알 수 있게.
        var refusal = purchase.GetRefusalReason(itemFood, total);
        if (refusal.HasValue)
        {
            ShowRefusalModal(refusal.Value);
            return;
        }
        if (purchase.TryBuy(itemFood, itemQty, itemUnitPrice))
            ShopUIAdapter.Instance?.NotifyItemPurchased(itemFood, itemQty);
    }

    private static void ShowRefusalModal(PurchaseRefusal r)
    {
        string message = r.Kind == PurchaseRefusalKind.Money
            ? "골드가 부족합니다."
            : $"{StorageLabelWithSubject(r.Category)} 가득 찼습니다. ({r.Used}/{r.Max})\n창고 탭에서 확장할 수 있습니다.";
        ConfirmModal.Alert(title: "구매 불가", message: message);
    }

    private static string StorageLabelWithSubject(IngredientDisplayCategory c) => c switch
    {
        IngredientDisplayCategory.Refrigerator => "냉장고가",
        IngredientDisplayCategory.UpperShelf   => "윗 찬장이",
        IngredientDisplayCategory.LowerShelf   => "아랫 찬장이",
        _                                      => "저장 공간이",
    };

    private void UpdateQtyDisplay()
    {
        if (qtyLabel != null) qtyLabel.text = itemQty.ToString();
        int total = itemQty * itemUnitPrice;
        if (priceLabel != null) priceLabel.text = $"{total}G";
        // 실패 사유(잔액/저장 부족)는 클릭 후 모달로 안내 — 버튼은 qty>0이면 항상 활성.
        if (buyBtn != null) buyBtn.interactable = itemQty > 0;
    }

    // ─── 업그레이드 액션 ──────────────────────────────────────────────

    private void OnUpgradeClicked()
    {
        bool success = upgradeKind switch
        {
            UpgradeKind.Tool    => toolUpgrade?.TryUpgrade(upgradeId)    ?? false,
            UpgradeKind.Storage => storageUpgrade?.TryUpgrade(upgradeId) ?? false,
            UpgradeKind.Farm    => farmUpgrade?.TryUpgrade(upgradeId)    ?? false,
            _                   => false,
        };
        if (success) ShopUIAdapter.Instance?.NotifyUpgradeApplied();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
