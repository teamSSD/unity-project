using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>초보 플레이어 근사. 서투른 판단 요소들:
    /// - 메뉴는 unlocked 첫 3개 (마진 안 봄)
    /// - 재료는 general만 조금씩 (target=3), special 안 삼
    /// - 업그레이드는 자산 30% 넘을 때만 (지나치게 보수적)
    /// - 미니게임 정확도 0.60 (실력 부족)
    /// - 사이드는 최선을 다해 다 붙임 (게임 몰라서)</summary>
    public class NovicePolicy : IPlayerPolicy
    {
        public string Name => "Novice";

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            return ctx.FoodById.Values
                .Where(f => f != null && f.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .Take(3)
                .Select(f => f.id)
                .ToArray();
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase) => PhaseAction.Shopping;

        public UpgradeDecision DecideUpgrade(object simContext)
        {
            var ctx = (SimContext)simContext;
            int money = ctx.Stats.GetMoney();
            // 자산의 30% 넘어야 업그레이드 — 몹시 보수적
            int threshold = money * 3 / 10;
            foreach (var toolId in new[] { "T001", "T002", "T003" })
            {
                var next = ctx.ToolUpgrade.GetNextData(toolId);
                if (next != null && next.cost <= threshold && (money - next.cost) >= 8000)
                    return new UpgradeDecision { doUpgrade = true, category = "tool", trackId = toolId };
            }
            return new UpgradeDecision { doUpgrade = false };
        }

        public List<PurchaseDecision> DecidePurchases(object simContext)
        {
            var ctx = (SimContext)simContext;
            int budget = System.Math.Max(0, ctx.Stats.GetMoney() - 5000); // 큰 안전금
            var decisions = new List<PurchaseDecision>();
            if (budget <= 0) return decisions;

            var slots = ctx.Purchase.GetItemList(ctx.State.phase.Day, (int)ctx.State.phase.Phase);
            if (slots == null) return decisions;

            // general만, 재고 3개 유지 (부족)
            foreach (var s in slots.Where(x => x.type == ProductType.General && x.item?.type == FoodType.INGREDIENT))
            {
                int price = s.item.ingredient?.defaultPrice ?? 0;
                if (price <= 0) continue;
                int current = ctx.Inventory.CheckStockAmount(s.item);
                int need = System.Math.Max(0, 3 - current);
                int affordable = budget / price;
                int qty = System.Math.Min(need, affordable);
                if (qty <= 0) continue;
                decisions.Add(new PurchaseDecision { foodId = s.item.id, qty = qty });
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
                .Take(3)
                .Select(f => f.id)
                .ToList();
            return new ServingDecision { sideFoodIds = sides, accuracy = 0.60f };
        }
    }
}
