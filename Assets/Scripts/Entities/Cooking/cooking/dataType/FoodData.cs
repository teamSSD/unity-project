using System;
using System.Collections.Generic;
using System.Linq;

public enum FoodType
{
    GARBAGE, INGREDIENT, PROCESSING, MAIN, SIDE
}

public class FoodData : CsvParsable
{
    public string id;
    public string ingredientName;
    public string description;
    public string imageName;
    public ISet<string> availableTool;
    public FoodType type;

    public void Init(string[] args)
    {
        if (args.Length != 6) throw new CsvParsingException("length of args isn't match.");
        try
        {
            id = args[0].Trim();
            ingredientName = args[1].Trim();
            description = args[2].Trim();
            imageName = args[3].Trim();
            availableTool = args[4].Split('/').ToHashSet();
            type = (FoodType)Enum.Parse(typeof(FoodType), args[5].Trim());
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing csv\nmessage : {e.Message}");
        }
    }

    public FoodData() { }

    public FoodData(string id, string ingredientName, string description, string imageName, ISet<string> availableTool)
    {
        this.id = id;
        this.ingredientName = ingredientName;
        this.description = description;
        this.imageName = imageName;
        this.availableTool = availableTool;
    }
}