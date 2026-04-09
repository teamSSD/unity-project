using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    public FoodData food;
    public float foodWeight;

    public RecipeIngredient(FoodData food, float foodWeight)
    {
        this.food = food;
        this.foodWeight = foodWeight;
    }
}

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Data/Recipe")]
public class RecipeData : ScriptableObject, CsvParsable
{
    [Header("Recipe Identity")]
    public string id;
    [Header("Output")]
    public FoodData outputFood;
    [Header("Settings")]
    public string minigameId;
    [Header("Ingredients")]
    public List<RecipeIngredient> inputs = new List<RecipeIngredient>();
    public HashSet<RecipeIngredient> GetInputInfoSet() => new HashSet<RecipeIngredient>(inputs);
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
            id = args[0].Trim();
            string outputId = args[1].Trim();
            outputFood = Resources.Load<FoodData>("ScriptableObjects/FoodData/" + outputId);
            minigameId = args[2].Trim();
            inputs = args[3].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(item =>
            {
                string[] parts = item.Split('-');
                string ingredientId = parts[0].Trim();
                float weight = float.Parse(parts[1]);
                FoodData ingredientSO = Resources.Load<FoodData>("ScriptableObjects/FoodData/" + ingredientId);
                return new RecipeIngredient(ingredientSO, weight);
            }).ToList();
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing\nmessage : {e.Message}");
        }
    }

    public RecipeData() {}

    public RecipeData(string id, FoodData outputFood, string minigameId, List<RecipeIngredient> inputs) {
        this.id = id;
        this.outputFood = outputFood;
        this.minigameId = minigameId;
        this.inputs = inputs;
    }
}
