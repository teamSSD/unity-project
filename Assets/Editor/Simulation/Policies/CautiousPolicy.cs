using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>보수적 정책. Novice와 Baseline 중간.
    /// - 안전 완충 큼 (자산 30% 유지)
    /// - 재료는 딱 필요량만 (perMain=6, 여유 없이)
    /// - 업그레이드 자산 15% 이내에서만
    /// - 미니게임 정확도 0.80</summary>
    public class CautiousPolicy : IPlayerPolicy
    {
        public string Name => "Cautious";

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
            int max = money * 15 / 100;
            bool CanAfford(int c) => c <= max && (money - c) >= money * 3 / 10; // 30% 여유
            foreach (var t in new[] { "T001", "T002", "T003" })
            {
                var next = ctx.ToolUpgrade.GetNextData(t);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = t };
            }
            return new UpgradeDecision { doUpgrade = false };
        }

        public List<PurchaseDecision> DecidePurchases(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int reserve = money * 3 / 10; // 30% 안전금
            int budget = System.Math.Max(0, money - System.Math.Max(reserve, 1000));
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            var need = ComputeNeeds(ctx, 6); // 손님 절반만 대비 (보수적)
            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            foreach (var s in slots.Where(x => x.item?.type == FoodType.INGREDIENT && x.type == ProductType.General))
            {
                int price = s.item.ingredient?.defaultPrice ?? 0;
                if (price <= 0) continue;
                int n = NeedOf(need, s.item.id, ctx);
                if (n <= 0) continue;
                int qty = System.Math.Min(n, budget / price);
                if (qty <= 0) continue;
                decisions.Add(new PurchaseDecision { foodId = s.item.id, qty = qty });
                budget -= qty * price;
                if (budget <= 0) break;
            }
            return decisions;
        }

        public ServingDecision DecideServing(object simContext, string mainFoodId) =>
            new ServingDecision { sideFoodIds = new List<string>(), accuracy = 0.80f };

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
        }

        private Dictionary<string, int> ComputeNeeds(SimContext ctx, int per)
        {
            var t = new Dictionary<string, int>();
            foreach (var mid in ctx.SelectedMenuIds ?? new string[0])
                foreach (var kv in ctx.Recipes.LeafIngredients(mid))
                {
                    t.TryGetValue(kv.Key, out int c);
                    t[kv.Key] = c + kv.Value * per;
                }
            return t;
        }

        private int NeedOf(Dictionary<string, int> need, string foodId, SimContext ctx)
        {
            if (!need.TryGetValue(foodId, out int target)) return 0;
            if (!ctx.FoodById.TryGetValue(foodId, out var food) || food == null) return 0;
            return System.Math.Max(0, target - ctx.Inventory.CheckStockAmount(food));
        }
    }
}
