using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Cooking;
using Game.Domain.Garden;
using Game.Domain.Mall;
using Game.Domain.Shop;
using Game.Editor.Simulation.Cooking;
using Game.Editor.Simulation.Events;
using Game.Editor.Simulation.Policies;
using Game.Schema.State;

namespace Game.Editor.Simulation
{
    /// <summary>
    /// Sim 실행 컨텍스트. GameSessionRoot.WireServices 흐름을 headless로 복제.
    /// 상태(GameState) + POCO 서비스들 + policy + 이벤트 로그를 담음.
    /// SimHarness/CookingSimulator/BaselinePolicy가 공유.
    /// </summary>
    public class SimContext
    {
        // ── 상태 ──
        public GameState State { get; private set; }

        // ── 서비스 (POCO) ──
        public StatsService Stats { get; private set; }
        public WeatherService Weather { get; private set; }
        public SettlementService Settlement { get; private set; }
        public InventoryService Inventory { get; private set; }
        public MenuSelectionService MenuSelection { get; private set; }
        public UnlockedFoodService UnlockedFood { get; private set; }
        public RecipeLookupService RecipeLookup { get; private set; }
        public CropCatalogService CropCatalog { get; private set; }
        public FarmUpgradeService FarmUpgrade { get; private set; }
        public StorageUpgradeService StorageUpgrade { get; private set; }
        public ToolUpgradeService ToolUpgrade { get; private set; }
        public PurchaseService Purchase { get; private set; }
        public OrderService Order { get; private set; }

        // ── Headless 대체 ──
        public HeadlessProgress Progress { get; private set; }
        public DirectMoneyAdapter Money { get; private set; }
        public DirectExpenseAdapter Expense { get; private set; }

        // ── Sim infra ──
        public SimEventLog Log { get; private set; }
        public IPlayerPolicy Policy { get; set; }
        public int Seed { get; private set; }
        public bool Bankrupt { get; set; }
        /// <summary>Preparation에서 정책이 선택한 3 메뉴 main id. 이후 페이즈들이 참조.</summary>
        public string[] SelectedMenuIds { get; set; }

        /// <summary>튜닝용 game config (upgrade cost, side coef 등). null이면 기본값.</summary>
        public SimGameConfig GameConfig { get; private set; }

        // ── 카탈로그 (레시피 해상용) ──
        public RecipeIngredientResolver Recipes { get; private set; }
        public IReadOnlyDictionary<string, int> IngredientPriceById { get; private set; }
        public IReadOnlyDictionary<string, FoodData> FoodById { get; private set; }
        public FoodShopConfigSO ShopConfig { get; private set; }

        /// <summary>새 컨텍스트 조립. config 넘겨 upgrade 비용/side 계수 등 override 가능.</summary>
        public static SimContext Build(int seed, SimGameConfig gameConfig = null)
        {
            var ctx = new SimContext();
            ctx.Seed = seed;
            ctx.GameConfig = gameConfig ?? new SimGameConfig();
            ctx.Log = new SimEventLog();
            int startMoney = ctx.GameConfig.startingMoney;

            // 카탈로그 로드
            var cats = CatalogLoader.LoadAll();
            if (cats.food == null || cats.recipe == null || cats.ingredient == null || cats.csvs == null)
                throw new System.Exception("[Sim] 필수 카탈로그 SO 미로드");

            ctx.FoodById = CatalogLoader.BuildFoodMap(cats.food);
            ctx.IngredientPriceById = CatalogLoader.BuildIngredientPriceMap(cats.ingredient);
            ctx.ShopConfig = cats.foodShopConfig;
            ctx.Recipes = new RecipeIngredientResolver(cats.food.All, cats.recipe.All);

            // GameState
            ctx.State = new GameState();
            ctx.State.stats.money = startMoney;
            ctx.State.stats.stamina = 100;
            ctx.State.phase.Day = 0;
            ctx.State.phase.Phase = PhaseType.Preparation;

            // 서비스 조립 (GameSessionRoot 순서 재현)
            ctx.Stats = new StatsService(() => ctx.State.stats);

            // Weather + Settlement 먼저 (upgrade 서비스가 필요)
            ctx.Weather = new WeatherService();
            ctx.Settlement = new SettlementService();

            // Adapters (Stats/Settlement 필요)
            ctx.Money = new DirectMoneyAdapter(ctx.Stats);
            ctx.Expense = new DirectExpenseAdapter(ctx.Settlement);

            // 업그레이드 CSV 파싱 + config로 비용 조정
            var farmRows = CsvModelConverter.Parse<FarmUpgradeData>(cats.csvs.farmUpgrade);
            var storageRows = CsvModelConverter.Parse<StorageUpgradeData>(cats.csvs.storageUpgrade);
            var toolRows = CsvModelConverter.Parse<ToolUpgradeData>(cats.csvs.toolUpgrade);
            var cropRows = CsvModelConverter.Parse<CropData>(cats.csvs.cropData);

            float upMul = ctx.GameConfig.upgradeCostMultiplier;
            float lateMul = ctx.GameConfig.lateUpgradeCostMultiplier;
            // L1-L2 = upMul. L3+ = upMul × lateMul (endgame gate).
            foreach (var r in farmRows) r.cost = (int)(r.cost * (r.level >= 3 ? upMul * lateMul : upMul));
            foreach (var r in storageRows) r.cost = (int)(r.cost * (r.level >= 3 ? upMul * lateMul : upMul));
            foreach (var r in toolRows) r.cost = (int)(r.cost * (r.level >= 3 ? upMul * lateMul : upMul));

            ctx.CropCatalog = new CropCatalogService(cropRows);
            ctx.FarmUpgrade = new FarmUpgradeService(ctx.State.garden.persistent, farmRows, ctx.Money, ctx.Expense);
            ctx.StorageUpgrade = new StorageUpgradeService(ctx.State.shop.persistent, storageRows, ctx.Money, ctx.Expense);
            ctx.ToolUpgrade = new ToolUpgradeService(ctx.State.shop.persistent, toolRows, ctx.Money, ctx.Expense);

            ctx.Inventory = new InventoryService(cats.food.All, ctx.StorageUpgrade);
            ctx.Purchase = new PurchaseService(cats.foodShopConfig, ctx.Inventory, ctx.Money, ctx.Expense);

            ctx.MenuSelection = new MenuSelectionService(cats.food.All);
            ctx.UnlockedFood = new UnlockedFoodService(cats.food.All);
            ctx.RecipeLookup = new RecipeLookupService(cats.recipe.All);

            ctx.Order = new OrderService(ctx.Money);

            // HeadlessProgress
            ctx.Progress = new HeadlessProgress(ctx.State.phase, ctx.Stats, ctx.Inventory, ctx.Weather, ctx.Settlement);

            // RNG seeds
            GameRandom.InitSession(seed, seed ^ 0x5F3759DF);
            GameRandom.InitDay(0);

            // 초기 default 재료 + 기본 레시피
            ctx.Inventory.ResetToDefault();
            ctx.UnlockedFood.UnlockDefaultRecipes();
            if (ctx.GameConfig.unlockAllMenus) ctx.UnlockedFood.UnlockAll();
            ctx.Weather.UpdateWeather(0);
            ctx.Settlement.Reset(ctx.Stats.GetMoney());

            return ctx;
        }
    }
}
