using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>
    /// L3 baseline 정책. Goal-directed heuristic + 안전 버퍼.
    ///
    /// 결정 요약:
    /// - Menu: unlocked MAIN 중 재고로 만들 수 있는 batchCount × 원가 대비 마진 top 3
    /// - Afternoon/Evening: Shopping. Night: Rest 도, 재고 부족 아니면 Shopping.
    /// - Upgrade: 자산 &gt; 비용 + 안전버퍼일 때 진행. 우선순위 Tool → Storage → Farm.
    /// - Purchase: 예상 daily 소비량의 2x 목표. general 우선 (스페셜은 30% 이상 마진 시).
    /// - Serving: 항상 사이드 3개 정확도 policy 지정 값.
    /// </summary>
    public class BaselinePolicy : IPlayerPolicy
    {
        public string Name => "Baseline";

        // 파라미터 (튜닝 knob)
        public int ReserveBufferDays = 3;
        public float CookingAccuracy = 0.85f;
        public float SpecialMarginThreshold = 0.30f; // special 매수 최소 마진율

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            // Unlocked MAIN 중 마진 top 3. feasibility 무시 — 오늘 못 만들어도
            // 이 메뉴들 위해 재료 살 것 (내일 이후 요리 파이프라인).
            // 계획 메뉴 = target 메뉴로 정책 일관성 유지.
            return ctx.FoodById.Values
                .Where(f => f != null && f.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .OrderByDescending(m => EstimateMarginPerCraft(ctx, m.id))
                .Take(3)
                .Select(f => f.id)
                .ToArray();
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase)
        {
            var ctx = (SimContext)simContext;
            // 재고 저조 시 무조건 Shopping
            if (IsInventoryLow(ctx)) return PhaseAction.Shopping;
            // 저녁/밤에도 기본 Shopping. Rest 는 stamina >= 100이라 사실상 무용 (PassDay가 리셋).
            return PhaseAction.Shopping;
        }

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            int currentDay = ctx.State.phase.Day;

            // 초반 3일 upgrade 전면 유예 — 자산 소진 방지 (실 유저의 "일단 재료부터" 판단 모사).
            // 이후에도 최근 3일 매출이 관리비 × 3 미만이면 healthy 아님 → upgrade skip.
            if (currentDay < 3) return new UpgradeDecision { doUpgrade = false };
            if (!RecentRevenueHealthy(ctx, currentDay, windowDays: 3))
                return new UpgradeDecision { doUpgrade = false };

            // 업그레이드는 자산의 20% 이내만 소진 (매우 보수적).
            // 이 정책은 endgame 도달보다 안정 루프 우선. Aggressive variant는 별도 정책.
            int maxUpgradeCost = money / 5;

            bool CanAfford(int cost) => cost <= maxUpgradeCost && (money - cost) >= 5000; // 최소 5k 잔액

            // Tool 우선 (미니게임 stamina 감소 → 요리 반복 증가로 ROI 높음)
            foreach (var toolId in new[] { "T001", "T002", "T003", "T004", "T005" })
            {
                var next = ctx.ToolUpgrade.GetNextData(toolId);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = toolId };
            }
            // Storage
            foreach (var stg in new[] { "refrigerator", "upperShelf", "lowerShelf" })
            {
                var next = ctx.StorageUpgrade.GetNextData(stg);
                if (next != null && CanAfford(next.cost))
                    return new UpgradeDecision { doUpgrade = true, category = "storage", trackId = stg };
            }
            // Farm
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

            // 재료 구매엔 reserve buffer 적용 X (재고 없으면 돈 벌 수 없음).
            // 관리비 1000만 남기고 나머지 다 씀. (업그레이드는 별도 DecideUpgrade에서 reserve 처리)
            int budget = System.Math.Max(0, money - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            // 선택된 메뉴들에서 필요한 leaf ingredient 수요 계산.
            // Baseline은 Afternoon/Evening/Night 다 Shopping이라 실제 요리는 Morning 4명만.
            // (Morning 스폰 35s로 튜닝된 후 조정)
            var need = ComputeIngredientNeeds(ctx, expectedCustomersPerDay: 4);

            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            // 필요량 높은 재료 먼저, 저렴 순으로 매수.
            var candidates = slots
                .Where(s => s.item != null && s.item.type == FoodType.INGREDIENT)
                .Select(s => (info: s, price: PriceOf(s), needed: NeedOf(need, s.item.id, ctx)))
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

        /// <summary>선택된 메뉴들의 하루치 leaf ingredient 필요량 총합. inventory 현재고 차감.</summary>
        private Dictionary<string, int> ComputeIngredientNeeds(SimContext ctx, int expectedCustomersPerDay)
        {
            var totals = new Dictionary<string, int>();

            // 메인: 선택된 3개 (없으면 unlocked 첫 3개)
            string[] mainIds = ctx.SelectedMenuIds != null && ctx.SelectedMenuIds.Length > 0
                ? ctx.SelectedMenuIds
                : ctx.FoodById.Values
                    .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                    .Take(3).Select(f => f.id).ToArray();

            // 각 메뉴에 모든 손님이 몰려도 커버 (worst-case 완전 대응).
            // 손님 8명이 특정 메뉴로 몰려도 그 메뉴 재료가 충분해야 timeout 방지.
            int perMain = expectedCustomersPerDay;
            foreach (var mid in mainIds) AddLeavesFor(ctx, mid, perMain, totals);

            // 사이드 재료는 미구매 (DecideServing이 사이드 안 씀 → 재고 낭비 방지).

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
            int have = ctx.Inventory.CheckStockAmount(food);
            return System.Math.Max(0, target - have);
        }

        public ServingDecision DecideServing(object simContext, string mainFoodId)
        {
            // 메인만 서빙 — MenuValidator 공식상 사이드는 +5%/개인데 재료비 훨씬 큼 → 순손실.
            // 향후 정책 variant에서 사이드 붙이는 SideStuffedPolicy 별도 실험.
            return new ServingDecision { sideFoodIds = new List<string>(), accuracy = CookingAccuracy };
        }

        // ── 헬퍼 ──

        private bool CanCraftOnce(SimContext ctx, string foodId)
        {
            var leaves = ctx.Recipes.LeafIngredients(foodId);
            foreach (var kv in leaves)
            {
                if (!ctx.FoodById.TryGetValue(kv.Key, out var food) || food == null) return false;
                if (ctx.Inventory.CheckStockAmount(food) < kv.Value) return false;
            }
            return true;
        }

        private int EstimateMarginPerCraft(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            // 판매가는 요리 후 pricing 공식이라 정확히 모름 — cost × 1.5 assume (실측 실효 마진 근사)
            int estimatedPrice = (int)(cost * 1.5);
            return estimatedPrice - cost;
        }

        private bool IsInventoryLow(SimContext ctx)
        {
            // 임시 heuristic: unlocked main 중 하나라도 못 만들면 low
            var mains = ctx.FoodById.Values.Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id));
            int feasible = 0;
            foreach (var m in mains) if (CanCraftOnce(ctx, m.id)) feasible++;
            return feasible < 3;
        }

        private int ReserveNeeded(SimContext ctx)
        {
            // 관리비 + 최근 하루 평균 지출 × ReserveBufferDays. 초기엔 관리비만.
            int mgmt = Game.Domain.Mall.SettlementService.ManagementFee;
            int dailyExpense = 3000; // 초기 근사; TODO Phase 2에서 event log 기반 실측치로 교체
            return mgmt + dailyExpense * ReserveBufferDays;
        }

        private int PriceOf(ItemShopSlotInfo info) =>
            info?.item?.ingredient != null ? info.item.ingredient.defaultPrice : 0;

        /// <summary>최근 windowDays 일 요리 매출 합이 관리비 windowDays 배 이상이면 healthy.
        /// 즉 하루 평균 매출이 관리비를 커버해야 upgrade 진행.</summary>
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
