using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/Recipe")]
public class RecipeData : ScriptableObject
{
    public string id;
    public FoodData outputFood;
    public (FoodData, float) InputInfoList;
    public List<int> inputFoodIds;
    public int cookingToolId;
    public string minigame;

    public override bool Equals(object obj)
    {
        if (obj is RecipeData other)
            return this.id == other.id;
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
