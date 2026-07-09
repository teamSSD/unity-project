using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>자산 임계치 기반 사이드 채택 정책 (히스테리시스 방지판).
    /// - 자산 &lt; SideAdoptionThreshold: Baseline과 동일 (메인만)
    /// - 자산이 임계치 최초 도달 → 이후 "사이드 모드" 영구 유지 (자산 떨어져도 유지)
    /// - 서빙 시 사이드 재료 재고 있으면 붙임 (매수/서빙 불일치 방지)
    /// 초기 안정화 → 후반 마진 확대 패턴 검증용. 15% side 계수 이득 실측 목적.</summary>
    public class AdaptiveSidePolicy : IPlayerPolicy
    {
        public string Name => "AdaptiveSide";

        // Tuning knobs
        public int SideAdoptionThreshold = 100_000; // G. 이 이상 최초 도달 시 side 모드 진입.
        public float CookingAccuracy = 0.85f;

        // 상태 — 임계치 최초 도달 후 latch. 한 번 true 되면 안 내려감.
        private bool _sideModeLatched = false;

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
            int currentDay = ctx.State.phase.Day;

            // Baseline과 동일한 초반 upgrade 유예 로직 (실 유저의 상식적 판단 모사).
            if (currentDay < 3) return new UpgradeDecision { doUpgrade = false };
            if (!RecentRevenueHealthy(ctx, currentDay, windowDays: 3))
                return new UpgradeDecision { doUpgrade = false };

            int max = money / 5;
            bool CanAfford(int c) => c <= max && (money - c) >= 5000;

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

            // Latch: 최초 임계치 도달 후 영구 사이드 모드.
            if (!_sideModeLatched && money >= SideAdoptionThreshold) _sideModeLatched = true;

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

            // 사이드 모드 latch 유지 검사 (Purchase에서 이미 세팅되지만 안전 재확인).
            if (!_sideModeLatched && ctx.Stats.GetMoney() >= SideAdoptionThreshold) _sideModeLatched = true;

            if (_sideModeLatched)
            {
                // 첫 unlocked SIDE 선택. 재고 확인 — 재료 부족하면 사이드 skip (매수/서빙 불일치 방지).
                var side = ctx.FoodById.Values
                    .FirstOrDefault(f => f?.type == FoodType.SIDE && ctx.UnlockedFood.IsUnlocked(f.id));
                if (side != null && HasIngredientsFor(ctx, side.id))
                    sideIds.Add(side.id);
            }
            return new ServingDecision { sideFoodIds = sideIds, accuracy = CookingAccuracy };
        }

        /// <summary>foodId 요리에 필요한 leaf ingredient 전부 재고 충분한지.</summary>
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

        // ── 헬퍼 ──

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
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

        /// <summary>최근 windowDays 요리 매출 합이 관리비 windowDays 배 이상이면 healthy.</summary>
        private bool RecentRevenueHealthy(SimContext ctx, int currentDay, int windowDays)
        {
            int fromDay = System.Math.Max(0, currentDay - windowDays);
            int totalRevenue = 0;
            foreach (var e in ctx.Log.OfType<Game.Editor.Simulation.Events.CustomerServedEvent>())
            {
                if (e.day >= fromDay && e.day < currentDay)
                    totalRevenue += e.reward;
            }
            int threshold = Game.Domain.Mall.SettlementService.ManagementFee * windowDays;
            return totalRevenue >= threshold;
        }
    }
}
