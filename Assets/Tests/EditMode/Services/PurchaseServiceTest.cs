using System.Collections.Generic;
using Game.Domain.Shop;
using NUnit.Framework;
using UnityEngine;

public class PurchaseServiceTest
{
    /// <summary>
    /// 테스트용 concrete ShopConfigSO. BuildSlotList 미리 주입된 슬롯 반환.
    /// </summary>
    private class FakeShopConfig : ShopConfigSO
    {
        public List<ItemShopSlotInfo> slots = new();
        public override List<ItemShopSlotInfo> BuildSlotList() => new List<ItemShopSlotInfo>(slots);
    }

    private static FoodData NewFood(string id)
    {
        var f = ScriptableObject.CreateInstance<FoodData>();
        f.id = id;
        f.ingredientName = id;
        return f;
    }

    private static ItemShopSlotInfo Slot(FoodData food, int stock)
    {
        return new ItemShopSlotInfo(food, stock, ProductType.General);
    }

    [Test]
    public void NullConfig_ReturnsEmptyList()
    {
        var svc = new PurchaseService(null);
        Assert.AreEqual(0, svc.GetItemListForDay(1).Count);
    }

    [Test]
    public void GetItemListForDay_FirstCall_BuildsAndCaches()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(Slot(food, 5));
        var svc = new PurchaseService(cfg);

        var slots1 = svc.GetItemListForDay(1);
        var slots2 = svc.GetItemListForDay(1);

        Assert.AreEqual(1, slots1.Count);
        Assert.AreSame(slots1, slots2); // 같은 캐시 반환

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void NotifyPurchased_ReducesRemaining()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(Slot(food, 5));
        var svc = new PurchaseService(cfg);

        svc.GetItemListForDay(1);
        svc.NotifyPurchased(food, 2);

        var info = cfg.slots[0];
        Assert.AreEqual(2, svc.GetPurchasedToday(food));
        Assert.AreEqual(3, svc.GetRemaining(info));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void DayChange_ResetsDailyPurchased()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(Slot(food, 5));
        var svc = new PurchaseService(cfg);

        svc.GetItemListForDay(1);
        svc.NotifyPurchased(food, 3);

        // 다음 날
        svc.GetItemListForDay(2);

        Assert.AreEqual(0, svc.GetPurchasedToday(food));
        Assert.AreEqual(5, svc.GetRemaining(cfg.slots[0]));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void GetRemaining_NeverNegative()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(Slot(food, 3));
        var svc = new PurchaseService(cfg);

        svc.GetItemListForDay(1);
        svc.NotifyPurchased(food, 100); // 과초과

        Assert.AreEqual(0, svc.GetRemaining(cfg.slots[0]));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }
}
