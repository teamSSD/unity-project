using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Phase 1 tests: Verify isLocked field removal and Model data usage
/// </summary>
public class BentoToggleListDataTest
{
    private DiaryModel model;
    private BentoToggleList bentoList;
    private GameObject testObject;

    [SetUp]
    public void SetUp()
    {
        // Create test GameObject
        testObject = new GameObject("TestBentoToggleList");
        bentoList = testObject.AddComponent<BentoToggleList>();

        // Create DiaryModel with mock progressor
        model = new DiaryModel(new MockPhaseProgressor());

        // Initialize BentoToggleList with Model
        bentoList.Initialize(model);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    /// <summary>
    /// Phase 1.1: Verify lock state uses Model only (no isLocked field)
    /// </summary>
    [Test]
    public void TestLockStateUsesModelOnly()
    {
        // Given: Model is not locked initially
        Assert.IsFalse(model.IsBentoLocked, "Model should start unlocked");

        // When: Lock the model
        model.LockBentoSelections();

        // Then: Model reflects locked state
        Assert.IsTrue(model.IsBentoLocked, "Model should be locked");

        // Verify: No isLocked field exists in BentoToggleList
        var fields = typeof(BentoToggleList).GetFields(
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        var isLockedField = System.Array.Find(fields, f => f.Name == "isLocked");
        Assert.IsNull(isLockedField, "BentoToggleList should not have isLocked field");

        // Cleanup
        model.UnlockBentoSelections();
    }

    /// <summary>
    /// Phase 1.2: Verify UI display uses Model data
    /// </summary>
    [Test]
    public void TestUIDisplayUsesModelData()
    {
        // Given: Add food to Model
        var testFood = ScriptableObject.CreateInstance<FoodData>();
        testFood.id = "test_main_1";
        testFood.ingredientName = "Test Main";
        testFood.type = FoodType.MAIN;

        model.AddBentoFood(0, testFood);

        // When: Call HasAnySelection (should use Model data)
        bool hasSelection = bentoList.HasAnySelection();

        // Then: Returns true (from Model data)
        Assert.IsTrue(hasSelection, "Should detect selection from Model data");

        // Verify: Model has the food
        var bentoFromModel = model.GetBentoForDisplay(0);
        Assert.IsNotNull(bentoFromModel, "Model should have bento at index 0");
        Assert.AreEqual(testFood, bentoFromModel.MainMenu, "Model should have the test food");

        // Cleanup
        Object.DestroyImmediate(testFood);
    }

    /// <summary>
    /// Phase 1.2: Verify ToString uses Model data
    /// </summary>
    [Test]
    public void TestToStringUsesModelData()
    {
        // Given: Add food to Model
        var testFood = ScriptableObject.CreateInstance<FoodData>();
        testFood.id = "test_main_2";
        testFood.ingredientName = "Test Main 2";
        testFood.type = FoodType.MAIN;

        model.AddBentoFood(0, testFood);

        // When: Call ToString (should use Model data)
        string summary = bentoList.ToString();

        // Then: Summary contains food from Model
        Assert.IsTrue(summary.Contains("Test Main 2"), $"ToString should use Model data. Got: {summary}");

        // Cleanup
        Object.DestroyImmediate(testFood);
    }

    /// <summary>
    /// Phase 1.2: Verify GetBentoCount uses Model
    /// </summary>
    [Test]
    public void TestGetBentoCountFromModel()
    {
        // Given: Model has 3 bentos
        int count = model.GetBentoCount();

        // Then: Count is 3
        Assert.AreEqual(3, count, "Model should have 3 bentos");
    }

    /// <summary>
    /// Phase 1.2: Verify GetBentoForDisplay returns data
    /// </summary>
    [Test]
    public void TestGetBentoForDisplay()
    {
        // Given: Valid index
        int index = 0;

        // When: Get bento for display
        var bento = model.GetBentoForDisplay(index);

        // Then: Returns valid bento
        Assert.IsNotNull(bento, "Should return bento at valid index");
        Assert.AreEqual("도시락 1", bento.Name, "Bento name should match");

        // When: Invalid index
        var nullBento = model.GetBentoForDisplay(-1);

        // Then: Returns null
        Assert.IsNull(nullBento, "Should return null for invalid index");
    }

    /// <summary>
    /// Mock phase progressor for testing
    /// </summary>
    private class MockPhaseProgressor : IPhaseProgressor
    {
        public void PassPhase() { }
    }
}
