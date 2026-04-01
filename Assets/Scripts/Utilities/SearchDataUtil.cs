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
                _foodList = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
            return _foodList;
        }
    }

    private static IngredientData[] ingredientList
    {
        get
        {
            if (_ingredientList == null)
                _ingredientList = Resources.LoadAll<IngredientData>("ScriptableObjects/IngredientData");
            return _ingredientList;
        }
    }

    private static RecipeData[] recipeList
    {
        get
        {
            if (_recipeList == null)
                _recipeList = Resources.LoadAll<RecipeData>("ScriptableObjects/RecipeData");
            return _recipeList;
        }
    }

    private static CookingToolData[] cookingToolList
    {
        get
        {
            if (_cookingToolList == null)
                _cookingToolList = Resources.LoadAll<CookingToolData>("ScriptableObjects/CookingTools");
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
        Debug.Log("[SearchDataUtil] Cache invalidated");
    }
}
