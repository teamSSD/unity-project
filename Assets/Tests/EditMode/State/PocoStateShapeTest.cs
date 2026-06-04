using Game.Schema.State;
using Game.Schema.State.Garden;
using Game.Schema.State.Mall;
using Game.Schema.State.Shop;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 상태 POCO 기본 인스턴스화/직렬화 라운드트립 검증.
/// (BasicStats, GardenPersistent, ShopPersistent, MallPersistent, GameState)
/// </summary>
public class PocoStateShapeTest
{
    [Test]
    public void BasicStats_DefaultValues()
    {
        var s = new BasicStats();
        Assert.AreEqual(0, s.stamina);
        Assert.AreEqual(0, s.day);
        Assert.AreEqual(0, s.time);
        Assert.AreEqual(0, s.money);
    }

    [Test]
    public void BasicStats_JsonRoundTrip()
    {
        var s = new BasicStats { stamina = 50, day = 7, time = 720, money = 12345 };
        var json = JsonUtility.ToJson(s);
        var clone = JsonUtility.FromJson<BasicStats>(json);
        Assert.AreEqual(50, clone.stamina);
        Assert.AreEqual(7, clone.day);
        Assert.AreEqual(720, clone.time);
        Assert.AreEqual(12345, clone.money);
    }

    [Test]
    public void GardenPersistent_InitialEmpty()
    {
        var g = new GardenPersistent();
        Assert.AreEqual(0, g.upgradeTypes.Count);
        Assert.AreEqual(0, g.upgradeLevels.Count);
        Assert.AreEqual(GardenPersistent.TileCount, g.tiles.Length);
    }

    [Test]
    public void GardenPersistent_JsonRoundTrip()
    {
        var g = new GardenPersistent();
        g.upgradeTypes.Add("tile");
        g.upgradeLevels.Add(2);
        var clone = JsonUtility.FromJson<GardenPersistent>(JsonUtility.ToJson(g));
        Assert.AreEqual(1, clone.upgradeTypes.Count);
        Assert.AreEqual("tile", clone.upgradeTypes[0]);
        Assert.AreEqual(2, clone.upgradeLevels[0]);
    }

    [Test]
    public void ShopPersistent_InitialEmpty()
    {
        var s = new ShopPersistent();
        Assert.AreEqual(0, s.storageTypes.Count);
        Assert.AreEqual(0, s.toolIds.Count);
    }

    [Test]
    public void ShopPersistent_JsonRoundTrip()
    {
        var s = new ShopPersistent();
        s.storageTypes.Add("refrigerator");
        s.storageLevels.Add(3);
        s.toolIds.Add("T001");
        s.toolLevels.Add(1);
        var clone = JsonUtility.FromJson<ShopPersistent>(JsonUtility.ToJson(s));
        Assert.AreEqual("refrigerator", clone.storageTypes[0]);
        Assert.AreEqual(3, clone.storageLevels[0]);
        Assert.AreEqual("T001", clone.toolIds[0]);
        Assert.AreEqual(1, clone.toolLevels[0]);
    }

    [Test]
    public void MallPersistent_InitialEmpty()
    {
        var m = new MallPersistent();
        Assert.AreEqual(0, m.questGroupIds.Count);
    }

    [Test]
    public void MallPersistent_JsonRoundTrip()
    {
        var m = new MallPersistent();
        m.questGroupIds.Add("npc_chef");
        m.questStages.Add(2);
        var clone = JsonUtility.FromJson<MallPersistent>(JsonUtility.ToJson(m));
        Assert.AreEqual("npc_chef", clone.questGroupIds[0]);
        Assert.AreEqual(2, clone.questStages[0]);
    }

    [Test]
    public void GameState_TreeShape()
    {
        var gs = new GameState();
        Assert.IsNotNull(gs.stats);
        Assert.IsNotNull(gs.phase);
        Assert.IsNotNull(gs.garden?.persistent);
        Assert.IsNotNull(gs.shop?.persistent);
        Assert.IsNotNull(gs.mall?.persistent);
    }

    [Test]
    public void PhaseData_DefaultMorning()
    {
        var pd = new PhaseData();
        Assert.AreEqual(0, pd.Day);
        Assert.IsNotNull(pd.SelectedMenus);
        Assert.IsNotNull(pd.UnlockedRecipes);
    }
}
