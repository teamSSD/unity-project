using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>메뉴 다양화 정책. 편중 완화 검증용.
    /// - 매일 마진 순 대신 무작위 3개 픽 (다양성 강조)
    /// - 나머지 로직은 Baseline과 동일</summary>
    public class MenuDiversifyPolicy : IPlayerPolicy
    {
        public string Name => "MenuDiversify";
        private readonly BaselinePolicy _base = new();

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;
            var unlocked = ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .Select(f => f.id).ToList();
            if (unlocked.Count == 0) return new string[0];

            // Variable RNG로 3개 무작위 (매일 다른 조합)
            var picked = new List<string>();
            var pool = new List<string>(unlocked);
            while (picked.Count < 3 && pool.Count > 0)
            {
                int idx = GameRandom.Range(GameRandom.Variable, 0, pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return picked.ToArray();
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase) => _base.DecidePhaseAction(simContext, phase);
        public UpgradeDecision DecideUpgrade(object simContext) => _base.DecideUpgrade(simContext);
        public List<PurchaseDecision> DecidePurchases(object simContext) => _base.DecidePurchases(simContext);
        public ServingDecision DecideServing(object simContext, string mainFoodId) => _base.DecideServing(simContext, mainFoodId);
    }
}
