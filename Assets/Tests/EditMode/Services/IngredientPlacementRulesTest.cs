using System.Collections.Generic;
using Game.Domain.Cooking;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// FoodModel.AddToBento 극성 반전 버그(2026-07 fix c0044b1) 회귀 방어 + 드롭 규칙 결정 매트릭스
/// 스냅샷. IngredientPlacementRules를 순수 POCO로 유지하는 한 이 테스트가 회귀를 잡음.
/// </summary>
public class IngredientPlacementRulesTest
{
    private FoodData rawIngredient;
    private FoodData mainDish;
    private FoodData sideDish;
    private FoodData toolReadyFood;

    [SetUp]
    public void Setup()
    {
        rawIngredient = ScriptableObject.CreateInstance<FoodData>();
        rawIngredient.id = "carrot_raw";
        rawIngredient.type = FoodType.INGREDIENT;
        rawIngredient.availableTools = new List<string> { "T003" }; // cutting board

        mainDish = ScriptableObject.CreateInstance<FoodData>();
        mainDish.id = "rice_bowl";
        mainDish.type = FoodType.MAIN;
        mainDish.availableTools = new List<string>();

        sideDish = ScriptableObject.CreateInstance<FoodData>();
        sideDish.id = "pickled_side";
        sideDish.type = FoodType.SIDE;
        sideDish.availableTools = new List<string>();

        toolReadyFood = ScriptableObject.CreateInstance<FoodData>();
        toolReadyFood.id = "potato_raw";
        toolReadyFood.type = FoodType.INGREDIENT;
        toolReadyFood.availableTools = new List<string> { "T001", "T005" }; // pan/plate
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(rawIngredient);
        Object.DestroyImmediate(mainDish);
        Object.DestroyImmediate(sideDish);
        Object.DestroyImmediate(toolReadyFood);
    }

    // ─── Tool 진입 ─────────────────────────────────────────────

    [Test]
    public void CanFoodEnterTool_AvailableTool_Accepts()
    {
        Assert.IsTrue(IngredientPlacementRules.CanFoodEnterTool(toolReadyFood, "T001"));
        Assert.IsTrue(IngredientPlacementRules.CanFoodEnterTool(toolReadyFood, "T005"));
    }

    [Test]
    public void CanFoodEnterTool_UnavailableTool_Rejects()
    {
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterTool(toolReadyFood, "T003"));
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterTool(rawIngredient, "T001"));
    }

    [Test]
    public void CanFoodEnterTool_NullOrEmptyToolId_Rejects()
    {
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterTool(toolReadyFood, null));
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterTool(toolReadyFood, ""));
    }

    [Test]
    public void CanFoodEnterTool_NullFood_Rejects()
    {
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterTool(null, "T001"));
    }

    // ─── Bento 진입 (Wave 0 극성 반전 회귀 방어의 핵심) ─────────────────────

    [Test]
    public void CanFoodEnterBento_RawIngredient_Rejects()
    {
        // 이 케이스가 참(true)으로 뒤집히면 극성 반전 버그가 부활한 것.
        // Wave 0 커밋 c0044b1의 정확성 앵커.
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterBento(rawIngredient));
    }

    [Test]
    public void CanFoodEnterBento_MainDish_Accepts()
    {
        Assert.IsTrue(IngredientPlacementRules.CanFoodEnterBento(mainDish));
    }

    [Test]
    public void CanFoodEnterBento_SideDish_Accepts()
    {
        Assert.IsTrue(IngredientPlacementRules.CanFoodEnterBento(sideDish));
    }

    [Test]
    public void CanFoodEnterBento_NullFood_Rejects()
    {
        Assert.IsFalse(IngredientPlacementRules.CanFoodEnterBento(null));
    }
}
