using UnityEngine;
using System.Linq;

public class SearchDataUtil
{
    private static FoodData[] foodList = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
    private static IngredientData[] ingredientList = Resources.LoadAll<IngredientData>("ScriptableObjects/IngredientData");
    private static RecipeData[] recipeList = Resources.LoadAll<RecipeData>("ScriptableObjects/RecipeData");
    public static FoodData GetFoodDataById(string id)
    {
        return foodList.FirstOrDefault(f => f.id == id);
    }
    public static IngredientData GetIngredientDataById(string id)
    {
        return ingredientList.FirstOrDefault(f => f.id == id);
    }
    public static RecipeData GetRecipeDataByFoodId(string id)
    {
        return recipeList.FirstOrDefault(f => f.outputFood.id == id);
    }
}
