using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 상점 매니저.
/// ShopDialog_Prefab을 런타임에 인스턴스화하여 팝업 UI로 사용.
/// 탭 전환 시 Content만 교체.
/// </summary>
public class UnifiedShopManager : MonoBehaviour
{
    public enum Tab { Item, Tool, Storage, Farm }

    private static UnifiedShopManager instance;
    public static UnifiedShopManager Instance => instance;

    private const string DialogPrefabPath          = "Prefabs/Shop/ShopDialog_Prefab";
    private const string ItemSlotPrefabPath        = "Prefabs/Shop/ItemShopSlot";
    private const string SpecialItemSlotPrefabPath = "Prefabs/Shop/SpecialItemShopSlot";
    private const string UpgradeSlotPrefabPath     = "Prefabs/Shop/UpgradeShopSlot";
    private const string FarmSlotPrefabPath        = "Prefabs/garden/GardenShopSlot";

    // 런타임에 프리팹에서 찾는 참조
    private GameObject dialogInstance;
    private Transform slotContent;
    private TextMeshProUGUI totalPriceText;
    private Button confirmButton;
    private Button cancelButton;
    private TextMeshProUGUI confirmButtonText;

    // 아이템 구매 상태
    private readonly List<ItemShopSlot> itemSlots = new();
    private readonly Dictionary<FoodData, int> purchaseList = new();
    private int totalPrice;

    // 일일 캐시 — 같은 날이면 상품 목록/재고 유지
    private int cachedDay = -1;
    private List<ItemShopSlotInfo> cachedSlotList;
    private readonly Dictionary<FoodData, int> dailyPurchased = new();

    // 업그레이드 슬롯
    private readonly List<UpgradeShopSlot> upgradeSlots = new();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        SetupDialog();
    }

    private void SetupDialog()
    {
        var prefab = Resources.Load<GameObject>(DialogPrefabPath);
        if (prefab == null) { Debug.LogError("[UnifiedShop] ShopDialog_Prefab not found"); return; }

        dialogInstance = Instantiate(prefab);

        // ShopDialog_Prefab > ShopDialog
        var dialog = dialogInstance.transform.GetChild(0);

        slotContent    = dialog.Find("Scroll View/Viewport/Content");
        totalPriceText = dialog.Find("totalPrice")?.GetComponent<TextMeshProUGUI>();
        cancelButton   = dialog.Find("Cancel")?.GetComponent<Button>();
        confirmButton  = dialog.Find("Select")?.GetComponent<Button>();
        confirmButtonText = confirmButton?.GetComponentInChildren<TextMeshProUGUI>();

        cancelButton?.onClick.AddListener(CloseShop);
        confirmButton?.onClick.AddListener(BuyItems);

        dialogInstance.SetActive(false);
    }

    // ── 열기 / 닫기 ──────────────────────────────────────────────────

    public void OpenShop(Tab tab = Tab.Item)
    {
        if (UILockManager.IsLocked) return;
        if (dialogInstance == null) return;

        UILockManager.Lock(UILockManager.Owner.Shop);
        dialogInstance.SetActive(true);
        LoadTab(tab);
    }

    public void CloseShop()
    {
        ClearAllSlots();
        if (dialogInstance != null) dialogInstance.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Shop);
    }

    // ── 탭 로드 ──────────────────────────────────────────────────────

    private void LoadTab(Tab tab)
    {
        ClearAllSlots();

        bool isItemTab = tab == Tab.Item;
        if (confirmButton) confirmButton.gameObject.SetActive(isItemTab);
        if (totalPriceText) totalPriceText.gameObject.SetActive(isItemTab);

        switch (tab)
        {
            case Tab.Item:    LoadItemTab();    break;
            case Tab.Tool:    LoadToolTab();    break;
            case Tab.Storage: LoadStorageTab(); break;
            case Tab.Farm:    LoadFarmTab();    break;
        }
    }

    // ── 아이템 탭 ────────────────────────────────────────────────────

    private void LoadItemTab()
    {
        purchaseList.Clear();
        totalPrice = 0;
        UpdateTotalPrice();

        // 날이 바뀌면 상품 목록 + 구매 기록 초기화
        int today = StatsSystem.Instance.GetDay();
        if (cachedDay != today || cachedSlotList == null)
        {
            var config = Resources.Load<ShopConfigSO>(ResourcePaths.SO.FoodShopConfig);
            if (config == null) { Debug.LogError("[UnifiedShop] FoodShopConfig not found"); return; }
            cachedSlotList = config.BuildSlotList();
            cachedDay = today;
            dailyPurchased.Clear();
        }

        var itemSlotPrefab    = Resources.Load<GameObject>(ItemSlotPrefabPath);
        var specialSlotPrefab = Resources.Load<GameObject>(SpecialItemSlotPrefabPath);

        foreach (var slotInfo in cachedSlotList)
        {
            var prefab = slotInfo.type == ProductType.Special ? specialSlotPrefab : itemSlotPrefab;
            var slot = Instantiate(prefab, slotContent).GetComponent<ItemShopSlot>();
            int alreadyBought = dailyPurchased.TryGetValue(slotInfo.item, out var v) ? v : 0;
            slot.InitSlot(slotInfo, alreadyBought);
            itemSlots.Add(slot);
        }

        foreach (var slot in itemSlots) slot.RefreshSlot();
    }

    // ── 업그레이드 탭 ────────────────────────────────────────────────

    private void LoadToolTab()
    {
        var prefab = Resources.Load<GameObject>(UpgradeSlotPrefabPath);
        foreach (var id in ToolUpgradeManager.Instance.GetAllToolIds())
        {
            var slot = Instantiate(prefab, slotContent).GetComponent<UpgradeShopSlot>();
            slot.InitTool(id);
            upgradeSlots.Add(slot);
        }
    }

    private void LoadStorageTab()
    {
        var prefab = Resources.Load<GameObject>(UpgradeSlotPrefabPath);
        foreach (var type in StorageUpgradeManager.Instance.GetAllTypes())
        {
            var slot = Instantiate(prefab, slotContent).GetComponent<UpgradeShopSlot>();
            slot.InitStorage(type);
            upgradeSlots.Add(slot);
        }
    }

    // ── 농장 탭 ──────────────────────────────────────────────────────

    private void LoadFarmTab()
    {
        var prefab = Resources.Load<GameObject>(UpgradeSlotPrefabPath);
        foreach (var type in FarmUpgradeManager.Instance.GetAllTypes())
        {
            var slot = Instantiate(prefab, slotContent).GetComponent<UpgradeShopSlot>();
            slot.InitFarm(type);
            upgradeSlots.Add(slot);
        }
    }

    // ── 슬롯 정리 ───────────────────────────────────────────────────

    private void ClearAllSlots()
    {
        foreach (var slot in itemSlots)
            if (slot != null) Destroy(slot.gameObject);
        itemSlots.Clear();

        foreach (var slot in upgradeSlots)
            if (slot != null) Destroy(slot.gameObject);
        upgradeSlots.Clear();
    }

    // ── ItemShopSlot 콜백 ────────────────────────────────────────────

    public void AddProduct(FoodData item)
    {
        if (purchaseList.ContainsKey(item)) purchaseList[item]++;
        else purchaseList.Add(item, 1);
    }

    public void SubProduct(FoodData item)
    {
        if (purchaseList.ContainsKey(item) && purchaseList[item] == 1) purchaseList.Remove(item);
        else if (purchaseList.ContainsKey(item)) purchaseList[item]--;
    }

    public void AddPrice(int p) { totalPrice += p; }
    public void SubPrice(int p) { totalPrice -= p; }

    public void UpdateTotalPrice()
    {
        if (totalPriceText != null) totalPriceText.text = $"{totalPrice}G";
    }

    public void CheckMoneyOver()
    {
        if (confirmButton == null) return;
        bool over = totalPrice > StatsSystem.Instance.GetMoney();
        confirmButton.interactable = !over;
        Color c;
        if (over)
        {
            ColorUtility.TryParseHtmlString("#FF8E94", out c);
            if (totalPriceText) totalPriceText.color = c;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#948A8A", out c);
            if (totalPriceText) totalPriceText.color = c;
            if (confirmButtonText) confirmButtonText.color = c;
        }
    }

    public void BuyItems()
    {
        if (totalPrice <= 0) return;
        StatsSystem.Instance.SubMoney(totalPrice);
        SettlementManager.Instance?.AddExpense("재료 구매", totalPrice);
        foreach (var item in purchaseList)
        {
            InventoryManager.Instance?.AddFood(item.Key, item.Value);
            if (dailyPurchased.ContainsKey(item.Key)) dailyPurchased[item.Key] += item.Value;
            else dailyPurchased[item.Key] = item.Value;
        }
        CloseShop();
    }

    // ── UpgradeShopSlot 콜백 ─────────────────────────────────────────

    public void RefreshAllUpgradeSlots()
    {
        foreach (var slot in upgradeSlots) slot.Refresh();
    }


}
