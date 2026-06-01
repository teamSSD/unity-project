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
}
