using System.Collections.Generic;
using Game.Domain.Cooking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MenuSelectionServiceTest
{
    private FoodData mainA;
    private FoodData sideA;
    private FoodData sideB;

    [SetUp]
    public void Setup()
    {
        mainA = ScriptableObject.CreateInstance<FoodData>();
        mainA.id = "MA"; mainA.type = FoodType.MAIN;
        sideA = ScriptableObject.CreateInstance<FoodData>();
        sideA.id = "SA"; sideA.type = FoodType.SIDE;
        sideB = ScriptableObject.CreateInstance<FoodData>();
        sideB.id = "SB"; sideB.type = FoodType.SIDE;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(mainA);
        Object.DestroyImmediate(sideA);
        Object.DestroyImmediate(sideB);
    }

    private MenuSelectionService BuildSvc(params FoodData[] catalog)
        => new MenuSelectionService(catalog);

    [Test]
    public void Initial_AllMenusEmpty()
    {
        var svc = BuildSvc(mainA, sideA);
        Assert.IsFalse(svc.HasAnySelection());
        for (int i = 0; i < 3; i++)
            Assert.IsNotNull(svc.GetMenu(i));
    }

    [Test]
    public void GetMenu_InvalidIndex_ReturnsNull()
    {
        var svc = BuildSvc();
        LogAssert.Expect(LogType.Error, "[MenuSelectionService] Invalid menu index: -1");
        Assert.IsNull(svc.GetMenu(-1));
        LogAssert.Expect(LogType.Error, "[MenuSelectionService] Invalid menu index: 99");
        Assert.IsNull(svc.GetMenu(99));
    }

    [Test]
    public void HasAnySelection_TrueAfterSetMain()
    {
        var svc = BuildSvc(mainA);
        svc.GetMenu(0).SetMain(mainA);
        Assert.IsTrue(svc.HasAnySelection());
    }

    [Test]
    public void ClearAllMenus_ResetsAll()
    {
        var svc = BuildSvc(mainA);
        svc.GetMenu(0).SetMain(mainA);
        svc.ClearAllMenus();
        Assert.IsFalse(svc.HasAnySelection());
    }

    [Test]
    public void GetAllMenusAsSchema_ReturnsOnlySelected()
    {
        var svc = BuildSvc(mainA, sideA);
        svc.GetMenu(0).SetMain(mainA);
        svc.GetMenu(0).AddSide(sideA);

        var schemas = svc.GetAllMenusAsSchema();
        Assert.AreEqual(1, schemas.Count);
        Assert.AreEqual(1, schemas[0].orderNumber); // index 0 → orderNumber 1
        Assert.AreSame(mainA, schemas[0].mainMenus[0]);
    }

    [Test]
    public void SaveData_RoundTrips()
    {
        var svc1 = BuildSvc(mainA, sideA, sideB);
        svc1.GetMenu(0).SetMain(mainA);
        svc1.GetMenu(0).AddSide(sideA);
        svc1.GetMenu(0).AddSide(sideB);

        var saved = svc1.GetSaveData();

        var svc2 = BuildSvc(mainA, sideA, sideB);
        svc2.ApplySaveData(saved);

        var menu0 = svc2.GetMenu(0);
        Assert.AreEqual("MA", menu0.MainMenu?.id);
        Assert.AreEqual(2, menu0.SideMenus.Count);
    }

    [Test]
    public void ApplySaveData_NullSafe()
    {
        var svc = BuildSvc();
        Assert.DoesNotThrow(() => svc.ApplySaveData(null));
        Assert.DoesNotThrow(() => svc.ApplySaveData(new RecipeBookSaveData()));
    }

    [Test]
    public void SaveData_EmptySlots_PreservesIndices()
    {
        var svc1 = BuildSvc(mainA);
        svc1.GetMenu(2).SetMain(mainA); // 3번 슬롯만 선택

        var saved = svc1.GetSaveData();

        Assert.AreEqual(3, saved.selectedMenus.Count);
        Assert.AreEqual("", saved.selectedMenus[0]);
        Assert.AreEqual("", saved.selectedMenus[1]);
        Assert.IsTrue(saved.selectedMenus[2].StartsWith("MA|"));
    }
}
