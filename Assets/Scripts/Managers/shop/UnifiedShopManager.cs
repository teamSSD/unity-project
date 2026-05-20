using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 상점 매니저.
/// ShopBook.prefab(레시피북 메타포)을 런타임에 인스턴스화.
/// 탭 4개(재료/도구/창고/농장)를 인덱스로 전환, 카트 없이 한 행씩 즉시 구매/업그레이드.
/// </summary>
public class UnifiedShopManager : MonoBehaviour
{
    public enum Tab { Item, Tool, Storage, Farm }

    public static UnifiedShopManager Instance { get; private set; }

    private const string BookPrefabPath = "Prefabs/Shop/ShopBook";
    private const string RowPrefabPath  = "Prefabs/Shop/ShopListRow";

    // 런타임 책 참조
    private GameObject bookInstance;
    private Transform  listContent;       // Page_L/Scroll/Viewport/Content
    private TextMeshProUGUI headerLabel;  // Page_L/Header
    private ShopDetailPanel detailPanel;  // Page_R 의 컴포넌트
    private Button closeButton;
    private Button[] bookmarkButtons = new Button[4]; // Tab.Item~Farm

    private GameObject cachedRowPrefab;
    private readonly List<ShopListRow> currentRows = new();
    private ShopListRow selectedRow;

    private Tab currentTab = Tab.Item;

    // 인덱스 탭 사이즈 — 가로 길이만 선택 시 늘림
    private const float BookmarkWidthUnselected = 70f;
    private const float BookmarkWidthSelected   = 105f;
    private const float BookmarkHeight          = 45f;

    // 재료 탭 일일 캐시
    private int cachedDay = -1;
    private List<ItemShopSlotInfo> cachedItemList;
    private readonly Dictionary<FoodData, int> dailyPurchased = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        SpawnBook();
    }

    private void SpawnBook()
    {
        var prefab = Resources.Load<GameObject>(BookPrefabPath);
        if (prefab == null) { Debug.LogError("[UnifiedShop] ShopBook prefab not found"); return; }

        // Canvas 래퍼 — 매니저 자식으로 두어 라이프사이클 공유 (Screen Space Overlay)
        var wrapper = new GameObject("ShopBookCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        wrapper.transform.SetParent(transform, false);
        var canvas = wrapper.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = wrapper.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        bookInstance = Instantiate(prefab, wrapper.transform, false);
        var book = bookInstance.transform;

        listContent = book.Find("Page/ShopPage/Page_L/Scroll/Viewport/Content");
        var headerT = book.Find("Page/ShopPage/Page_L/Header");
        if (headerT != null) headerLabel = headerT.GetComponent<TextMeshProUGUI>();
        var pageR = book.Find("Page/ShopPage/Page_R");
        if (pageR != null) detailPanel = pageR.GetComponent<ShopDetailPanel>();

        closeButton = book.Find("Button_Close")?.GetComponent<Button>();
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

        // 인덱스 탭 4개 (BookMark_Item / Tool / Storage / Farm)
        for (int i = 0; i < 4; i++)
        {
            var bm = book.Find($"Page/ShopPage/BookMark_{(Tab)i}");
            if (bm != null) bookmarkButtons[i] = bm.GetComponent<Button>();
        }
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            if (bookmarkButtons[i] != null)
                bookmarkButtons[i].onClick.AddListener(() => SwitchTab((Tab)idx));
        }

        bookInstance.SetActive(false);
    }

    // ─── 열기 / 닫기 ─────────────────────────────────────────────────

    public void OpenShop(Tab tab = Tab.Item)
    {
        if (UILockManager.IsLocked) return;
        if (bookInstance == null) return;

        UILockManager.Lock(UILockManager.Owner.Shop);
        bookInstance.SetActive(true);
        UISoundManager.Instance?.PlayUIBook();
        GlobalButtonSfxManager.Instance?.RegisterButtons(bookInstance.transform);
        SwitchTab(tab);
    }

    public void CloseShop()
    {
        ClearRows();
        if (bookInstance != null) bookInstance.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Shop);
    }

    // ─── 탭 전환 ─────────────────────────────────────────────────────

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;
        ClearRows();
        detailPanel?.ShowEmpty();
        if (headerLabel != null) headerLabel.text = TabName(tab);
        UpdateBookmarkSelection(tab);

        switch (tab)
        {
            case Tab.Item:    PopulateItemList();    break;
            case Tab.Tool:    PopulateToolList();    break;
            case Tab.Storage: PopulateStorageList(); break;
            case Tab.Farm:    PopulateFarmList();    break;
        }
    }

    private void UpdateBookmarkSelection(Tab selected)
    {
        for (int i = 0; i < 4; i++)
        {
            if (bookmarkButtons[i] == null) continue;
            var rt = bookmarkButtons[i].GetComponent<RectTransform>();
            bool isSel = (Tab)i == selected;
            rt.sizeDelta = new Vector2(isSel ? BookmarkWidthSelected : BookmarkWidthUnselected, BookmarkHeight);
        }
    }

    private static string TabName(Tab t) => t switch
    {
        Tab.Item    => "재료",
        Tab.Tool    => "도구",
        Tab.Storage => "창고",
        Tab.Farm    => "농장",
        _           => "",
    };

    private GameObject GetRowPrefab()
    {
        if (cachedRowPrefab == null) cachedRowPrefab = Resources.Load<GameObject>(RowPrefabPath);
        return cachedRowPrefab;
    }

    private void ClearRows()
    {
        foreach (var r in currentRows) if (r != null) Destroy(r.gameObject);
        currentRows.Clear();
        selectedRow = null;
    }

    private ShopListRow AddRow(Sprite icon, string name, string leftSub, string rightSub, object data)
    {
        var prefab = GetRowPrefab();
        if (prefab == null || listContent == null) return null;
        var row = Instantiate(prefab, listContent).GetComponent<ShopListRow>();
        row.SetData(icon, name, leftSub, rightSub, data);
        row.OnClicked = OnRowClicked;
        currentRows.Add(row);
        return row;
    }

    private void OnRowClicked(ShopListRow row)
    {
        if (selectedRow != null) selectedRow.SetSelected(false);
        selectedRow = row;
        row.SetSelected(true);

        switch (row.UserData)
        {
            case ItemRowData item:    detailPanel?.ShowItem(item.Food, item.UnitPrice, item.RemainingStock); break;
            case ToolRowData tool:    ShowToolDetail(tool.Id);    break;
            case StorageRowData stg:  ShowStorageDetail(stg.Type); break;
            case FarmRowData farm:    ShowFarmDetail(farm.Type);   break;
        }
    }

    // ─── 재료 탭 ─────────────────────────────────────────────────────

    private void PopulateItemList()
    {
        // 일일 캐시
        int today = StatsSystem.Instance.GetDay();
        if (cachedDay != today || cachedItemList == null)
        {
            var config = Resources.Load<ShopConfigSO>(ResourcePaths.SO.FoodShopConfig);
            if (config == null) { Debug.LogError("[UnifiedShop] FoodShopConfig not found"); return; }
            cachedItemList = config.BuildSlotList();
            cachedDay = today;
            dailyPurchased.Clear();
        }

        foreach (var info in cachedItemList)
        {
            int already = dailyPurchased.TryGetValue(info.item, out var v) ? v : 0;
            int remaining = Mathf.Max(0, info.stock - already);
            int price = info.item.ingredient != null ? info.item.ingredient.defaultPrice : 0;

            AddRow(
                info.item.image,
                info.item.ingredientName,
                $"재고 {remaining}/{info.stock}",
                $"{price}G",
                new ItemRowData { Food = info.item, UnitPrice = price, Stock = info.stock, RemainingStock = remaining }
            );
        }
    }

    public void NotifyItemPurchased(FoodData item, int qty)
    {
        if (dailyPurchased.ContainsKey(item)) dailyPurchased[item] += qty;
        else dailyPurchased[item] = qty;

        // 해당 행 갱신 + 선택돼 있으면 detail 도 갱신
        foreach (var row in currentRows)
        {
            if (row.UserData is ItemRowData d && d.Food == item)
            {
                d.RemainingStock = Mathf.Max(0, d.Stock - dailyPurchased[item]);
                row.SetData(item.image, item.ingredientName,
                    $"재고 {d.RemainingStock}/{d.Stock}", $"{d.UnitPrice}G", d);
                if (selectedRow == row) detailPanel?.ShowItem(d.Food, d.UnitPrice, d.RemainingStock);
                break;
            }
        }
    }

    // ─── 도구 탭 ─────────────────────────────────────────────────────

    private void PopulateToolList()
    {
        var mgr = ToolUpgradeManager.Instance;
        if (mgr == null) return;
        foreach (var id in mgr.GetAllToolIds())
        {
            var tool = SearchDataUtil.GetCookingToolDataById(id);
            var cur  = mgr.GetCurrentData(id);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(id) ? "MAX" : $"{mgr.GetNextData(id).cost}G";
            AddRow(
                tool?.defaultImage,
                tool != null ? tool.cookerName : id,
                leftSub,
                rightSub,
                new ToolRowData { Id = id }
            );
        }
    }

    private void ShowToolDetail(string id)
    {
        var mgr = ToolUpgradeManager.Instance;
        if (mgr == null) return;
        var tool = SearchDataUtil.GetCookingToolDataById(id);
        var cur  = mgr.GetCurrentData(id);
        string title = tool != null ? $"{tool.cookerName} 업그레이드" : id;
        bool isMax = mgr.IsMax(id);

        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc      = cur != null
                ? $"미니게임 시간 {cur.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}"
                : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(id);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc      = $"미니게임 시간 {cur.durationMultiplier * 100:0}%→{next.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}→{next.staminaCost}";
            canUpgrade = (StatsSystem.Instance?.GetMoney() ?? 0) >= next.cost;
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Tool, id, tool?.defaultImage, title, desc, levelText, costText, canUpgrade);
    }

    // ─── 창고 탭 ─────────────────────────────────────────────────────

    private void PopulateStorageList()
    {
        var mgr = StorageUpgradeManager.Instance;
        if (mgr == null) return;
        foreach (var type in mgr.GetAllTypes())
        {
            var cur  = mgr.GetCurrentData(type);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(type) ? "MAX" : $"{mgr.GetNextData(type).cost}G";
            AddRow(
                null,
                StorageTypeName(type),
                leftSub,
                rightSub,
                new StorageRowData { Type = type }
            );
        }
    }

    private void ShowStorageDetail(string type)
    {
        var mgr = StorageUpgradeManager.Instance;
        if (mgr == null) return;
        var cur = mgr.GetCurrentData(type);
        bool isMax = mgr.IsMax(type);
        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc      = cur != null ? $"{StorageTypeName(type)} {cur.value}칸" : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(type);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc      = $"{StorageTypeName(type)} {cur.value}칸→{next.value}칸";
            canUpgrade = (StatsSystem.Instance?.GetMoney() ?? 0) >= next.cost;
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Storage, type, null, StorageTypeName(type), desc, levelText, costText, canUpgrade);
    }

    // ─── 농장 탭 ─────────────────────────────────────────────────────

    private void PopulateFarmList()
    {
        var mgr = FarmUpgradeManager.Instance;
        if (mgr == null) return;
        foreach (var type in mgr.GetAllTypes())
        {
            var cur = mgr.GetCurrentData(type);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(type) ? "MAX" : $"{mgr.GetNextData(type).cost}G";
            AddRow(
                null,
                FarmTypeName(type),
                leftSub,
                rightSub,
                new FarmRowData { Type = type }
            );
        }
    }

    private void ShowFarmDetail(string type)
    {
        var mgr = FarmUpgradeManager.Instance;
        if (mgr == null) return;
        var cur = mgr.GetCurrentData(type);
        bool isMax = mgr.IsMax(type);
        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc      = cur != null ? FarmValueLabel(type, cur.value, cur.value) : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(type);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc      = FarmValueLabel(type, cur.value, next.value);
            canUpgrade = (StatsSystem.Instance?.GetMoney() ?? 0) >= next.cost;
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Farm, type, null, FarmTypeName(type), desc, levelText, costText, canUpgrade);
    }

    // ─── 업그레이드 후 갱신 ──────────────────────────────────────────

    public void NotifyUpgradeApplied()
    {
        // 현재 탭 전체 리로드 (가격/레벨 일괄 갱신)
        SwitchTab(currentTab);
    }

    // ─── 표시 보조 ───────────────────────────────────────────────────

    private static string StorageTypeName(string type) => type switch
    {
        "refrigerator" => "냉장고 확장",
        "upperShelf"   => "윗 찬장 확장",
        "lowerShelf"   => "아랫 찬장 확장",
        _              => type,
    };

    private static string FarmTypeName(string type) => type switch
    {
        "tile"          => "농장 확장",
        "timeReduction" => "수확 시간 감소",
        "harvestCount"  => "수확량 증가",
        _               => type,
    };

    private static string FarmValueLabel(string type, float cur, float next)
    {
        bool isMax = Mathf.Approximately(cur, next);
        return type switch
        {
            "tile"          => isMax ? $"타일 {(int)cur}개" : $"타일 {(int)cur}개→{(int)next}개",
            "timeReduction" => isMax ? $"수확 시간 -{cur * 100:0}%" : $"수확 시간 -{cur * 100:0}%→-{next * 100:0}%",
            "harvestCount"  => isMax ? $"수확량 {(int)cur}개" : $"수확량 {(int)cur}개→{(int)next}개",
            _               => $"{cur}→{next}",
        };
    }

    // ─── 행 UserData 타입 ────────────────────────────────────────────

    private class ItemRowData
    {
        public FoodData Food;
        public int UnitPrice;
        public int Stock;
        public int RemainingStock;
    }
    private class ToolRowData    { public string Id; }
    private class StorageRowData { public string Type; }
    private class FarmRowData    { public string Type; }
}
