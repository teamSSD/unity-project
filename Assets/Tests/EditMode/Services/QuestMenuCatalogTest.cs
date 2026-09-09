using System.Collections.Generic;
using Game.Domain.Mall;
using NUnit.Framework;

public class QuestMenuCatalogTest
{
    private static MenuSchema MakeMenu(string name)
    {
        return new MenuSchema(name, -1, (FoodData)null, new List<FoodData>());
    }

    [Test]
    public void GetByGroupId_ReturnsRegisteredMenu()
    {
        var catalog = new QuestMenuCatalog(new[] {
            ("group_a", MakeMenu("menu_a")),
            ("group_b", MakeMenu("menu_b")),
        });

        Assert.AreEqual("menu_a", catalog.GetByGroupId("group_a").name);
        Assert.AreEqual("menu_b", catalog.GetByGroupId("group_b").name);
    }

    [Test]
    public void GetByGroupId_UnknownGroup_ReturnsNull()
    {
        var catalog = new QuestMenuCatalog(new[] { ("a", MakeMenu("m")) });
        Assert.IsNull(catalog.GetByGroupId("missing"));
    }

    [Test]
    public void EmptyCatalog_ReturnsNullForAny()
    {
        var catalog = new QuestMenuCatalog(System.Array.Empty<(string, MenuSchema)>());
        Assert.IsNull(catalog.GetByGroupId("anything"));
    }

    [Test]
    public void CreateOrderMenu_PreservesEveryMainAndSideWithoutMutatingTemplate()
    {
        var mainA = UnityEngine.ScriptableObject.CreateInstance<FoodData>();
        var mainB = UnityEngine.ScriptableObject.CreateInstance<FoodData>();
        var side = UnityEngine.ScriptableObject.CreateInstance<FoodData>();
        var template = new MenuSchema(
            "multi-main",
            -1,
            new List<FoodData> { mainA, mainB },
            new List<FoodData> { side });
        var catalog = new QuestMenuCatalog(new[] { ("group", template) });

        var order = catalog.CreateOrderMenu("group", 7);

        Assert.AreEqual(7, order.orderNumber);
        CollectionAssert.AreEqual(new[] { mainA, mainB }, order.mainMenus);
        CollectionAssert.AreEqual(new[] { side }, order.sideMenus);
        Assert.AreNotSame(template.mainMenus, order.mainMenus);
        Assert.AreNotSame(template.sideMenus, order.sideMenus);
    }
}
