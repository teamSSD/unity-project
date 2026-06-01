using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 카탈로그를 통한 데이터 조회 헬퍼.
/// 모든 조회는 CatalogProvider를 경유 (Resources.Load 미사용).
/// 체인 깊이 계산 같은 부가 로직만 직접 보유.
/// </summary>
public class SearchDataUtil
{
    public static FoodData GetFoodDataById(string id)
    {
        var result = CatalogProvider.Food?.GetById(id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] FoodData not found: {id}");
        return result;
    }

    public static IngredientData GetIngredientDataById(string id)
    {
        var result = CatalogProvider.Ingredient?.GetById(id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] IngredientData not found: {id}");
        return result;
    }

    public static RecipeData GetRecipeDataByFoodId(string id)
    {
        var recipes = CatalogProvider.Recipe?.All;
        if (recipes == null) return null;
        var result = recipes.FirstOrDefault(r => r.outputFood != null && r.outputFood.id == id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] RecipeData not found for food: {id}");
        return result;
    }

    public static CookingToolData GetCookingToolDataById(string id)
    {
        var result = CatalogProvider.CookingTool?.GetById(id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] CookingToolData not found: {id}");
        return result;
    }

    private static readonly Dictionary<string, int> _chainDepthCache = new();

    /// <summary>
    /// 레시피 체인 깊이를 계산합니다. (예: 원재료=0, 1단계 조리=1, 2단계=2, ...)
    /// MAIN/SIDE 완성 보너스 배율 산출에 사용됩니다.
    /// </summary>
    public static int GetChainDepth(string foodId)
    {
        if (_chainDepthCache.TryGetValue(foodId, out int cached))
            return cached;

        var recipes = CatalogProvider.Recipe?.All;
        if (recipes == null) { _chainDepthCache[foodId] = 0; return 0; }

        var recipe = recipes.FirstOrDefault(r => r.outputFood != null && r.outputFood.id == foodId);
        if (recipe == null || recipe.inputs == null || recipe.inputs.Count == 0)
        {
            _chainDepthCache[foodId] = 0;
            return 0;
        }

        int maxInputDepth = 0;
        foreach (var input in recipe.inputs)
        {
            if (input.food == null) continue;
            int d = GetChainDepth(input.food.id);
            if (d > maxInputDepth) maxInputDepth = d;
        }

        int depth = maxInputDepth + 1;
        _chainDepthCache[foodId] = depth;
        return depth;
    }

    /// <summary>
    /// 캐시된 데이터를 무효화합니다.
    /// </summary>
    public static void Invalidate()
    {
        _chainDepthCache.Clear();
    }
}
