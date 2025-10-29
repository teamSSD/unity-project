using System;

public enum IngredientDisplayCategory
{
    NONE, Refrigerator, UpperShelf, LowerShelf
}

public enum IngredientTag
{
    Grain, Powder, Liquid, Sause, Vegetable, Egg, Fish, Seafood, Root, Meat
}

public class IngredientData : CsvParsable
{
    public string id;
    public string description;
    public IngredientDisplayCategory display;
    public int defaultPrice;
    public IngredientTag tag;
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
        this.id = args[0].Trim();
        this.description = args[1].Trim();
        this.display = (IngredientDisplayCategory) Enum.Parse(typeof(IngredientDisplayCategory), args[2].Trim());
        this.defaultPrice = int.Parse(args[3].Trim());
        this.tag = (IngredientTag) Enum.Parse(typeof(IngredientTag), args[4].Trim());
        this.expirationDay = int.Parse(args[5].Trim());
    }
}
