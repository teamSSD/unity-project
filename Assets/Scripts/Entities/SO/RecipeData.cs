using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Food {
    Ingredient, Dish
}

public class FoodId {
    public Food food;
    public int id;
}

[CreateAssetMenu(menuName = "SO/Recipe")]
public class RecipeData : ScriptableObject
{
    public int id;
    public FoodId outputFoodId;
    public List<FoodId> inputFoodIds;
    public string CookingTool;
    public string Minigame;
}
