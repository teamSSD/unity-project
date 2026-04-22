using UnityEngine;
using System.Linq;

public class SearchDataUtil
{
    private static FoodData[] _foodList;
    private static IngredientData[] _ingredientList;
    private static RecipeData[] _recipeList;
    private static CookingToolData[] _cookingToolList;

    private static FoodData[] foodList
    {
        get
        {
            if (_foodList == null)
                _foodList = Resources.LoadAll<FoodData>(ResourcePaths.Data.FoodData);
            return _foodList;
        }
    }

    private static IngredientData[] ingredientList
    {
        get
        {
            if (_ingredientList == null)
                _ingredientList = Resources.LoadAll<IngredientData>(ResourcePaths.Data.IngredientData);
            return _ingredientList;
        }
    }

    private static RecipeData[] recipeList
    {
        get
        {
            if (_recipeList == null)
                _recipeList = Resources.LoadAll<RecipeData>(ResourcePaths.Data.RecipeData);
            return _recipeList;
        }
    }

    private static CookingToolData[] cookingToolList
    {
        get
        {
            if (_cookingToolList == null)
                _cookingToolList = Resources.LoadAll<CookingToolData>(ResourcePaths.Data.CookingTools);
            return _cookingToolList;
        }
    }

    public static FoodData GetFoodDataById(string id)
    {
        var result = foodList.FirstOrDefault(f => f.id == id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] FoodData not found: {id}");
        return result;
    }

    public static IngredientData GetIngredientDataById(string id)
    {
        var result = ingredientList.FirstOrDefault(f => f.id == id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] IngredientData not found: {id}");
        return result;
    }

    public static RecipeData GetRecipeDataByFoodId(string id)
    {
        var result = recipeList.FirstOrDefault(f => f.outputFood.id == id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] RecipeData not found for food: {id}");
        return result;
    }

    public static CookingToolData GetCookingToolDataById(string id)
    {
        var result = cookingToolList.FirstOrDefault(f => f.id == id);
        if (result == null)
            Debug.LogWarning($"[SearchDataUtil] CookingToolData not found: {id}");
        return result;
    }

    private static readonly System.Collections.Generic.Dictionary<string, int> _chainDepthCache = new();

    /// <summary>
    /// 레시피 체인 깊이를 계산합니다. (예: 원재료=0, 1단계 조리=1, 2단계=2, ...)
    /// MAIN/SIDE 완성 보너스 배율 산출에 사용됩니다.
    /// </summary>
    public static int GetChainDepth(string foodId)
    {
        if (_chainDepthCache.TryGetValue(foodId, out int cached))
            return cached;

        var recipe = recipeList.FirstOrDefault(r => r.outputFood != null && r.outputFood.id == foodId);
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
    /// ScriptableObject가 런타임에 변경되었을 때 호출하세요.
    /// </summary>
    public static void Invalidate()
    {
        _foodList = null;
        _ingredientList = null;
        _recipeList = null;
        _cookingToolList = null;
        _chainDepthCache.Clear();
        Debug.Log("[SearchDataUtil] Cache invalidated");
    }
}
