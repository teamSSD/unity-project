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
/// Phase 3-C 진행에 따라 Service 필드/wiring이 추가됨.
/// </summary>
public class GameSessionRoot : SingletonMonoBehaviour<GameSessionRoot>
{
    public GameState State { get; private set; }

    public CropCatalogService CropCatalog { get; private set; }
    public FarmUpgradeService FarmUpgrade { get; private set; }
    public StorageUpgradeService StorageUpgrade { get; private set; }
    public ToolUpgradeService ToolUpgrade { get; private set; }
    public PurchaseService Purchase { get; private set; }
    public DeliveryQuestService DeliveryQuest { get; private set; }
    public OrderService Order { get; private set; }

    protected override void OnSingletonAwake()
    {
        State = new GameState();
        WireServices();
    }

    private void WireServices()
    {
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

        Purchase = new PurchaseService(CatalogProvider.FoodShopConfig);

        DeliveryQuest = new DeliveryQuestService(State.mall.persistent);
        Order = new OrderService(money);
    }
}
