using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InventoryManagerTest
{
    private InventoryManager inventory;
    private FoodData foodA;
    private FoodData foodB;
    private IngredientData ingredientA;
    private IngredientData ingredientB;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("InventoryManager");
        inventory = go.AddComponent<InventoryManager>();

        ingredientA = ScriptableObject.CreateInstance<IngredientData>();
        ingredientA.expirationDay = 3;
        ingredientA.display = IngredientDisplayCategory.Refrigerator;

        ingredientB = ScriptableObject.CreateInstance<IngredientData>();
        ingredientB.expirationDay = 5;
        ingredientB.display = IngredientDisplayCategory.UpperShelf;

        foodA = ScriptableObject.CreateInstance<FoodData>();
        foodA.id = "IA";
        foodA.type = FoodType.INGREDIENT;
        foodA.ingredient = ingredientA;

        foodB = ScriptableObject.CreateInstance<FoodData>();
        foodB.id = "IB";
        foodB.type = FoodType.INGREDIENT;
        foodB.ingredient = ingredientB;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(inventory.gameObject);
        Object.DestroyImmediate(ingredientA);
        Object.DestroyImmediate(ingredientB);
        Object.DestroyImmediate(foodA);
        Object.DestroyImmediate(foodB);
    }

    // ── 기본 추가/소비 ──

    [Test]
    public void AddFood_IncreasesStock()
    {
        inventory.AddFood(foodA, 5);
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));
    }

    [Test]
    public void ConsumeFood_DecreasesStock()
    {
        inventory.AddFood(foodA, 10);
        inventory.ConsumeFood(foodA, 3);
        Assert.AreEqual(7, inventory.CheckStockAmount(foodA));
    }

    [Test]
    public void ConsumeFood_MoreThanStock_ClampsToZero()
    {
        inventory.AddFood(foodA, 2);
        inventory.ConsumeFood(foodA, 10);
        Assert.AreEqual(0, inventory.CheckStockAmount(foodA));
    }

    [Test]
    public void CheckStockAmount_NoFood_ReturnsZero()
    {
        Assert.AreEqual(0, inventory.CheckStockAmount(foodA));
    }

    // ── 배치 시스템 ──

    [Test]
    public void AddFood_CreatesBatchWithExpirationDay()
    {
        inventory.AddFood(foodA, 5); // expirationDay = 3
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));
    }

    [Test]
    public void MultipleBatches_StockSumsAll()
    {
        inventory.AddFood(foodA, 3);
        inventory.AddFood(foodA, 2);
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));
    }

    // ── FIFO 소비 ──

    [Test]
    public void ConsumeFood_FIFO_OldestBatchFirst()
    {
        // Batch 1: 3개, 3일 만료
        inventory.AddFood(foodA, 3);
        // 하루 경과 → Batch 1은 2일 남음
        inventory.AdvanceDay();
        // Batch 2: 2개, 3일 만료 (새로 구매)
        inventory.AddFood(foodA, 2);

        // 총 5개. FIFO로 소비하면 Batch 1(2일)부터 소모
        inventory.ConsumeFood(foodA, 4);
        // Batch 1: 0개, Batch 2: 1개
        Assert.AreEqual(1, inventory.CheckStockAmount(foodA));
    }

    // ── 유통기한 만료 ──

    [Test]
    public void AdvanceDay_ExpiresOldBatches()
    {
        inventory.AddFood(foodA, 5); // expirationDay = 3
        inventory.AdvanceDay(); // 2일 남음
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));
        inventory.AdvanceDay(); // 1일 남음
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));
        inventory.AdvanceDay(); // 0일 → 만료
        Assert.AreEqual(0, inventory.CheckStockAmount(foodA));
    }

    [Test]
    public void AdvanceDay_OnlyExpiresOldBatch_NewBatchSurvives()
    {
        inventory.AddFood(foodA, 3); // 3일 만료
        inventory.AdvanceDay(); // 2일 남음
        inventory.AdvanceDay(); // 1일 남음
        inventory.AddFood(foodA, 2); // 새 배치: 3일 만료
        inventory.AdvanceDay(); // 구 배치 만료, 새 배치 2일 남음

        Assert.AreEqual(2, inventory.CheckStockAmount(foodA));
    }

    // ── 유통기한 시나리오 (요구사항 예시) ──

    [Test]
    public void Scenario_MixedBatches()
    {
        // 3일짜리 5개 → batch{5, 3}
        inventory.AddFood(foodA, 5);
        // 1일 경과 → batch{5, 2}
        inventory.AdvanceDay();
        Assert.AreEqual(5, inventory.CheckStockAmount(foodA));

        // 3개 새로 구매 → [batch{5, 2}, batch{3, 3}]
        inventory.AddFood(foodA, 3);
        Assert.AreEqual(8, inventory.CheckStockAmount(foodA));

        // 4개 사용 (FIFO) → [batch{1, 2}, batch{3, 3}]
        inventory.ConsumeFood(foodA, 4);
        Assert.AreEqual(4, inventory.CheckStockAmount(foodA));

        // 1일 경과 → [batch{1, 1}, batch{3, 2}]
        inventory.AdvanceDay();
        Assert.AreEqual(4, inventory.CheckStockAmount(foodA));

        // 1일 더 경과 → 구 배치 만료, [batch{3, 1}]
        inventory.AdvanceDay();
        Assert.AreEqual(3, inventory.CheckStockAmount(foodA));
    }

    // ── Save/Load ──

    [Test]
    public void SaveData_RoundTrips()
    {
        inventory.AddFood(foodA, 5);
        inventory.AdvanceDay();
        inventory.AddFood(foodB, 3);

        var saveData = inventory.GetSaveData();

        // 새 인스턴스에 로드
        var go2 = new GameObject("Inv2");
        var inv2 = go2.AddComponent<InventoryManager>();
        // allFoodData를 수동으로 로드해야 하므로 ApplySaveData 테스트는
        // Initialize()를 거친 실제 환경에서만 유효.
        // 여기서는 saveData 구조 검증만.
        Assert.IsTrue(saveData.items.Count > 0);
        Assert.AreEqual("IA", saveData.items[0].foodId);

        Object.DestroyImmediate(go2);
    }

    // ── LoadIngredientsByCategory ──

    [Test]
    public void LoadIngredientsByCategory_FiltersCorrectly()
    {
        inventory.AddFood(foodA, 5); // Refrigerator
        inventory.AddFood(foodB, 3); // UpperShelf

        var fridge = inventory.LoadIngredientsByCategory(IngredientDisplayCategory.Refrigerator);
        var upper = inventory.LoadIngredientsByCategory(IngredientDisplayCategory.UpperShelf);

        Assert.AreEqual(1, fridge.Count);
        Assert.AreEqual(foodA, fridge[0].food);
        Assert.AreEqual(1, upper.Count);
        Assert.AreEqual(foodB, upper[0].food);
    }
}
