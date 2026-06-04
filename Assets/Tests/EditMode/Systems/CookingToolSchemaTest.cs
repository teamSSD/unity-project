using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CookingToolSchemaTest
{
    private CookingToolData tool;
    private FoodData ingA, ingB, output;
    private RecipeData recipe;

    [SetUp]
    public void Setup()
    {
        tool = ScriptableObject.CreateInstance<CookingToolData>();
        tool.id = "T001"; tool.cookerName = "팬";

        ingA = ScriptableObject.CreateInstance<FoodData>();
        ingA.id = "IA"; ingA.type = FoodType.INGREDIENT;
        ingA.availableTools = new List<string> { "T001" };

        ingB = ScriptableObject.CreateInstance<FoodData>();
        ingB.id = "IB"; ingB.type = FoodType.INGREDIENT;
        ingB.availableTools = new List<string> { "T001" };

        output = ScriptableObject.CreateInstance<FoodData>();
        output.id = "OUT"; output.type = FoodType.PROCESSING;
        output.availableTools = new List<string> { "T001" };

        recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.id = "R"; recipe.outputFood = output;
        recipe.inputs = new List<RecipeIngredient> {
            new RecipeIngredient(ingA, 1f),
            new RecipeIngredient(ingB, 1f)
        };
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(tool);
        Object.DestroyImmediate(ingA);
        Object.DestroyImmediate(ingB);
        Object.DestroyImmediate(output);
        Object.DestroyImmediate(recipe);
    }

    private FoodSchema Schema(FoodData fd, int price = 100) => new FoodSchema(fd, price);

    [Test]
    public void Initial_NotCookable()
    {
        var t = new CookingToolSchema(tool);
        Assert.IsFalse(t.IsCookable());
    }

    [Test]
    public void AddIngredient_MakesCookable()
    {
        var t = new CookingToolSchema(tool);
        Assert.IsTrue(t.AddIngredient(Schema(ingA)));
        Assert.IsTrue(t.IsCookable());
        Assert.AreEqual(1, t.Ingredients.Count);
    }

    [Test]
    public void AddIngredient_WrongTool_Refused()
    {
        var wrongFood = ScriptableObject.CreateInstance<FoodData>();
        wrongFood.id = "W"; wrongFood.type = FoodType.INGREDIENT;
        wrongFood.availableTools = new List<string> { "T999" };

        var t = new CookingToolSchema(tool);
        Assert.IsFalse(t.AddIngredient(Schema(wrongFood)));

        Object.DestroyImmediate(wrongFood);
    }

    [Test]
    public void AddIngredient_Duplicate_Refused()
    {
        var t = new CookingToolSchema(tool);
        t.AddIngredient(Schema(ingA));
        Assert.IsFalse(t.AddIngredient(Schema(ingA))); // 같은 재료 중복 금지
    }

    [Test]
    public void AddIngredient_MaxSize_Refused()
    {
        var t = new CookingToolSchema(tool, maxIngredientSize: 2);
        t.AddIngredient(Schema(ingA));
        t.AddIngredient(Schema(ingB));

        var ingC = ScriptableObject.CreateInstance<FoodData>();
        ingC.id = "IC"; ingC.type = FoodType.INGREDIENT;
        ingC.availableTools = new List<string> { "T001" };
        Assert.IsFalse(t.AddIngredient(Schema(ingC)));

        Object.DestroyImmediate(ingC);
    }

    [Test]
    public void ClearIngredient_ResetsState()
    {
        var t = new CookingToolSchema(tool);
        t.AddIngredient(Schema(ingA));
        t.ClearIngredient();
        Assert.AreEqual(0, t.Ingredients.Count);
        Assert.IsFalse(t.IsCookable());
    }

    [Test]
    public void MinigameStart_Locks()
    {
        var t = new CookingToolSchema(tool);
        t.AddIngredient(Schema(ingA));
        t.MinigameStart();
        Assert.IsFalse(t.IsCookable()); // locked
        Assert.IsFalse(t.AddIngredient(Schema(ingB))); // locked
    }

    [Test]
    public void Cook_ProducesResult()
    {
        var t = new CookingToolSchema(tool);
        t.AddIngredient(Schema(ingA, 100));
        t.AddIngredient(Schema(ingB, 200));
        t.Cook(output, recipe, 1.0f, 1);

        var result = t.GetResult();
        Assert.IsNotNull(result);
        Assert.AreEqual("OUT", result.foodData.id);
        Assert.AreEqual(0, t.Ingredients.Count); // 재료 소모
    }

    [Test]
    public void Cook_ChainDepthBonus_AppliedToMainSide()
    {
        var mainOut = ScriptableObject.CreateInstance<FoodData>();
        mainOut.id = "MAIN"; mainOut.type = FoodType.MAIN;

        var t1 = new CookingToolSchema(tool);
        t1.AddIngredient(Schema(ingA, 100));
        t1.AddIngredient(Schema(ingB, 100));
        t1.Cook(mainOut, recipe, 1.0f, 1);
        int priceChain1 = t1.GetResult().Price;

        var t3 = new CookingToolSchema(tool);
        t3.AddIngredient(Schema(ingA, 100));
        t3.AddIngredient(Schema(ingB, 100));
        t3.Cook(mainOut, recipe, 1.0f, 3); // chainBonus = 1 + 0.15 * 2 = 1.3
        int priceChain3 = t3.GetResult().Price;

        Assert.Greater(priceChain3, priceChain1);

        Object.DestroyImmediate(mainOut);
    }
}
