using System.Collections.Generic;
using Game.Domain.Cooking;
using NUnit.Framework;
using UnityEngine;

public class RecipeLookupServiceTest
{
    private FoodData ingA, ingB, outputX;
    private RecipeData recipeM001, recipeM002;

    [SetUp]
    public void Setup()
    {
        ingA = ScriptableObject.CreateInstance<FoodData>(); ingA.id = "IA";
        ingB = ScriptableObject.CreateInstance<FoodData>(); ingB.id = "IB";
        outputX = ScriptableObject.CreateInstance<FoodData>(); outputX.id = "X";

        recipeM001 = ScriptableObject.CreateInstance<RecipeData>();
        recipeM001.id = "R001";
        recipeM001.minigameId = "M001"; // → T001
        recipeM001.outputFood = outputX;
        recipeM001.inputs = new List<RecipeIngredient> {
            new RecipeIngredient(ingA, 1f)
        };

        recipeM002 = ScriptableObject.CreateInstance<RecipeData>();
        recipeM002.id = "R002";
        recipeM002.minigameId = "M002"; // → T002
        recipeM002.outputFood = outputX;
        recipeM002.inputs = new List<RecipeIngredient> {
            new RecipeIngredient(ingA, 1f),
            new RecipeIngredient(ingB, 1f)
        };
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(ingA);
        Object.DestroyImmediate(ingB);
        Object.DestroyImmediate(outputX);
        Object.DestroyImmediate(recipeM001);
        Object.DestroyImmediate(recipeM002);
    }

    [Test]
    public void GetToolIdForMinigame_KnownMapping()
    {
        var svc = new RecipeLookupService(new[] { recipeM001 });
        Assert.AreEqual("T001", svc.GetToolIdForMinigame("M001"));
        Assert.AreEqual("T002", svc.GetToolIdForMinigame("M002"));
        Assert.AreEqual("T003", svc.GetToolIdForMinigame("M004"));
        Assert.AreEqual("T003", svc.GetToolIdForMinigame("M005"));
        Assert.AreEqual("T004", svc.GetToolIdForMinigame("M006"));
        Assert.AreEqual("T005", svc.GetToolIdForMinigame("M007"));
    }

    [Test]
    public void GetToolIdForMinigame_UnknownReturnsNull()
    {
        var svc = new RecipeLookupService(new RecipeData[0]);
        Assert.IsNull(svc.GetToolIdForMinigame("M999"));
    }

    [Test]
    public void Search_MatchesRegisteredRecipe()
    {
        var svc = new RecipeLookupService(new[] { recipeM001 });
        var result = svc.Search("T001", new List<FoodData> { ingA });
        Assert.IsNotNull(result);
        Assert.AreEqual("R001", result.id);
    }

    [Test]
    public void Search_OrderIndependent()
    {
        var svc = new RecipeLookupService(new[] { recipeM002 });
        var byIA_IB = svc.Search("T002", new List<FoodData> { ingA, ingB });
        var byIB_IA = svc.Search("T002", new List<FoodData> { ingB, ingA });
        Assert.IsNotNull(byIA_IB);
        Assert.IsNotNull(byIB_IA);
        Assert.AreEqual(byIA_IB.id, byIB_IA.id);
    }

    [Test]
    public void Search_WrongTool_ReturnsNull()
    {
        var svc = new RecipeLookupService(new[] { recipeM001 });
        var result = svc.Search("T002", new List<FoodData> { ingA });
        Assert.IsNull(result);
    }

    [Test]
    public void Search_EmptyIngredients_ReturnsNull()
    {
        var svc = new RecipeLookupService(new[] { recipeM001 });
        Assert.IsNull(svc.Search("T001", new List<FoodData>()));
        Assert.IsNull(svc.Search("T001", null));
    }

    [Test]
    public void GetAllRecipes_ReturnsCopy()
    {
        var svc = new RecipeLookupService(new[] { recipeM001, recipeM002 });
        var list = svc.GetAllRecipes();
        Assert.AreEqual(2, list.Count);
        list.Clear();
        Assert.AreEqual(2, svc.GetAllRecipes().Count); // 원본 보존
    }

    [Test]
    public void GetRecipesForTool_FiltersByMapping()
    {
        var svc = new RecipeLookupService(new[] { recipeM001, recipeM002 });
        var t001 = svc.GetRecipesForTool("T001");
        var t002 = svc.GetRecipesForTool("T002");
        Assert.AreEqual(1, t001.Count); Assert.AreEqual("R001", t001[0].id);
        Assert.AreEqual(1, t002.Count); Assert.AreEqual("R002", t002[0].id);
    }
}
