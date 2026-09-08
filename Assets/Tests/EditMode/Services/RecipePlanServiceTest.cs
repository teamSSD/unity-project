using System.Collections.Generic;
using Game.Domain.Cooking;
using NUnit.Framework;
using UnityEngine;

public class RecipePlanServiceTest
{
    private readonly List<Object> _objects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var item in _objects) Object.DestroyImmediate(item);
        _objects.Clear();
    }

    [Test]
    public void Build_DerivesLeafNeedsAndPostOrderFromCatalog()
    {
        var rawA = Food("raw-a", FoodType.INGREDIENT);
        var rawB = Food("raw-b", FoodType.INGREDIENT);
        var processed = Food("processed", FoodType.PROCESSING);
        var main = Food("main", FoodType.MAIN);
        var child = Recipe("child", processed, "M006", rawA);
        var parent = Recipe("parent", main, "M001", processed, rawB);

        var plan = new RecipePlanService(
            new[] { rawA, rawB, processed, main },
            new[] { parent, child }).Build(main.id);

        Assert.That(plan.valid, Is.True, plan.error);
        Assert.That(plan.steps.ConvertAll(step => step.outputFoodId), Is.EqualTo(new[] { "processed", "main" }));
        Assert.That(plan.steps[0].toolId, Is.EqualTo("T004"));
        Assert.That(plan.steps[1].toolId, Is.EqualTo("T001"));
        Assert.That(plan.ingredients.ConvertAll(item => $"{item.foodId}:{item.quantity}"),
            Is.EqualTo(new[] { "raw-a:1", "raw-b:1" }));
    }

    [Test]
    public void Build_ReportsMissingRecipeInsteadOfInventingAnAction()
    {
        var main = Food("main", FoodType.MAIN);

        var plan = new RecipePlanService(new[] { main }, new RecipeData[0]).Build(main.id);

        Assert.That(plan.valid, Is.False);
        Assert.That(plan.error, Does.Contain("No recipe"));
        Assert.That(plan.steps, Is.Empty);
    }

    private FoodData Food(string id, FoodType type)
    {
        var food = ScriptableObject.CreateInstance<FoodData>();
        food.id = id;
        food.type = type;
        _objects.Add(food);
        return food;
    }

    private RecipeData Recipe(string id, FoodData output, string minigameId, params FoodData[] inputs)
    {
        var recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.id = id;
        recipe.outputFood = output;
        recipe.minigameId = minigameId;
        recipe.inputs = new List<RecipeIngredient>();
        foreach (var input in inputs) recipe.inputs.Add(new RecipeIngredient(input, 1f));
        _objects.Add(recipe);
        return recipe;
    }
}
