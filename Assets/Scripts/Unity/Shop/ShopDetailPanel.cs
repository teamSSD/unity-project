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
        var purchase = GameSessionRoot.Instance?.Purchase;
        if (purchase == null) return;
        if (purchase.TryBuy(itemFood, itemQty, itemUnitPrice))
            ShopUIAdapter.Instance?.NotifyItemPurchased(itemFood, itemQty);
    }

    private void UpdateQtyDisplay()
    {
        if (qtyLabel != null) qtyLabel.text = itemQty.ToString();
        int total = itemQty * itemUnitPrice;
        if (priceLabel != null) priceLabel.text = $"{total}G";
        if (buyBtn != null)
        {
            bool canBuy = GameSessionRoot.Instance?.Purchase?.CanBuy(itemFood, total) ?? false;
            buyBtn.interactable = itemQty > 0 && canBuy;
        }
    }

    // ─── 업그레이드 액션 ──────────────────────────────────────────────

    private void OnUpgradeClicked()
    {
        bool success = upgradeKind switch
        {
            UpgradeKind.Tool    => GameSessionRoot.Instance?.ToolUpgrade?.TryUpgrade(upgradeId)    ?? false,
            UpgradeKind.Storage => GameSessionRoot.Instance?.StorageUpgrade?.TryUpgrade(upgradeId) ?? false,
            UpgradeKind.Farm    => GameSessionRoot.Instance?.FarmUpgrade?.TryUpgrade(upgradeId)    ?? false,
            _                   => false,
        };
        if (success) ShopUIAdapter.Instance?.NotifyUpgradeApplied();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
