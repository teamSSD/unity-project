using System.Collections.Generic;
using UnityEngine;

public enum FoodCategory {
    General, Special, Assistance, Processed, Failure
}

public enum SourceType {
    Purchase, Cook, Failure
}

public enum FoodTag {
    Grain, Powder, Liquid, Sause, Vegetable, Egg, Fish, Seafood, Root, Meat
}

[CreateAssetMenu(menuName = "SO/Food")]
public class FoodData : ScriptableObject
{
    public string id;
    public string ingredientName;
    public FoodCategory category;
    public int tier;
    public string description;
    public SourceType sourceType;
    public int purchasePrice;
    public int sellPrice;
    public int pirationDays;
    public FoodTag tag;
    public Sprite defaultImage;
    public List<string> availableTool;

    public override bool Equals(object obj)
    {
        if (obj is FoodData other)
            return this.id == other.id;
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
