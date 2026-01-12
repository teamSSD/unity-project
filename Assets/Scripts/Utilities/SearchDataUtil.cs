using UnityEngine;
using System.Linq;

public class SearchDataUtil
{
    private static FoodData[] foodList = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
    private static IngredientData[] ingredientList = Resources.LoadAll<IngredientData>("ScriptableObjects/IngredientData");
    public static FoodData GetFoodDataById(string id)
    {
        return foodList.FirstOrDefault(f => f.id == id);
    }
    public static IngredientData GetIngredientDataById(string id)
    {
        return ingredientList.FirstOrDefault(f => f.id == id);
    }
}
