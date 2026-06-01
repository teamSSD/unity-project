using System.Collections.Generic;
using Game.Domain.Shop;
using Game.Schema.State.Shop;
using NUnit.Framework;

public class StorageUpgradeServiceTest
{
    private ShopPersistent state;
    private FakeMoneyService money;
    private FakeExpenseLog expense;

    private static StorageUpgradeData Row(string type, int level, int cost, int value)
    {
        return new StorageUpgradeData { type = type, level = level, cost = cost, value = value };
    }

    private StorageUpgradeService Build(IEnumerable<StorageUpgradeData> rows, int initialMoney = 1000)
    {
        state = new ShopPersistent();
        money = new FakeMoneyService { Current = initialMoney };
        expense = new FakeExpenseLog();
        return new StorageUpgradeService(state, rows, money, expense);
    }

    [Test]
    public void TryUpgrade_Success()
    {
        var svc = Build(new[] {
            Row("refrigerator", 0, 0, 7),
            Row("refrigerator", 1, 200, 10),
        }, initialMoney: 500);

        bool ok = svc.TryUpgrade("refrigerator");

        Assert.IsTrue(ok);
        Assert.AreEqual(10, svc.GetCurrentData("refrigerator").value);
        Assert.AreEqual(300, money.Current);
    }

    [Test]
    public void IsMax_TrueAtTopLevel()
    {
        var svc = Build(new[] { Row("refrigerator", 0, 0, 7) });
        Assert.IsTrue(svc.IsMax("refrigerator"));
    }

    [Test]
    public void TryUpgrade_InsufficientMoney_Fails()
    {
        var svc = Build(new[] { Row("upperShelf", 0, 0, 3), Row("upperShelf", 1, 500, 5) }, initialMoney: 100);
        Assert.IsFalse(svc.TryUpgrade("upperShelf"));
        Assert.AreEqual(0, svc.GetCurrentData("upperShelf").level);
    }

    [Test]
    public void StatePersists_SamePersistentReusedAfterReconstruct()
    {
        var svc1 = Build(new[] { Row("lowerShelf", 0, 0, 4), Row("lowerShelf", 1, 100, 6) }, initialMoney: 500);
        svc1.TryUpgrade("lowerShelf");

        var svc2 = new StorageUpgradeService(state,
            new[] { Row("lowerShelf", 0, 0, 4), Row("lowerShelf", 1, 100, 6) },
            new FakeMoneyService { Current = 0 }, new FakeExpenseLog());

        Assert.AreEqual(1, svc2.GetCurrentData("lowerShelf").level);
    }
}
