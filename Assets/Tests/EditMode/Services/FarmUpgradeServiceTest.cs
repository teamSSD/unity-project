using System.Collections.Generic;
using Game.Domain.Garden;
using Game.Schema.State.Garden;
using NUnit.Framework;

public class FarmUpgradeServiceTest
{
    private GardenPersistent state;
    private FakeMoneyService money;
    private FakeExpenseLog expense;

    private static FarmUpgradeData Row(string type, int level, int cost, float value)
    {
        return new FarmUpgradeData { type = type, level = level, cost = cost, value = value };
    }

    private FarmUpgradeService Build(IEnumerable<FarmUpgradeData> rows, int initialMoney = 1000)
    {
        state = new GardenPersistent();
        money = new FakeMoneyService { Current = initialMoney };
        expense = new FakeExpenseLog();
        return new FarmUpgradeService(state, rows, money, expense);
    }

    [Test]
    public void Constructor_InitializesAllTypesAtLevel0()
    {
        var svc = Build(new[] {
            Row("tile", 0, 0, 3),
            Row("tile", 1, 100, 4),
            Row("harvestCount", 0, 0, 5),
        });

        Assert.AreEqual(0, svc.GetCurrentData("tile").level);
        Assert.AreEqual(0, svc.GetCurrentData("harvestCount").level);
    }

    [Test]
    public void GetNextData_ReturnsLevelPlusOne()
    {
        var svc = Build(new[] { Row("tile", 0, 0, 3), Row("tile", 1, 100, 4) });
        Assert.AreEqual(1, svc.GetNextData("tile").level);
        Assert.AreEqual(100, svc.GetNextData("tile").cost);
    }

    [Test]
    public void IsMax_TrueWhenNoNextLevel()
    {
        var svc = Build(new[] { Row("tile", 0, 0, 3) });
        Assert.IsTrue(svc.IsMax("tile"));
    }

    [Test]
    public void TryUpgrade_SuccessOnSufficientMoney()
    {
        var svc = Build(new[] { Row("tile", 0, 0, 3), Row("tile", 1, 100, 4) }, initialMoney: 500);

        bool ok = svc.TryUpgrade("tile");

        Assert.IsTrue(ok);
        Assert.AreEqual(1, svc.GetCurrentData("tile").level);
        Assert.AreEqual(400, money.Current);
        Assert.AreEqual(1, expense.Entries.Count);
        Assert.AreEqual(("업그레이드", 100), expense.Entries[0]);
    }

    [Test]
    public void TryUpgrade_FailureOnInsufficientMoney()
    {
        var svc = Build(new[] { Row("tile", 0, 0, 3), Row("tile", 1, 100, 4) }, initialMoney: 50);

        bool ok = svc.TryUpgrade("tile");

        Assert.IsFalse(ok);
        Assert.AreEqual(0, svc.GetCurrentData("tile").level);
        Assert.AreEqual(50, money.Current);
        Assert.AreEqual(0, expense.Entries.Count);
    }

    [Test]
    public void TryUpgrade_FailureAtMaxLevel()
    {
        var svc = Build(new[] { Row("tile", 0, 0, 3) }, initialMoney: 1000);
        bool ok = svc.TryUpgrade("tile");
        Assert.IsFalse(ok);
        Assert.AreEqual(1000, money.Current);
    }

    [Test]
    public void StatePersists_AcrossNewServiceInstance()
    {
        // 저장된 상태가 다음 세션에서 재구축된 Service에 전파되는지 검증.
        var svc1 = Build(new[] { Row("tile", 0, 0, 3), Row("tile", 1, 100, 4), Row("tile", 2, 200, 5) }, initialMoney: 1000);
        svc1.TryUpgrade("tile");
        svc1.TryUpgrade("tile");

        var svc2 = new FarmUpgradeService(
            state,
            new[] { Row("tile", 0, 0, 3), Row("tile", 1, 100, 4), Row("tile", 2, 200, 5) },
            new FakeMoneyService { Current = 0 },
            new FakeExpenseLog()
        );

        Assert.AreEqual(2, svc2.GetCurrentData("tile").level);
    }

    [Test]
    public void GetAllTypes_ReturnsUniqueTypes()
    {
        var svc = Build(new[] {
            Row("tile", 0, 0, 3),
            Row("tile", 1, 100, 4),
            Row("harvestCount", 0, 0, 5),
        });

        var types = new List<string>(svc.GetAllTypes());
        Assert.Contains("tile", types);
        Assert.Contains("harvestCount", types);
        Assert.AreEqual(2, types.Count);
    }
}
