using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>팜 상황에 맞춰 메뉴 선택하는 실사용 근사 정책.
    /// - 매일 메뉴 선택: unlocked MAIN 중 farm crop overlap이 높은 top-3
    ///   (팜에 심긴/재고 있는 crop을 leaf로 쓰는 메뉴 우선)
    /// - Farm crop overlap이 tie면 마진 desc 순
    /// - 나머지 로직 (upgrade/purchase/serving) = Baseline과 동일
    /// 실 유저의 "농장 나오는 걸로 요리 만든다" 플레이 스타일 모사.</summary>
    public class FarmAwarePolicy : IPlayerPolicy
    {
        public string Name => "FarmAware";
        private readonly BaselinePolicy _base = new();

        public string[] SelectMenusForDay(object simContext)
        {
            var ctx = (SimContext)simContext;

            // 현 시점 farm crop 가용성 파악
            var farmCropIds = CollectFarmAvailableCrops(ctx);

            // 각 unlocked MAIN에 대해 (farm crop overlap 개수, 마진) 계산
            var candidates = ctx.FoodById.Values
                .Where(f => f != null && f.type == FoodType.MAIN && ctx.UnlockedFood.IsUnlocked(f.id))
                .Select(m => (
                    id: m.id,
                    farmMatch: CountFarmLeaves(ctx, m.id, farmCropIds),
                    margin: EstimateMargin(ctx, m.id)))
                .OrderByDescending(x => x.farmMatch)
                .ThenByDescending(x => x.margin)
                .Take(3)
                .Select(x => x.id)
                .ToArray();

            return candidates;
        }

        public PhaseAction DecidePhaseAction(object simContext, int phase) => _base.DecidePhaseAction(simContext, phase);
        public UpgradeDecision DecideUpgrade(object simContext) => _base.DecideUpgrade(simContext);
        public List<PurchaseDecision> DecidePurchases(object simContext) => _base.DecidePurchases(simContext);
        public ServingDecision DecideServing(object simContext, string mainFoodId) => _base.DecideServing(simContext, mainFoodId);

        // ── 헬퍼 ──

        /// <summary>현재 팜에서 나오는/재고에 있는 crop id 집합.
        /// 심긴 타일의 cropId + 재고 &gt; 0인 crop.</summary>
        private HashSet<string> CollectFarmAvailableCrops(SimContext ctx)
        {
            var set = new HashSet<string>();

            // 타일에 심긴 crop (수확 예정 포함)
            var tiles = ctx.State.garden.persistent?.tiles;
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile != null && !string.IsNullOrEmpty(tile.cropId))
                        set.Add(tile.cropId);
                }
            }

            // 재고에 있는 farm crop
            foreach (var cropId in SimGameConfig.FarmCropIds)
            {
                if (ctx.FoodById.TryGetValue(cropId, out var food) && food != null
                    && ctx.Inventory.CheckStockAmount(food) > 0)
                {
                    set.Add(cropId);
                }
            }
            return set;
        }

        /// <summary>foodId의 leaf ingredients 중 farm crop인 개수 (weight 무관, 종류 수).</summary>
        private int CountFarmLeaves(SimContext ctx, string foodId, HashSet<string> farmCropIds)
        {
            var leaves = ctx.Recipes.LeafIngredients(foodId);
            int match = 0;
            foreach (var kv in leaves)
                if (farmCropIds.Contains(kv.Key)) match++;
            return match;
        }

        private int EstimateMargin(SimContext ctx, string foodId)
        {
            int cost = ctx.Recipes.EstimateCostBasis(foodId, ctx.IngredientPriceById);
            return (int)(cost * 1.5) - cost;
        }
    }
}
