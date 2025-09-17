using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/CookingTool")]
public class CookingToolData : ScriptableObject
{
    public int id;
    public string cookerName;
    public List<TempMinigameData> minigames;
    public List<int> availableIngredientIds;

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