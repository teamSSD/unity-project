using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>사이드 붙임 정책. 서빙마다 사이드 1개 (경제 안 무너지는 최소) + 그 재료도 구매.
    /// 사이드가 정말 순손실인지 검증 목적. side coefficient sweep의 subject.
    /// 원래 3개였는데 초기 예산 부족으로 못 감당 → 1개로 축소 (여전히 side 유무 효과 측정 가능).</summary>
    public class SideStuffedPolicy : IPlayerPolicy
    {
        public string Name => "SideStuffed";

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
            foreach (var t in new[] { "T001", "T002", "T003", "T004", "T005" })
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
            int budget = System.Math.Max(0, ctx.Stats.GetMoney() - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            // 하루 8 손님 × main 1 + side 3 = 4 요리씩. 재료 수요 대폭 증가.
            var need = ComputeNeeds(ctx, expectedCustomersPerDay: 8);

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
            var sides = ctx.FoodById.Values
                .Where(f => f?.type == FoodType.SIDE && ctx.UnlockedFood.IsUnlocked(f.id))
                .Take(1).Select(f => f.id).ToList(); // 사이드 1개만 (초기 예산 감안)
            return new ServingDecision { sideFoodIds = sides, accuracy = 0.85f };
        }

        // ── 헬퍼 ──

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
        }

        /// <summary>메인 + 사이드 모든 leaf 재료 수요 계산. SideStuffed는 사이드도 필요하므로 명시 포함.</summary>
        private Dictionary<string, int> ComputeNeeds(SimContext ctx, int expectedCustomersPerDay)
        {
            var totals = new Dictionary<string, int>();

            // 메인: 선택된 3개
            string[] mainIds = ctx.SelectedMenuIds != null && ctx.SelectedMenuIds.Length > 0
                ? ctx.SelectedMenuIds
                : ctx.FoodById.Values
                    .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                    .Take(3).Select(f => f.id).ToArray();
            int perMain = expectedCustomersPerDay;
            foreach (var mid in mainIds) AddLeavesFor(ctx, mid, perMain, totals);

            // 사이드: 서빙마다 1개 (DecideServing과 일치). unlocked SIDE 첫 1개.
            var sideIds = ctx.FoodById.Values
                .Where(f => f?.type == FoodType.SIDE && ctx.UnlockedFood.IsUnlocked(f.id))
                .Take(1).Select(f => f.id).ToArray();
            foreach (var sid in sideIds) AddLeavesFor(ctx, sid, expectedCustomersPerDay, totals);

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
    }
}
