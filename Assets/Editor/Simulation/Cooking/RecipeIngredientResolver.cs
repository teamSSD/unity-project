using System.Collections.Generic;
using System.Linq;
using Game.Domain.Cooking;

namespace Game.Editor.Simulation.Cooking
{
    /// <summary>
    /// 메뉴(main/side food) → 필요한 leaf ingredient 수량 매핑.
    /// MenuCardRecipeBuilder는 SearchDataUtil 싱글턴 사용 — sim에선 명시적 카탈로그 dict로 대체.
    ///
    /// 주의 (scout 지적): RecipeData.inputs의 각 항목은 "1 단위" 소비로 취급.
    /// foodWeight는 pricing weight이지 quantity가 아님. 실제 프로덕션 로직과 일치.
    /// </summary>
    public class RecipeIngredientResolver
    {
        private readonly Dictionary<string, FoodData> _foodById;
        private readonly RecipePlanService _plans;

        // 캐시: foodId → leaf ingredient counts. 반복 조회 최적화.
        private readonly Dictionary<string, Dictionary<string, int>> _cache = new();

        public RecipeIngredientResolver(IEnumerable<FoodData> foods, IEnumerable<RecipeData> recipes)
        {
            _foodById = new Dictionary<string, FoodData>();
            foreach (var f in foods) if (f != null && !string.IsNullOrEmpty(f.id)) _foodById[f.id] = f;

            _plans = new RecipePlanService(_foodById.Values, recipes);
        }

        /// <summary>foodId 완성에 필요한 leaf ingredient(id → 수량).
        /// foodId 자체가 INGREDIENT면 {id: 1}. 순환 방지용 visited.</summary>
        public IReadOnlyDictionary<string, int> LeafIngredients(string foodId)
        {
            if (_cache.TryGetValue(foodId, out var cached)) return cached;
            var plan = _plans.Build(foodId);
            var acc = plan.valid
                ? plan.ingredients.ToDictionary(item => item.foodId, item => item.quantity)
                : new Dictionary<string, int>();
            _cache[foodId] = acc;
            return acc;
        }

        /// <summary>foodId 하나 요리 시 원가 (leaf ingredient defaultPrice 합).</summary>
        public int EstimateCostBasis(string foodId, IReadOnlyDictionary<string, int> ingredientPriceById)
        {
            int total = 0;
            var leaves = LeafIngredients(foodId);
            foreach (var kv in leaves)
            {
                if (ingredientPriceById.TryGetValue(kv.Key, out int price))
                    total += price * kv.Value;
            }
            return total;
        }

        public bool TryGetFood(string id, out FoodData food) => _foodById.TryGetValue(id, out food);
    }
}
