using System.Collections.Generic;

namespace Game.Domain.Cooking
{
    /// <summary>
    /// 드래그·드롭 시 재료가 특정 target(도구/도시락)에 수용될 수 있는지 판정하는 순수 규칙 함수.
    /// Unity 타입 의존 없이 <see cref="FoodData"/> + tool id/type만 참조 → EditMode 테스트 가능.
    ///
    /// 도입 배경(2026-07): FoodModel.AddToBento 극성 반전 버그 회귀 방어. 규칙을 뷰에서 분리해
    /// 단일 원천으로. 자세한 논의는 tmp/refactor-2026-05/reports/review_2026-07_deepdig.md D 참조.
    /// </summary>
    public static class IngredientPlacementRules
    {
        /// <summary>재료가 지정된 도구에 진입 가능한가.</summary>
        public static bool CanFoodEnterTool(FoodData food, string toolId)
        {
            if (food == null || string.IsNullOrEmpty(toolId)) return false;
            var tools = food.availableTools;
            return tools != null && tools.Contains(toolId);
        }

        /// <summary>재료가 도시락에 진입 가능한가. MAIN/SIDE만 허용 — raw INGREDIENT는 거부.</summary>
        public static bool CanFoodEnterBento(FoodData food)
        {
            if (food == null) return false;
            return food.type == FoodType.MAIN || food.type == FoodType.SIDE;
        }
    }
}
