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
    public IngredientData outputFood;
    public List<int> inputFoodIds;
    public TempMinigameData Minigame;
}
