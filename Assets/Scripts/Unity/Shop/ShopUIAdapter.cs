using System.Collections.Generic;
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

    private GameObject cachedRowPrefab;
    private readonly List<ShopListRow> currentRows = new();
    private ShopListRow selectedRow;

    private Tab currentTab = Tab.Item;

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
        var book = bookInstance.transform;

        listContent = book.Find("Page/ShopPage/Page_L/Scroll/Viewport/Content");
        var headerT = book.Find("Page/ShopPage/Page_L/Header");
        if (headerT != null) headerLabel = headerT.GetComponent<TextMeshProUGUI>();
        var pageR = book.Find("Page/ShopPage/Page_R");
        if (pageR != null) detailPanel = pageR.GetComponent<ShopDetailPanel>();

        closeButton = book.Find("Button_Close")?.GetComponent<Button>();
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

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
            case ItemRowData item:    detailPanel?.ShowItem(item.Food, item.UnitPrice, item.RemainingStock); break;
            case ToolRowData tool:    ShowToolDetail(tool.Id);    break;
            case StorageRowData stg:  ShowStorageDetail(stg.Type); break;
            case FarmRowData farm:    ShowFarmDetail(farm.Type);   break;
        }
    }

    private void PopulateItemList()
    {
        var purchase = GameSessionRoot.Instance?.Purchase;
        if (purchase == null) return;

        int today = GameSessionRoot.Instance?.Stats.GetDay() ?? 0;
        var slots = purchase.GetItemListForDay(today);

        foreach (var info in slots)
        {
            int remaining = purchase.GetRemaining(info);
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
        var purchase = GameSessionRoot.Instance?.Purchase;
        purchase?.NotifyPurchased(item, qty);

        foreach (var row in currentRows)
        {
            if (row.UserData is ItemRowData d && d.Food == item)
            {
                d.RemainingStock = purchase != null
                    ? Mathf.Max(0, d.Stock - purchase.GetPurchasedToday(item))
                    : d.RemainingStock;
                row.SetData(item.image, item.ingredientName,
                    $"재고 {d.RemainingStock}/{d.Stock}", $"{d.UnitPrice}G", d);
                if (selectedRow == row) detailPanel?.ShowItem(d.Food, d.UnitPrice, d.RemainingStock);
                break;
            }
        }
    }

    public void NotifyUpgradeApplied() => SwitchTab(currentTab);

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
