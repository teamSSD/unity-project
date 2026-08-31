using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Garden;
using Game.Domain.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 상점 UI Adapter. ShopBook.prefab(레시피북 메타포)을 런타임에 인스턴스화.
/// 탭 4개(재료/도구/창고/농장) 전환, 한 행씩 즉시 구매/업그레이드.
/// 영구/일일 상태는 PurchaseService(POCO)에 위임, 이 클래스는 순수 UI.
/// 업그레이드 탭(Tool/Storage/Farm) 처리는 ShopUIAdapter.UpgradeTabs.cs (partial).
/// </summary>
public partial class ShopUIAdapter : SingletonMonoBehaviour<ShopUIAdapter>
{
    public enum Tab { Item, Tool, Storage, Farm }

    private GameObject bookInstance;
    private Transform  listContent;
    private TextMeshProUGUI headerLabel;
    private ShopDetailPanel detailPanel;
    private Button closeButton;
    private Button[] bookmarkButtons = new Button[4];
    private Button refreshButton;
    private TextMeshProUGUI refreshCostLabel;

    private GameObject cachedRowPrefab;
    private readonly List<ShopListRow> currentRows = new();
    private ShopListRow selectedRow;

    private Tab currentTab = Tab.Item;

    // ADR-008 — GameSessionRoot 하위 서비스는 캐시 후 재사용. .Instance 반복 금지.
    private PurchaseService purchase;
    private StatsService stats;
    private ProgressService progress;
    private ToolUpgradeService toolUpgrade;
    private StorageUpgradeService storageUpgrade;
    private FarmUpgradeService farmUpgrade;

    private const float BookmarkWidthUnselected = 70f;
    private const float BookmarkWidthSelected   = 105f;
    private const float BookmarkHeight          = 45f;

    protected override void OnSingletonAwake()
    {
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        SpawnBook();
    }

    private void SpawnBook()
    {
        var prefab = CatalogProvider.Prefabs?.shopBook;
        if (prefab == null) { Debug.LogError("[ShopUIAdapter] ShopBook prefab not in PrefabCatalog"); return; }

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
        var refs = bookInstance.GetComponent<ShopBookRefs>();
        if (refs == null)
        {
            Debug.LogError("[ShopUIAdapter] ShopBook prefab missing ShopBookRefs component");
            return;
        }

        listContent = refs.ListContent;
        headerLabel = refs.HeaderLabel;
        detailPanel = refs.DetailPanel;
        closeButton = refs.CloseButton;
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

        CacheServices();
        detailPanel?.Inject(purchase, toolUpgrade, storageUpgrade, farmUpgrade);

        var bookmarks = refs.BookmarkButtons;
        for (int i = 0; i < bookmarkButtons.Length && i < (bookmarks?.Length ?? 0); i++)
        {
            bookmarkButtons[i] = bookmarks[i];
            if (bookmarkButtons[i] == null) continue;
            int idx = i;
            bookmarkButtons[i].onClick.AddListener(() => SwitchTab((Tab)idx));
        }

        refreshButton = refs.RefreshButton;
        refreshCostLabel = refs.RefreshCostLabel;
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);

        bookInstance.SetActive(false);
    }

    /// <summary>ADR-008 — GameSessionRoot 루트에서 하위 서비스를 한 번 fetch해서 필드에 보관.</summary>
    private void CacheServices()
    {
        var session = GameSessionRoot.Instance;
        if (session == null) return;
        purchase       = session.Purchase;
        stats          = session.Stats;
        progress       = session.Progress;
        toolUpgrade    = session.ToolUpgrade;
        storageUpgrade = session.StorageUpgrade;
        farmUpgrade    = session.FarmUpgrade;
    }

    public void OpenShop(Tab tab = Tab.Item)
    {
        if (UILockManager.IsLocked) return;
        if (bookInstance == null) return;

        UILockManager.Lock(UILockManager.Owner.Shop);
        bookInstance.SetActive(true);
        SoundManager.Instance?.PlayUIBook();
        SoundManager.Instance?.RegisterButtons(bookInstance.transform);
        SwitchTab(tab);
    }

    public void CloseShop()
    {
        ClearRows();
        if (bookInstance != null) bookInstance.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.Shop);
    }

    private void Update()
    {
        if (bookInstance != null && bookInstance.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseShop();
    }

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

        // Populate 이후에 갱신 — GetItemList가 phase 감지하여 _refreshCount를
        // 리셋한 뒤 GetRefreshCost가 호출되게. 이전 페이즈 stale 비용 표시 방지.
        UpdateRefreshButton();
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
        if (cachedRowPrefab == null) cachedRowPrefab = CatalogProvider.Prefabs?.shopRow;
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
            case ItemRowData item:    detailPanel?.ShowItem(item.Food, item.UnitPrice, item.IsUnlimited ? int.MaxValue : item.RemainingStock); break;
            case ToolRowData tool:    ShowToolDetail(tool.Id);    break;
            case StorageRowData stg:  ShowStorageDetail(stg.Type); break;
            case FarmRowData farm:    ShowFarmDetail(farm.Type);   break;
        }
    }

    private void PopulateItemList()
    {
        if (purchase == null) return;

        // 라인업 키: (day, phase). BasicStats.day 폐기됨 — SSOT는 PhaseData.
        int day = progress?.PhaseData?.Day ?? 0;
        int phaseIndex = (int)(progress?.PhaseData?.Phase ?? PhaseType.Preparation);
        var slots = purchase.GetItemList(day, phaseIndex);

        foreach (var info in slots)
        {
            int remaining = purchase.GetRemaining(info);
            int price = info.item.ingredient != null ? info.item.ingredient.defaultPrice : 0;

            // General(unlimited)은 재고 표시 없음. Special만 "재고 N" (0도 표시).
            string leftSub = info.IsUnlimited ? "" : $"재고 {remaining}";

            AddRow(
                info.item.image,
                info.item.ingredientName,
                leftSub,
                $"{price}G",
                new ItemRowData
                {
                    Food = info.item,
                    UnitPrice = price,
                    IsUnlimited = info.IsUnlimited,
                    InitialStock = info.stock,
                    RemainingStock = remaining
                }
            );
        }
    }

    private void OnRefreshClicked()
    {
        if (purchase == null || currentTab != Tab.Item) return;
        int day = progress?.PhaseData?.Day ?? 0;
        int phaseIndex = (int)(progress?.PhaseData?.Phase ?? PhaseType.Preparation);
        if (!purchase.TryRefresh(day, phaseIndex)) return;
        SoundManager.Instance?.PlayButtonClick();
        SwitchTab(currentTab); // 새 라인업 재populate
    }

    /// <summary>Item 탭일 때만 보이고, cost/가용성 라벨 업데이트.</summary>
    private void UpdateRefreshButton()
    {
        bool showRefresh = currentTab == Tab.Item;
        if (refreshButton != null) refreshButton.gameObject.SetActive(showRefresh);
        if (!showRefresh || purchase == null) return;

        int cost = purchase.GetRefreshCost();
        if (refreshCostLabel != null) refreshCostLabel.text = $"{cost}G";
        if (refreshButton != null) refreshButton.interactable = purchase.CanRefresh();
    }

    public void NotifyItemPurchased(FoodData item, int qty)
    {
        // NotifyPurchased는 PurchaseService.TryBuy에서 이미 호출됨.
        // 여기는 순수 UI 갱신 — 중복 호출 시 재고 2배 차감 버그 재발.
        foreach (var row in currentRows)
        {
            if (row.UserData is ItemRowData d && d.Food == item)
            {
                if (!d.IsUnlimited)
                {
                    d.RemainingStock = purchase != null
                        ? Mathf.Max(0, d.InitialStock - purchase.GetPurchasedThisPhase(item))
                        : d.RemainingStock;
                    row.SetData(item.image, item.ingredientName,
                        $"재고 {d.RemainingStock}", $"{d.UnitPrice}G", d);
                }
                if (selectedRow == row) detailPanel?.ShowItem(d.Food, d.UnitPrice, d.IsUnlimited ? int.MaxValue : d.RemainingStock);
                break;
            }
        }
    }

    public void NotifyUpgradeApplied() => SwitchTab(currentTab);

    private class ItemRowData
    {
        public FoodData Food;
        public int UnitPrice;
        public bool IsUnlimited;
        public int InitialStock;   // Special만 의미 있음 (원 재고)
        public int RemainingStock; // Special만 의미 있음
    }
    private class ToolRowData    { public string Id; }
    private class StorageRowData { public string Type; }
    private class FarmRowData    { public string Type; }
}
