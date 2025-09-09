using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientType
{
    Sause, None, ColdStorage
}

[CreateAssetMenu(menuName = "SO/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public IngredientType type;
}
