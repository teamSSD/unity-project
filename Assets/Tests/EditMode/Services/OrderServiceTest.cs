using System.Collections.Generic;
using Game.Domain.Mall;
using NUnit.Framework;

public class OrderServiceTest
{
    private OrderService svc;
    private FakeMoneyService money;

    [SetUp]
    public void Setup()
    {
        money = new FakeMoneyService { Current = 0 };
        svc = new OrderService(money);
    }

    private static MenuSchema MakeMenu(int orderNumber)
    {
        return new MenuSchema("test_menu", orderNumber, (FoodData)null, new List<FoodData>());
    }

    [Test]
    public void GenerateOrder_AddsToList()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "npc_a");

        Assert.AreEqual(1, svc.GetOrders().Count);
        var order = svc.GetOrder("q1");
        Assert.IsNotNull(order);
        Assert.AreEqual("npc_a", order.npcId);
        Assert.AreEqual(DeliveryOrderState.Ordered, order.state);
    }

    [Test]
    public void MarkCookedWithPrice_TransitionsToCooked()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "npc_a");

        bool ok = svc.MarkCookedWithPrice("q1", 500);

        Assert.IsTrue(ok);
        var order = svc.GetOrder("q1");
        Assert.AreEqual(DeliveryOrderState.Cooked, order.state);
        Assert.AreEqual(500, order.cookedPrice);
    }

    [Test]
    public void MarkCookedWithPrice_FailsIfNotOrdered()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "npc_a");
        svc.MarkCookedWithPrice("q1", 500);

        bool again = svc.MarkCookedWithPrice("q1", 999);
        Assert.IsFalse(again);
        Assert.AreEqual(500, svc.GetOrder("q1").cookedPrice);  // 변경 안 됨
    }

    [Test]
    public void ConsumeBento_AddsMoneyAndDelivers()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "npc_a");
        svc.MarkCookedWithPrice("q1", 750);

        int reward = svc.ConsumeBento("q1");

        Assert.AreEqual(750, reward);
        Assert.AreEqual(750, money.Current);
        Assert.AreEqual(DeliveryOrderState.Delivered, svc.GetOrder("q1").state);
    }

    [Test]
    public void ConsumeBento_UnknownQuest_ReturnsZero()
    {
        Assert.AreEqual(0, svc.ConsumeBento("missing"));
    }

    [Test]
    public void Clear_RemovesAllOrders()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "a");
        svc.GenerateOrder(MakeMenu(2), "q2", "b");
        svc.Clear();
        Assert.AreEqual(0, svc.GetOrders().Count);
    }

    [Test]
    public void SetOrders_ReplacesContent()
    {
        svc.GenerateOrder(MakeMenu(1), "q1", "a");

        svc.SetOrders(new[] {
            new DeliveryOrderData { questId = "loaded", state = DeliveryOrderState.Cooked, cookedPrice = 999, menuSchema = MakeMenu(99) },
        });

        Assert.AreEqual(1, svc.GetOrders().Count);
        Assert.AreEqual("loaded", svc.GetOrders()[0].questId);
        Assert.AreEqual(999, svc.GetOrders()[0].cookedPrice);
    }
}
