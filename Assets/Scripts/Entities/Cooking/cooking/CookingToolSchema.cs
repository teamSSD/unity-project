using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class CookingToolSchema
{
    private readonly int maxIngredientSize;
    public readonly CookingToolData cookingToolData;
    public List<FoodSchema> Ingredients { get; private set; }
    private FoodSchema result;
    private bool locked = false;

    public CookingToolSchema(
        CookingToolData cookingToolData,
        int maxIngredientSize = 7)
    {
        this.cookingToolData = cookingToolData;
        this.maxIngredientSize = maxIngredientSize;
        Ingredients = new List<FoodSchema>();
    }

    public bool IsCookable()
    {
        return !locked && Ingredients.Count != 0;
    }

    public bool IsAddable(FoodSchema food)
    {
        if (locked || food == null) return false;
        if (result != null || Ingredients.Count == maxIngredientSize || Ingredients.Any(ingredient => ingredient.IsSameFood(food))) return false;
        //if (!cookingToolData.availableIngredientIds.Contains(food.foodData.id)) return false;
        //if (result != null && !cookingToolData.availableIngredientIds.Contains(result.foodData.id)) return false;

        return true;
    }

    public bool AddIngredient(FoodSchema food)
    {
        if (!IsAddable(food)) return false;

        if (result != null) Ingredients.Add(result);

        Ingredients.Add(food);
        return true;
    }

    public void ClearIngredient()
    {
        if (locked) return;
        Ingredients.Clear();
        result = null;
    }

    public void MinigameStart()
    {
        this.locked = true;
    }

    public void Cook(FoodData foodData, RecipeData recipeData, float score)
    {
        int newPrice = (int) Ingredients.Join(
            recipeData.inputInfoSet,
            ingredient => ingredient.foodData.id,
            inputInfo => inputInfo.foodId,
            (ingredient, inputInfo) => ingredient.Price * (1 + inputInfo.foodWeight * score)
        ).Sum();

        result = new FoodSchema(foodData, newPrice);
        Ingredients.Clear();
        locked = false;
    }

    public FoodSchema GetResult()
    {
        return result;
    }
}