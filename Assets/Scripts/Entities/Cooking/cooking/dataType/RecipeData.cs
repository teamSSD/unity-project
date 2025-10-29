using System;
using System.Collections.Generic;
using System.Linq;

public class RecipeIngredient
{
    public string foodId;
    public float foodWeight;

    public RecipeIngredient(string foodId, float foodWeight)
    {
        this.foodId = foodId;
        this.foodWeight = foodWeight;
    }
}

public class RecipeData : CsvParsable
{
    public string id;
    public string outputId;
    public string minigameId;
    public ISet<RecipeIngredient> inputInfoSet;

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

    public void Init(string[] args)
    {
        if (args.Length != 4) throw new CsvParsingException("length of args isn't match.");

        try
        {
            this.id = args[0].Trim();
            this.outputId = args[1].Trim();
            this.minigameId = args[2].Trim();
            this.inputInfoSet = args[3].Split('/').Select(item =>
                    {
                        string[] parts = item.Split('-');
                        return new RecipeIngredient(parts[0].Trim(), float.Parse(parts[1]));
                    }).ToHashSet();
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing csv\nmessage : {e.Message}");
        }
    }

    public RecipeData() {}

    public RecipeData(string id, string outputId, string minigameId, ISet<RecipeIngredient> inputInfoSet) {
        this.id = id;
        this.outputId = outputId;
        this.minigameId = minigameId;
        this.inputInfoSet = inputInfoSet;
    }
}
