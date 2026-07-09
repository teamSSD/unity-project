using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>이론상 최적 근사. 결과가 ceiling — 인간 이렇게까진 못함.
    /// - 메뉴는 원가 대비 마진율 최대 top 3
    /// - 모든 페이즈 요리 (Morning=Work 자동, Aft/Eve/Night도 Work — Shopping은 Preparation에 몰빵)
    /// - Preparation에서 Shopping 안 함 (Shopping은 phase.Preparation에선 불가하므로 별도 처리 필요)
    ///   실제로는 이전 페이즈들에서 사놓은 재고로 다음날 요리
    /// - 업그레이드 자산 50% 넘으면 즉시
    /// - 재료: 각 메뉴 hourly 손님 수 × 여유 2x (넉넉)
    /// - 미니게임 정확도 0.95</summary>
    public class MinMaxPolicy : IPlayerPolicy
    {
        public string Name => "MinMax";

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            return ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .OrderByDescending(m => EstimateMarginRate(ctx, m.id))
                .Take(3).Select(f => f.id).ToArray();
        }

        // 오후=Work (요리), 저녁=Shopping (리필), 밤=Work
        public PhaseAction DecidePhaseAction(object simContext, int phase) => phase switch
        {
            2 => PhaseAction.Work,     // Afternoon: 요리
            3 => PhaseAction.Shopping, // Evening: 다음날 재료 확보
            4 => PhaseAction.Work,     // Night: 요리 (배달 대신)
            _ => PhaseAction.Shopping,
        };

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int threshold = money / 2; // 50%

            bool CanAfford(int cost) => cost <= threshold && (money - cost) >= 1000;

            // Tool > Storage > Farm 순 (미니게임 stamina 감소가 요리 회전율에 직결)
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

            // 3 페이즈 × 8 + 5 + 3 = 16 손님. 여유 없이 정확히 16.
            var need = ComputeNeeds(ctx, targetPerMain: 16);

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
            new ServingDecision { sideFoodIds = new List<string>(), accuracy = 0.95f };

        private float EstimateMarginRate(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            if (cost <= 0) return 0f;
            int estPrice = (int)(cost * 1.5);
            return (float)(estPrice - cost) / cost;
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
