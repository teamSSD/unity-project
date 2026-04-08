using NUnit.Framework;
using UnityEngine;

public class MenuSelectionTest
{
    private FoodData mainFood;
    private FoodData side1;
    private FoodData side2;
    private FoodData side3;

    [SetUp]
    public void Setup()
    {
        mainFood = ScriptableObject.CreateInstance<FoodData>();
        mainFood.id = "M001";
        mainFood.type = FoodType.MAIN;

        side1 = ScriptableObject.CreateInstance<FoodData>();
        side1.id = "S001";
        side1.type = FoodType.SIDE;

        side2 = ScriptableObject.CreateInstance<FoodData>();
        side2.id = "S002";
        side2.type = FoodType.SIDE;

        side3 = ScriptableObject.CreateInstance<FoodData>();
        side3.id = "S003";
        side3.type = FoodType.SIDE;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(mainFood);
        Object.DestroyImmediate(side1);
        Object.DestroyImmediate(side2);
        Object.DestroyImmediate(side3);
    }

    [Test]
    public void NewMenuSelection_HasNoSelection()
    {
        var menu = new MenuSelection("도시락 1");
        Assert.IsFalse(menu.HasSelection());
    }

    [Test]
    public void SetMain_HasSelection()
    {
        var menu = new MenuSelection("도시락 1");
        menu.SetMain(mainFood);
        Assert.IsTrue(menu.HasSelection());
        Assert.AreEqual(mainFood, menu.MainMenu);
    }

    [Test]
    public void AddSide_MaxThree()
    {
        var menu = new MenuSelection("도시락 1");
        menu.AddSide(side1);
        menu.AddSide(side2);
        menu.AddSide(side3);
        Assert.AreEqual(3, menu.SideMenus.Count);

        // 4번째는 무시
        var extra = ScriptableObject.CreateInstance<FoodData>();
        menu.AddSide(extra);
        Assert.AreEqual(3, menu.SideMenus.Count);
        Object.DestroyImmediate(extra);
    }

    [Test]
    public void RemoveSide_RemovesCorrectly()
    {
        var menu = new MenuSelection("도시락 1");
        menu.AddSide(side1);
        menu.AddSide(side2);
        menu.RemoveSide(side1);
        Assert.AreEqual(1, menu.SideMenus.Count);
        Assert.AreEqual(side2, menu.SideMenus[0]);
    }

    [Test]
    public void Clear_ResetsAll()
    {
        var menu = new MenuSelection("도시락 1");
        menu.SetMain(mainFood);
        menu.AddSide(side1);
        menu.Clear();
        Assert.IsFalse(menu.HasSelection());
        Assert.AreEqual(0, menu.SideMenus.Count);
    }
}
