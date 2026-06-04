using Game.Domain.Cooking;
using NUnit.Framework;
using UnityEngine;

public class UnlockedFoodServiceTest
{
    private FoodData main1, main2, side1, side2;

    [SetUp]
    public void Setup()
    {
        main1 = ScriptableObject.CreateInstance<FoodData>();
        main1.id = "I044"; main1.type = FoodType.MAIN;
        main2 = ScriptableObject.CreateInstance<FoodData>();
        main2.id = "I060"; main2.type = FoodType.MAIN;
        side1 = ScriptableObject.CreateInstance<FoodData>();
        side1.id = "I046"; side1.type = FoodType.SIDE;
        side2 = ScriptableObject.CreateInstance<FoodData>();
        side2.id = "I062"; side2.type = FoodType.SIDE;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(main1);
        Object.DestroyImmediate(main2);
        Object.DestroyImmediate(side1);
        Object.DestroyImmediate(side2);
    }

    private UnlockedFoodService BuildSvc()
        => new UnlockedFoodService(new[] { main1, main2, side1, side2 });

    [Test]
    public void Initial_NothingUnlocked()
    {
        var svc = BuildSvc();
        Assert.IsFalse(svc.IsUnlocked("I044"));
        Assert.AreEqual(0, svc.GetUnlockedMainFoods().Count);
        Assert.AreEqual(0, svc.GetUnlockedSideFoods().Count);
    }

    [Test]
    public void UnlockDefaultRecipes_AddsTwoMainsOneSide()
    {
        var svc = BuildSvc();
        svc.UnlockDefaultRecipes();
        Assert.IsTrue(svc.IsUnlocked("I044"));
        Assert.IsTrue(svc.IsUnlocked("I060"));
        Assert.IsTrue(svc.IsUnlocked("I046"));
        Assert.IsFalse(svc.IsUnlocked("I062")); // 환기구 연어구이는 시작 unlock에서 제외 (시작 재료 인벤토리 cap fit)
    }

    [Test]
    public void UnlockRecipe_Idempotent()
    {
        var svc = BuildSvc();
        svc.UnlockRecipe("I044");
        svc.UnlockRecipe("I044");
        Assert.AreEqual(1, svc.GetUnlockedMainFoods().Count);
    }

    [Test]
    public void LockRecipe_RemovesFromUnlocked()
    {
        var svc = BuildSvc();
        svc.UnlockRecipe("I044");
        svc.LockRecipe("I044");
        Assert.IsFalse(svc.IsUnlocked("I044"));
    }

    [Test]
    public void UnlockAll_UnlocksAllCatalog()
    {
        var svc = BuildSvc();
        svc.UnlockAll();
        Assert.AreEqual(2, svc.GetUnlockedMainFoods().Count);
        Assert.AreEqual(2, svc.GetUnlockedSideFoods().Count);
    }

    [Test]
    public void GetAllMainFoods_FiltersByType()
    {
        var svc = BuildSvc();
        Assert.AreEqual(2, svc.GetAllMainFoods().Count);
        Assert.AreEqual(2, svc.GetAllSideFoods().Count);
    }

    [Test]
    public void SaveData_RoundTrips()
    {
        var svc1 = BuildSvc();
        svc1.UnlockRecipe("I044");
        svc1.UnlockRecipe("I046");

        var saved = svc1.GetSaveData();

        var svc2 = BuildSvc();
        svc2.ApplySaveData(saved);

        Assert.IsTrue(svc2.IsUnlocked("I044"));
        Assert.IsTrue(svc2.IsUnlocked("I046"));
        Assert.IsFalse(svc2.IsUnlocked("I060"));
    }

    [Test]
    public void ApplySaveData_EmptyData_UnlocksDefaults()
    {
        var svc = BuildSvc();
        svc.ApplySaveData(new UnlockedRecipesSaveData());
        Assert.IsTrue(svc.IsUnlocked("I044"));
        Assert.IsTrue(svc.IsUnlocked("I060"));
        Assert.IsTrue(svc.IsUnlocked("I046"));
        Assert.IsFalse(svc.IsUnlocked("I062")); // 시작 unlock에서 제외
    }

    [Test]
    public void ApplySaveData_NullData_UnlocksDefaults()
    {
        var svc = BuildSvc();
        svc.ApplySaveData(null);
        Assert.IsTrue(svc.IsUnlocked("I044"));
    }
}
