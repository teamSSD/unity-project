using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>텃밭 활용 극대화 정책. Baseline 기반이지만:
    /// - 업그레이드 우선순위 Farm > Tool > Storage
    /// - 재료는 오직 general만 최소 매수 (수확 crop이 대부분 커버)</summary>
    public class FarmMaxPolicy : IPlayerPolicy
    {
        public string Name => "FarmMax";

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            return ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .OrderByDescending(m => EstimateMargin(ctx, m.id))
                .Take(3).Select(f => f.id).ToArray();
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase) => PhaseAction.Shopping;

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int max = money / 5;
            bool CanAfford(int c) => c <= max && (money - c) >= 5000;

            // Farm 우선
            foreach (var f in new[] { "tile", "harvestCount", "timeReduction" })
            {
                var next = ctx.FarmUpgrade.GetNextData(f);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "farm", trackId = f };
            }
            foreach (var t in new[] { "T001", "T002", "T003", "T004", "T005" })
            {
                var next = ctx.ToolUpgrade.GetNextData(t);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = t };
            }
            foreach (var s in new[] { "refrigerator", "upperShelf", "lowerShelf" })
            {
                var next = ctx.StorageUpgrade.GetNextData(s);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "storage", trackId = s };
            }
            return new UpgradeDecision { doUpgrade = false };
        }

        public List<PurchaseDecision> DecidePurchases(object simContext)
        {
            var ctx = (SimContext)simContext;
            int budget = System.Math.Max(0, ctx.Stats.GetMoney() - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            var need = ComputeNeeds(ctx, targetPerMain: 8);
            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            var candidates = slots
                .Where(s => s.item?.type == FoodType.INGREDIENT && s.type == ProductType.General)
                .Select(s => (info: s, price: s.item.ingredient?.defaultPrice ?? 0,
                              needed: NeedOf(need, s.item.id, ctx)))
                .Where(x => x.price > 0 && x.needed > 0)
                .OrderByDescending(x => x.needed).ThenBy(x => x.price)
                .ToList();

            foreach (var (info, price, needed) in candidates)
            {
                int qty = System.Math.Min(needed, budget / price);
                if (qty <= 0) continue;
                decisions.Add(new PurchaseDecision { foodId = info.item.id, qty = qty });
                budget -= qty * price;
                if (budget <= 0) break;
            }
            return decisions;
        }

        public ServingDecision DecideServing(object simContext, string mainFoodId) =>
            new ServingDecision { sideFoodIds = new List<string>(), accuracy = 0.85f };

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
        }

        private Dictionary<string, int> ComputeNeeds(SimContext ctx, int targetPerMain)
        {
            var totals = new Dictionary<string, int>();
            var mainIds = ctx.SelectedMenuIds ?? new string[0];
            foreach (var mid in mainIds)
                foreach (var kv in ctx.Recipes.LeafIngredients(mid))
                {
                    totals.TryGetValue(kv.Key, out int cur);
                    totals[kv.Key] = cur + kv.Value * targetPerMain;
                }
            return totals;
        }

        private int NeedOf(Dictionary<string, int> need, string foodId, SimContext ctx)
        {
            if (!need.TryGetValue(foodId, out int target)) return 0;
            if (!ctx.FoodById.TryGetValue(foodId, out var food) || food == null) return 0;
            return System.Math.Max(0, target - ctx.Inventory.CheckStockAmount(food));
        }
    }
}
