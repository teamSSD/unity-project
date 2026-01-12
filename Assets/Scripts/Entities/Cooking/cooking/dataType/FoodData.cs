using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum FoodType
{
    GARBAGE, INGREDIENT, PROCESSING, MAIN, SIDE
}

[CreateAssetMenu(fileName = "NewFoodData", menuName = "Data/Food Data")]
public class FoodData : ScriptableObject, CsvParsable
{
    public string id;
    public string ingredientName;
    public string description;
    public Sprite image;
    public IngredientData ingredient;
    public List<string> availableTools = new List<string>();
    public FoodType type;

    public void Init(string[] args)
    {
        if (args.Length != 6) throw new CsvParsingException("length of args isn't match.");
        try
        {
            id = args[0].Trim();
            ingredientName = args[1].Trim();
            description = args[2].Trim();
            string spritePath = ResourcePaths.Art.FOOD + args[3].Trim();
            image = Resources.Load<Sprite>(spritePath);
            string ingredientPath = "ScriptableObjects/IngredientData/" + id;
            ingredient = Resources.Load<IngredientData>(ingredientPath);
            availableTools = new List<string>(args[4].Split('/'));
            type = (FoodType)Enum.Parse(typeof(FoodType), args[5].Trim());
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing \nmessage : {e.Message}");
        }
    }
}