using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>4축 조합 실험용 파라미터화된 정책.
    /// - useFarm: Farm crop 매칭 메뉴 선택 (false면 마진 top-3)
    /// - useSide: 자산 threshold 이후 사이드 추가 (false면 사이드 없음)
    /// - aggressiveUpgrade: true면 자산 40% 이내 소진 + 초반 유예 없음 (false면 20% + 유예)
    /// - skillHigh: 요리 accuracy 0.85 (false면 0.60 = 초보)
    /// </summary>
    public class MatrixPolicy : IPlayerPolicy
    {
        public string Name { get; }

        public bool useFarm;
        public bool useSide;
        public bool aggressiveUpgrade;
        public bool skillHigh;

        private bool _sideModeLatched = false;
        private const int SideAdoptionThreshold = 100_000;

        public MatrixPolicy(bool useFarm, bool useSide, bool aggressiveUpgrade, bool skillHigh)
        {
            this.useFarm = useFarm;
            this.useSide = useSide;
            this.aggressiveUpgrade = aggressiveUpgrade;
            this.skillHigh = skillHigh;
            Name = $"M_{(useFarm ? "F" : "-")}{(useSide ? "S" : "-")}{(aggressiveUpgrade ? "U" : "u")}{(skillHigh ? "K" : "k")}";
        }

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            var candidates = ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id));

            if (useFarm)
            {
                var farmIds = CollectFarmAvailableCrops(ctx);
                return candidates
                    .Select(m => (id: m.id,
                                  fmatch: CountFarmLeaves(ctx, m.id, farmIds),
                                  margin: EstimateMargin(ctx, m.id)))
                    .OrderByDescending(x => x.fmatch)
                    .ThenByDescending(x => x.margin)
                    .Take(3).Select(x => x.id).ToArray();
            }
            return candidates
                .OrderByDescending(m => EstimateMargin(ctx, m.id))
                .Take(3).Select(f => f.id).ToArray();
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase) => PhaseAction.Shopping;

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int currentDay = ctx.State.phase.Day;

            if (!aggressiveUpgrade)
            {
                if (currentDay < 3) return new UpgradeDecision { doUpgrade = false };
                if (!RecentRevenueHealthy(ctx, currentDay, 3))
                    return new UpgradeDecision { doUpgrade = false };
            }

            int maxCost = aggressiveUpgrade ? money * 2 / 5 : money / 5;
            int minReserve = aggressiveUpgrade ? 2000 : 5000;
            bool CanAfford(int c) => c <= maxCost && (money - c) >= minReserve;

            foreach (var t in new[] { "T001", "T002", "T003", "T004", "T005" })
            {
                var next = ctx.ToolUpgrade.GetNextData(t);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = t };
            }
            foreach (var stg in new[] { "refrigerator", "upperShelf", "lowerShelf" })
            {
                var next = ctx.StorageUpgrade.GetNextData(stg);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "storage", trackId = stg };
            }
            foreach (var farm in new[] { "tile", "harvestCount", "timeReduction" })
            {
                var next = ctx.FarmUpgrade.GetNextData(farm);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "farm", trackId = farm };
            }
            return new UpgradeDecision { doUpgrade = false };
        }

        public List<PurchaseDecision> DecidePurchases(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int budget = System.Math.Max(0, money - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            if (useSide && !_sideModeLatched && money >= SideAdoptionThreshold) _sideModeLatched = true;

            var need = ComputeNeeds(ctx, expectedCustomersPerDay: 4, includeSide: _sideModeLatched);

            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            var candidates = slots
                .Where(s => s.item?.type == FoodType.INGREDIENT)
                .Select(s => (info: s, price: s.item.ingredient?.defaultPrice ?? 0,
                              needed: NeedOf(need, s.item.id, ctx)))
                .Where(x => x.price > 0 && x.needed > 0)
                .OrderByDescending(x => x.needed).ThenBy(x => x.price)
                .ToList();

            foreach (var (info, price, needed) in candidates)
            {
                int cap = info.type == ProductType.Special ? ctx.Purchase.GetRemaining(info) : int.MaxValue;
                int qty = System.Math.Min(System.Math.Min(needed, budget / price), cap);
                if (qty <= 0) continue;
                decisions.Add(new PurchaseDecision { foodId = info.item.id, qty = qty });
                budget -= qty * price;
                if (budget <= 0) break;
            }
            return decisions;
        }

        public ServingDecision DecideServing(object simContext, string mainFoodId)
        {
            var ctx = (SimContext)simContext;
            var sideIds = new List<string>();
            float accuracy = skillHigh ? 0.85f : 0.60f;

            if (useSide)
            {
                if (!_sideModeLatched && ctx.Stats.GetMoney() >= SideAdoptionThreshold) _sideModeLatched = true;
                if (_sideModeLatched)
                {
                    var side = ctx.FoodById.Values
                        .FirstOrDefault(f => f?.type == FoodType.SIDE && ctx.UnlockedFood.IsUnlocked(f.id));
                    if (side != null && HasIngredientsFor(ctx, side.id))
                        sideIds.Add(side.id);
                }
            }
            return new ServingDecision { sideFoodIds = sideIds, accuracy = accuracy };
        }

        // ── 헬퍼 ──

        private HashSet<string> CollectFarmAvailableCrops(SimContext ctx)
        {
            var set = new HashSet<string>();
            var tiles = ctx.State.garden.persistent?.tiles;
            if (tiles != null)
            {
                foreach (var tile in tiles)
                    if (tile != null && !string.IsNullOrEmpty(tile.cropId)) set.Add(tile.cropId);
            }
            foreach (var cropId in SimGameConfig.FarmCropIds)
            {
                if (ctx.FoodById.TryGetValue(cropId, out var food) && food != null
                    && ctx.Inventory.CheckStockAmount(food) > 0)
                    set.Add(cropId);
            }
            return set;
        }

        private int CountFarmLeaves(SimContext ctx, string foodId, HashSet<string> farmCropIds)
        {
            var leaves = ctx.Recipes.LeafIngredients(foodId);
            int match = 0;
            foreach (var kv in leaves) if (farmCropIds.Contains(kv.Key)) match++;
            return match;
        }

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
        }

        private bool HasIngredientsFor(SimContext ctx, string foodId)
        {
            var leaves = ctx.Recipes.LeafIngredients(foodId);
            foreach (var kv in leaves)
            {
                if (!ctx.FoodById.TryGetValue(kv.Key, out var f) || f == null) return false;
                if (ctx.Inventory.CheckStockAmount(f) < kv.Value) return false;
            }
            return true;
        }

        private Dictionary<string, int> ComputeNeeds(SimContext ctx, int expectedCustomersPerDay, bool includeSide)
        {
            var totals = new Dictionary<string, int>();
            string[] mainIds = ctx.SelectedMenuIds != null && ctx.SelectedMenuIds.Length > 0
                ? ctx.SelectedMenuIds
                : ctx.FoodById.Values
                    .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                    .Take(3).Select(f => f.id).ToArray();
            foreach (var mid in mainIds) AddLeavesFor(ctx, mid, expectedCustomersPerDay, totals);

            if (includeSide)
            {
                var sideIds = ctx.FoodById.Values
                    .Where(f => f?.type == FoodType.SIDE && ctx.UnlockedFood.IsUnlocked(f.id))
                    .Take(1).Select(f => f.id).ToArray();
                foreach (var sid in sideIds) AddLeavesFor(ctx, sid, expectedCustomersPerDay, totals);
            }
            return totals;
        }

        private void AddLeavesFor(SimContext ctx, string foodId, int mult, Dictionary<string, int> acc)
        {
            var leaves = ctx.Recipes.LeafIngredients(foodId);
            foreach (var kv in leaves)
            {
                acc.TryGetValue(kv.Key, out int cur);
                acc[kv.Key] = cur + kv.Value * mult;
            }
        }

        private int NeedOf(Dictionary<string, int> need, string foodId, SimContext ctx)
        {
            if (!need.TryGetValue(foodId, out int target)) return 0;
            if (!ctx.FoodById.TryGetValue(foodId, out var food) || food == null) return 0;
            return System.Math.Max(0, target - ctx.Inventory.CheckStockAmount(food));
        }

        private bool RecentRevenueHealthy(SimContext ctx, int currentDay, int windowDays)
        {
            int fromDay = System.Math.Max(0, currentDay - windowDays);
            int totalRevenue = 0;
            foreach (var e in ctx.Log.OfType<Game.Editor.Simulation.Events.CustomerServedEvent>())
            {
                if (e.day >= fromDay && e.day < currentDay) totalRevenue += e.reward;
            }
            return totalRevenue >= Game.Domain.Mall.SettlementService.ManagementFee * windowDays;
        }
    }
}
