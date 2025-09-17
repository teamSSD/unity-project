using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/CookingTool")]
public class CookingToolData : ScriptableObject
{
    public int id;
    public string cookerName;
    public List<int> minigameIds;
    public List<int> availableIngredientIds;
}