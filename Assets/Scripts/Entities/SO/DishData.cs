using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DishType {
    Main, Side
}

[CreateAssetMenu(menuName = "SO/Dish")]
public class DishData : ScriptableObject
{
    public int id;
    public string dishName;
    public DishType type;
    public int basePrice;
    public int recipeId;
}
