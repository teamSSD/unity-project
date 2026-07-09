using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>공격적 플레이. 최대 소비 + 최대 요리:
    /// - Baseline과 동일한 메뉴 선택 (마진 top 3, feasibility 무시)
    /// - Afternoon/Evening/Night도 Work (모든 페이즈 요리)
    /// - 업그레이드 자산 40% 넘으면 무조건 (공격적)
    /// - 재료는 각 메뉴 손님 수 × 여유율 1.5 (worst case + buffer)
    /// - 미니게임 정확도 0.90 (숙련자)
    /// - 사이드 안 붙임 (BaselinePolicy 결론 — 순손실)</summary>
    public class AggressivePolicy : IPlayerPolicy
    {
        public string Name => "Aggressive";

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            return ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .OrderByDescending(m => EstimateMargin(ctx, m.id))
                .Take(3).Select(f => f.id).ToArray();
        }

        // Morning은 자동 Work. Afternoon/Evening/Night도 요리 우선.
        // 하지만 Shopping도 필요하므로 짝수 페이즈만 Work, 홀수는 Shopping — 재료 리필 사이클.
        public PhaseAction DecidePhaseAction(object simContext, int phase)
        {
            // Afternoon=2 Work, Evening=3 Shopping, Night=4 Work
            return phase switch
            {
                2 => PhaseAction.Work,
                3 => PhaseAction.Shopping,
                4 => PhaseAction.Work,
                _ => PhaseAction.Shopping,
            };
        }

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int threshold = money * 4 / 10; // 40% — 공격적

            bool CanAfford(int cost) => cost <= threshold && (money - cost) >= 2000;

            foreach (var toolId in new[] { "T001", "T002", "T003", "T004", "T005" })
            {
                var next = ctx.ToolUpgrade.GetNextData(toolId);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = toolId };
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
            int budget = System.Math.Max(0, ctx.Stats.GetMoney() - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            // 재료 목표: Morning + Aft + Night 세 페이즈 요리 → 8 + 5 + 3 = 16 손님. perMain=12로 절제.
            var need = ComputeNeeds(ctx, targetPerMain: 12);

            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            var candidates = slots
                .Where(s => s.item?.type == FoodType.INGREDIENT)
                .Select(s => (info: s, price: s.item.ingredient?.defaultPrice ?? 0,
                              needed: NeedOf(need, s.item.id, ctx)))
                .Where(x => x.price > 0 && x.needed > 0)
                .OrderByDescending(x => x.needed)
                .ThenBy(x => x.price)
                .ToList();

            foreach (var (info, price, needed) in candidates)
            {
                int affordable = budget / price;
                int cap = info.type == ProductType.Special ? ctx.Purchase.GetRemaining(info) : int.MaxValue;
                int qty = System.Math.Min(System.Math.Min(needed, affordable), cap);
                if (qty <= 0) continue;
                decisions.Add(new PurchaseDecision { foodId = info.item.id, qty = qty });
                budget -= qty * price;
                if (budget <= 0) break;
            }
            return decisions;
        }

        public ServingDecision DecideServing(object simContext, string mainFoodId) =>
            new ServingDecision { sideFoodIds = new List<string>(), accuracy = 0.90f };

        // ── 헬퍼 ──
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
            {
                var leaves = ctx.Recipes.LeafIngredients(mid);
                foreach (var kv in leaves)
                {
                    totals.TryGetValue(kv.Key, out int cur);
                    totals[kv.Key] = cur + kv.Value * targetPerMain;
                }
            }
            return totals;
        }

        private int NeedOf(Dictionary<string, int> need, string foodId, SimContext ctx)
        {
            if (!need.TryGetValue(foodId, out int target)) return 0;
            if (!ctx.FoodById.TryGetValue(foodId, out var food) || food == null) return 0;
            int have = ctx.Inventory.CheckStockAmount(food);
            return System.Math.Max(0, target - have);
        }
    }
}
