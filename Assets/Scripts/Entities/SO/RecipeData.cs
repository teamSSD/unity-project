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

    public override bool Equals(object obj)
    {
        if (obj is IngredientData other)
            return this.id == other.id;
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
