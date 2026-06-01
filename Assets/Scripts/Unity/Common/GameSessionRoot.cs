using Game.Domain.Common;
using Game.Domain.Garden;
using Game.Domain.Mall;
using Game.Domain.Shop;
using Game.Schema.State;
using UnityEngine;

/// <summary>
/// Composition Root (ADR-001 Option B).
/// GameState POCO 보관 + 모든 Service의 wiring 진입점.
/// Managers 씬에 단 1개 배치 (기존 Singleton 매니저들이 점진 흡수됨).
///
/// DefaultExecutionOrder(-999): CatalogProvider(-1000) 다음으로 Awake — 같은 씬의
/// 다른 SingletonMonoBehaviour(StatsSystem 등) facade가 Awake 시 GameSessionRoot.State
/// 안전 접근 보장.
///
/// Phase 3-C 진행에 따라 Service 필드/wiring이 추가됨.
/// </summary>
[DefaultExecutionOrder(-999)]
public class GameSessionRoot : SingletonMonoBehaviour<GameSessionRoot>
{
    public GameState State { get; private set; }

    public StatsService Stats { get; private set; }
    public CropCatalogService CropCatalog { get; private set; }
    public FarmUpgradeService FarmUpgrade { get; private set; }
    public StorageUpgradeService StorageUpgrade { get; private set; }
    public ToolUpgradeService ToolUpgrade { get; private set; }
    public PurchaseService Purchase { get; private set; }
    public DeliveryQuestService DeliveryQuest { get; private set; }
    public OrderService Order { get; private set; }
    public QuestMenuCatalog QuestMenus { get; private set; }
    public InventoryService Inventory { get; private set; }

    protected override void OnSingletonAwake()
    {
        State = new GameState();
        WireServices();
    }

    private void WireServices()
    {
        // 글로벌 통계 — GameState.stats 라이브 참조 closure
        Stats = new StatsService(() => State?.stats);

        var cropRows = CsvModelConverter.Parse<CropData>(CatalogProvider.Csvs?.cropData);
        foreach (var row in cropRows)
            row.sprite = CatalogProvider.CropSprites?.Get(row.imagePath);
        CropCatalog = new CropCatalogService(cropRows);

        var money = new StatsMoneyAdapter();
        var expense = new SettlementExpenseAdapter();

        var farmRows = CsvModelConverter.Parse<FarmUpgradeData>(CatalogProvider.Csvs?.farmUpgrade);
        FarmUpgrade = new FarmUpgradeService(State.garden.persistent, farmRows, money, expense);

        var storageRows = CsvModelConverter.Parse<StorageUpgradeData>(CatalogProvider.Csvs?.storageUpgrade);
        StorageUpgrade = new StorageUpgradeService(State.shop.persistent, storageRows, money, expense);

        var toolRows = CsvModelConverter.Parse<ToolUpgradeData>(CatalogProvider.Csvs?.toolUpgrade);
        ToolUpgrade = new ToolUpgradeService(State.shop.persistent, toolRows, money, expense);

        // Inventory를 Purchase보다 먼저 wiring (Purchase가 의존)
        var foodCatalog = CatalogProvider.Food?.All;
        Inventory = new InventoryService(foodCatalog, StorageUpgrade);

        Purchase = new PurchaseService(CatalogProvider.FoodShopConfig, Inventory, money, expense);

        DeliveryQuest = new DeliveryQuestService(State.mall.persistent);
        Order = new OrderService(money);
        QuestMenus = new QuestMenuCatalog(ParseQuestMenus());
    }

    private static System.Collections.Generic.IEnumerable<(string groupId, MenuSchema menu)> ParseQuestMenus()
    {
        var csv = CatalogProvider.Csvs?.deliveryQuest;
        if (csv == null) yield break;

        // CSV 헤더: GroupId, MenuName, MainMenuId, MainMenu2Id, SideMenu1Id, SideMenu2Id, SideMenu3Id
        var lines = csv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var t = lines[i].Split(',');
            if (t.Length < 2) continue;

            string gId = t[0].Trim();
            string menuName = t[1].Trim();

            var mains = new System.Collections.Generic.List<FoodData>();
            for (int j = 2; j <= 3 && j < t.Length; j++)
            {
                string id = t[j].Trim();
                if (!string.IsNullOrEmpty(id))
                    mains.Add(SearchDataUtil.GetFoodDataById(id));
            }

            var sides = new System.Collections.Generic.List<FoodData>();
            for (int j = 4; j < Mathf.Min(t.Length, 7); j++)
            {
                string id = t[j].Trim();
                if (!string.IsNullOrEmpty(id))
                    sides.Add(SearchDataUtil.GetFoodDataById(id));
            }

            yield return (gId, new MenuSchema(menuName, -1, mains, sides));
        }
    }
}
