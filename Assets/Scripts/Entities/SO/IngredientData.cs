using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientCategory {
    General, Special, Assistance, Processed, Failure
}

public enum SourceType {
    Purchase, Cook, Failure
}

public enum IngredientTag {
    Grain, Powder, Liquid, Sause, Vegetable, Egg, Fish, Seafood, Root, Meat
}

[CreateAssetMenu(menuName = "SO/Ingredient")]
public class IngredientData : ScriptableObject
{
    public int id;
    public string ingredientName;
    public IngredientCategory category;
    public int tier;
    public string description;
    public SourceType sourceType;
    public int purchasePrice;
    public int sellPrice;
    public int pirationDays;
    public IngredientTag tag;
}
