using System.Collections.Generic;
using Game.Domain.Shop;
using Game.Schema.State.Shop;
using NUnit.Framework;

public class ToolUpgradeServiceTest
{
    private ShopPersistent state;
    private FakeMoneyService money;
    private FakeExpenseLog expense;

    private static ToolUpgradeData Row(string toolId, int level, int cost, int staminaCost, float durationMultiplier)
    {
        return new ToolUpgradeData
        {
            toolId = toolId, level = level, cost = cost,
            staminaCost = staminaCost, durationMultiplier = durationMultiplier
        };
    }

    private ToolUpgradeService Build(IEnumerable<ToolUpgradeData> rows, int initialMoney = 1000)
    {
        state = new ShopPersistent();
        money = new FakeMoneyService { Current = initialMoney };
        expense = new FakeExpenseLog();
        return new ToolUpgradeService(state, rows, money, expense);
    }

    [Test]
    public void TryUpgrade_Success_ReducesStamina()
    {
        var svc = Build(new[] {
            Row("T001", 0, 0, 10, 1.0f),
            Row("T001", 1, 300, 8, 0.8f),
        }, initialMoney: 500);

        bool ok = svc.TryUpgrade("T001");

        Assert.IsTrue(ok);
        Assert.AreEqual(8, svc.GetCurrentData("T001").staminaCost);
        Assert.AreEqual(0.8f, svc.GetCurrentData("T001").durationMultiplier);
        Assert.AreEqual(200, money.Current);
    }

    [Test]
    public void GetAllToolIds_ReturnsRegisteredTools()
    {
        var svc = Build(new[] {
            Row("T001", 0, 0, 10, 1.0f),
            Row("T002", 0, 0, 10, 1.0f),
            Row("T005", 0, 0, 10, 1.0f),
        });

        var ids = new List<string>(svc.GetAllToolIds());
        Assert.AreEqual(3, ids.Count);
        Assert.Contains("T001", ids);
        Assert.Contains("T005", ids);
    }

    [Test]
    public void UnknownToolId_ReturnsNull()
    {
        var svc = Build(new[] { Row("T001", 0, 0, 10, 1.0f) });
        Assert.IsNull(svc.GetCurrentData("T999"));
        Assert.IsNull(svc.GetNextData("T999"));
    }
}
