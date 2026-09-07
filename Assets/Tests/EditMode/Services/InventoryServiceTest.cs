using System.Collections.Generic;
using Game.Domain.Common;
using Game.Schema.State;
using NUnit.Framework;
using UnityEngine;

public class InventoryServiceTest
{
    private FoodData foodA;
    private FoodData foodB;
    private IngredientData ingA;
    private IngredientData ingB;

    [SetUp]
    public void Setup()
    {
        ingA = ScriptableObject.CreateInstance<IngredientData>();
        ingA.expirationDay = 3;
        ingA.display = IngredientDisplayCategory.Refrigerator;
        ingB = ScriptableObject.CreateInstance<IngredientData>();
        ingB.expirationDay = 5;
        ingB.display = IngredientDisplayCategory.UpperShelf;

        foodA = ScriptableObject.CreateInstance<FoodData>();
        foodA.id = "IA"; foodA.type = FoodType.INGREDIENT; foodA.ingredient = ingA;
        foodB = ScriptableObject.CreateInstance<FoodData>();
        foodB.id = "IB"; foodB.type = FoodType.INGREDIENT; foodB.ingredient = ingB;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(ingA);
        Object.DestroyImmediate(ingB);
        Object.DestroyImmediate(foodA);
        Object.DestroyImmediate(foodB);
    }

    private InventoryService BuildSvc(params FoodData[] catalog)
        => new InventoryService(new InventoryState(), catalog, null);

    [Test]
    public void ServicesUsingSameState_ObserveTheSameInventory()
    {
        var state = new InventoryState();
        var writer = new InventoryService(state, new[] { foodA }, null);
        var reader = new InventoryService(state, new[] { foodA }, null);

        writer.AddFood(foodA, 4);

        Assert.AreEqual(4, reader.CheckStockAmount(foodA));
    }

    [Test]
    public void AddFood_NewItem_AddsBatch()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 5);
        Assert.AreEqual(5, svc.CheckStockAmount(foodA));
    }

    [Test]
    public void AddFood_SameExpiration_MergesIntoOneBatch()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 3);
        svc.AddFood(foodA, 2);
        Assert.AreEqual(5, svc.CheckStockAmount(foodA));
        Assert.AreEqual(1, svc.GetBatches(foodA).Count);
    }

    [Test]
    public void ConsumeFood_PartialBatchReduction()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 10);
        svc.ConsumeFood(foodA, 3);
        Assert.AreEqual(7, svc.CheckStockAmount(foodA));
    }

    [Test]
    public void ConsumeFood_AcrossBatches_FIFOByExpiry()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 2); // exp 3
        svc.AddFood(foodA, 5); // exp 3 (same)
        svc.ConsumeFood(foodA, 4);
        Assert.AreEqual(3, svc.CheckStockAmount(foodA));
    }

    [Test]
    public void ConsumeFood_AllStock_RemovesEntry()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 3);
        svc.ConsumeFood(foodA, 3);
        Assert.AreEqual(0, svc.CheckStockAmount(foodA));
        Assert.AreEqual(0, svc.GetBatches(foodA).Count);
    }

    [Test]
    public void AdvanceDay_DecrementsAndExpires()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 3); // exp 3 days
        svc.AdvanceDay();
        svc.AdvanceDay();
        Assert.AreEqual(3, svc.CheckStockAmount(foodA));
        svc.AdvanceDay(); // 3 → 0, expire
        Assert.AreEqual(0, svc.CheckStockAmount(foodA));
    }

    [Test]
    public void LoadIngredientsByCategory_FiltersCorrectly()
    {
        var svc = BuildSvc(foodA, foodB);
        svc.AddFood(foodA, 1);
        svc.AddFood(foodB, 1);

        var refList = svc.LoadIngredientsByCategory(IngredientDisplayCategory.Refrigerator);
        var upperList = svc.LoadIngredientsByCategory(IngredientDisplayCategory.UpperShelf);

        Assert.AreEqual(1, refList.Count);
        Assert.AreSame(foodA, refList[0].food);
        Assert.AreEqual(1, upperList.Count);
        Assert.AreSame(foodB, upperList[0].food);
    }

    [Test]
    public void SaveData_RoundTrips()
    {
        var svc1 = BuildSvc(foodA, foodB);
        svc1.AddFood(foodA, 7);
        svc1.AddFood(foodB, 3);

        var saved = svc1.GetSaveData();

        var svc2 = BuildSvc(foodA, foodB);
        svc2.ApplySaveData(saved);

        Assert.AreEqual(7, svc2.CheckStockAmount(foodA));
        Assert.AreEqual(3, svc2.CheckStockAmount(foodB));
    }

    [Test]
    public void AddHarvestedCrop_AddsByFoodId()
    {
        var svc = BuildSvc(foodA);
        svc.AddHarvestedCrop("IA", 4);
        Assert.AreEqual(4, svc.CheckStockAmount(foodA));
    }

    [Test]
    public void CanAcceptType_NullFood_ReturnsFalse()
    {
        var svc = BuildSvc(foodA);
        Assert.IsFalse(svc.CanAcceptType(null));
    }

    [Test]
    public void CanAcceptType_AlreadyHasItem_ReturnsTrue()
    {
        var svc = BuildSvc(foodA);
        svc.AddFood(foodA, 1);
        Assert.IsTrue(svc.CanAcceptType(foodA));
    }

    [Test]
    public void ResetToDefault_RecreatesStartingIngredients()
    {
        // 시작 재료 중 하나 (I007 = 고추장) 카탈로그 포함
        var ingI007 = ScriptableObject.CreateInstance<IngredientData>();
        ingI007.expirationDay = 5;
        var foodI007 = ScriptableObject.CreateInstance<FoodData>();
        foodI007.id = "I007"; foodI007.type = FoodType.INGREDIENT; foodI007.ingredient = ingI007;

        var svc = BuildSvc(foodA, foodI007);
        svc.AddFood(foodA, 99);
        svc.ResetToDefault();

        Assert.AreEqual(0, svc.CheckStockAmount(foodA));
        Assert.AreEqual(3, svc.CheckStockAmount(foodI007));

        Object.DestroyImmediate(foodI007);
        Object.DestroyImmediate(ingI007);
    }
}
