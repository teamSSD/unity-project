using System;
using UnityEngine;

public enum IngredientDisplayCategory
{
    NONE, Refrigerator, UpperShelf, LowerShelf
}

public enum IngredientTag
{
    Grains, Vegetables, Sauces, Meat, Liquid, Seafood, Dairy, Other
}

[CreateAssetMenu(fileName = "NewIngredient", menuName = "Data/Ingredient")]
public class IngredientData : ScriptableObject, CsvParsable
{
    [Header("Basic Info")]
    public string id;
    public string description;
    [Header("Category & Pricing")]
    public IngredientDisplayCategory display;
    public int defaultPrice;
    public IngredientTag tag;
    [Header("Gameplay Logic")]
    public int expirationDay;

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
    
    public void Init(string[] args)
    {
        if (args.Length < 6) return;

        this.id = args[0].Trim();
        this.description = args[1].Trim();
        this.display = (IngredientDisplayCategory) Enum.Parse(typeof(IngredientDisplayCategory), args[2].Trim());
        int.TryParse(args[3].Trim(), out this.defaultPrice);
        this.tag = (IngredientTag) Enum.Parse(typeof(IngredientTag), args[4].Trim());
        int.TryParse(args[5].Trim(), out this.expirationDay);
    }
}
