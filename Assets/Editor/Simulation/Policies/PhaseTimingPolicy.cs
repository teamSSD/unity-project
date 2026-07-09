using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>영업 시간대 변주 실험용. Baseline과 동일하나 어느 페이즈에 요리하는지 config.
    /// 파라미터: WorkPhases (Morning/Afternoon/Evening/Night 조합).
    /// 나머지 페이즈는 Shopping. Baseline 대비 서빙율/매출/생존율 변화 측정.</summary>
    public class PhaseTimingPolicy : IPlayerPolicy
    {
        public string Name { get; }
        private readonly HashSet<PhaseType> _workPhases;
        private readonly BaselinePolicy _base = new();
        /// <summary>true면 요리 phase 수에 비례해 재료 매수 스케일업 (실 유저 근사).</summary>
        public bool ScalePurchase = false;

        public PhaseTimingPolicy(string label, params PhaseType[] workPhases)
        {
            Name = $"T_{label}";
            _workPhases = new HashSet<PhaseType>(workPhases);
        }

        public string[] SelectMenusForDay(object simContext) => _base.SelectMenusForDay(simContext);

        public PhaseAction DecidePhaseAction(object simContext, int phase)
        {
            var p = (PhaseType)phase;
            return _workPhases.Contains(p) ? PhaseAction.Work : PhaseAction.Shopping;
        }

        public UpgradeDecision DecideUpgrade(object simContext) => _base.DecideUpgrade(simContext);
        public ServingDecision DecideServing(object simContext, string mainFoodId) => _base.DecideServing(simContext, mainFoodId);

        public List<PurchaseDecision> DecidePurchases(object simContext)
        {
            if (!ScalePurchase) return _base.DecidePurchases(simContext);

            // 요리 phase 수에 따라 needed × cookPhaseCount로 스케일업 매수.
            // (Morning은 강제 요리이므로 최소 1)
            var ctx = (SimContext)simContext;
            int cookPhases = System.Math.Max(1, _workPhases.Count + 1); // Morning 강제 + workPhases

            int money = ctx.Stats.GetMoney();
            int budget = System.Math.Max(0, money - Game.Domain.Mall.SettlementService.ManagementFee);
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            // Baseline이 8 customers/day 기준 → cookPhases 배로 매수.
            int perDayCustomers = 8 * cookPhases;
            var need = ComputeNeedsScaled(ctx, perDayCustomers);

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

        private Dictionary<string, int> ComputeNeedsScaled(SimContext ctx, int perDayCustomers)
        {
            var totals = new Dictionary<string, int>();
            string[] mainIds = ctx.SelectedMenuIds != null && ctx.SelectedMenuIds.Length > 0
                ? ctx.SelectedMenuIds
                : ctx.FoodById.Values
                    .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                    .Take(3).Select(f => f.id).ToArray();
            int n = System.Math.Max(1, mainIds.Length);
            int perMain = System.Math.Max(1, (perDayCustomers + n - 1) / n);
            foreach (var mid in mainIds)
            {
                var leaves = ctx.Recipes.LeafIngredients(mid);
                foreach (var kv in leaves)
                {
                    totals.TryGetValue(kv.Key, out int cur);
                    totals[kv.Key] = cur + kv.Value * perMain;
                }
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
