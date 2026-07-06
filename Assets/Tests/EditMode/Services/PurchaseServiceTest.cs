using System.Collections.Generic;
using Game.Domain.Shop;
using NUnit.Framework;
using UnityEngine;

public class PurchaseServiceTest
{
    /// <summary>
    /// 테스트용 concrete ShopConfigSO. BuildSlotList가 미리 주입된 슬롯 반환 (RNG 무시).
    /// </summary>
    private class FakeShopConfig : ShopConfigSO
    {
        public List<ItemShopSlotInfo> slots = new();
        public override List<ItemShopSlotInfo> BuildSlotList(System.Random rng) => new List<ItemShopSlotInfo>(slots);
    }

    private static FoodData NewFood(string id)
    {
        var f = ScriptableObject.CreateInstance<FoodData>();
        f.id = id;
        f.ingredientName = id;
        return f;
    }

    private static ItemShopSlotInfo SpecialSlot(FoodData food, int stock)
    {
        return new ItemShopSlotInfo(food, stock, ProductType.Special);
    }

    private static ItemShopSlotInfo GeneralSlot(FoodData food)
    {
        return new ItemShopSlotInfo(food, -1, ProductType.General);
    }

    [Test]
    public void NullConfig_ReturnsEmptyList()
    {
        var svc = new PurchaseService(null, null, null, null);
        Assert.AreEqual(0, svc.GetItemList(1, 0).Count);
    }

    [Test]
    public void GetItemList_FirstCall_BuildsAndCaches()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(SpecialSlot(food, 5));
        var svc = new PurchaseService(cfg, null, null, null);

        var slots1 = svc.GetItemList(1, 0);
        var slots2 = svc.GetItemList(1, 0);

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
        cfg.slots.Add(SpecialSlot(food, 5));
        var svc = new PurchaseService(cfg, null, null, null);

        svc.GetItemList(1, 0);
        svc.NotifyPurchased(food, 2);

        var info = cfg.slots[0];
        Assert.AreEqual(2, svc.GetPurchasedThisPhase(food));
        Assert.AreEqual(3, svc.GetRemaining(info));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void PhaseChange_ResetsPhasePurchased()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(SpecialSlot(food, 5));
        var svc = new PurchaseService(cfg, null, null, null);

        svc.GetItemList(1, 0);
        svc.NotifyPurchased(food, 3);

        // 같은 날 다음 페이즈
        svc.GetItemList(1, 1);

        Assert.AreEqual(0, svc.GetPurchasedThisPhase(food));
        Assert.AreEqual(5, svc.GetRemaining(cfg.slots[0]));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void DayChange_ResetsPhasePurchased()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(SpecialSlot(food, 5));
        var svc = new PurchaseService(cfg, null, null, null);

        svc.GetItemList(1, 0);
        svc.NotifyPurchased(food, 3);

        // 다음 날 같은 페이즈
        svc.GetItemList(2, 0);

        Assert.AreEqual(0, svc.GetPurchasedThisPhase(food));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void GetRemaining_NeverNegative()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(SpecialSlot(food, 3));
        var svc = new PurchaseService(cfg, null, null, null);

        svc.GetItemList(1, 0);
        svc.NotifyPurchased(food, 100); // 과초과

        Assert.AreEqual(0, svc.GetRemaining(cfg.slots[0]));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }

    [Test]
    public void GetRemaining_UnlimitedGeneral_ReturnsMaxValue()
    {
        var food = NewFood("F001");
        var cfg = ScriptableObject.CreateInstance<FakeShopConfig>();
        cfg.slots.Add(GeneralSlot(food));
        var svc = new PurchaseService(cfg, null, null, null);

        svc.GetItemList(1, 0);
        svc.NotifyPurchased(food, 100);

        Assert.IsTrue(cfg.slots[0].IsUnlimited);
        Assert.AreEqual(int.MaxValue, svc.GetRemaining(cfg.slots[0]));

        ScriptableObject.DestroyImmediate(cfg);
        ScriptableObject.DestroyImmediate(food);
    }
}
