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
    // Tool ID to tool type mapping
    private static readonly Dictionary<string, string> toolIdToType = new Dictionary<string, string>
    {
        { "T001", "pan" },
        { "T002", "pot" },
        { "T003", "bowl" },
        { "T004", "cut" },
        { "T005", "plate" }
    };

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

    /// <summary>
    /// Get the appropriate sprite for this ingredient when used with a specific tool
    /// </summary>
    /// <param name="toolId">Tool ID like "T001", "T002", etc.</param>
    /// <returns>Sprite variant for the tool type</returns>
    public Sprite GetImageForTool(string toolId)
    {
        if (!toolIdToType.TryGetValue(toolId, out string toolType))
        {
            Debug.LogError($"[FoodData] Unknown tool ID: {toolId} for ingredient {ingredientName} ({id})");
            return image; // Fallback to raw if unknown tool
        }

        // Get base name without _raw suffix
        string baseName = image.name.Replace("_raw", "");
        string variantName = $"{baseName}_{toolType}";

        // Load variant image
        string variantPath = ResourcePaths.Art.FOOD + variantName;
        Sprite variant = Resources.Load<Sprite>(variantPath);

        if (variant == null)
        {
            Debug.LogError($"[FoodData] Missing variant image: {variantPath} for ingredient {ingredientName} ({id})");
            return image; // Fallback to raw if variant missing
        }

        return variant;
    }
}