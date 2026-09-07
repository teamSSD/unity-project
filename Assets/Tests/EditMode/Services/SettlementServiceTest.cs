using Game.Domain.Mall;
using Game.Schema.State.Mall;
using NUnit.Framework;

public class SettlementServiceTest
{
    [Test]
    public void ServicesUsingSameState_ObserveTheSameSettlement()
    {
        var state = new MallSessionState();
        var writer = new SettlementService(state);
        var reader = new SettlementService(state);

        writer.AddIncome("배달", 1200);
        writer.AddExpense("구매", 300);

        Assert.AreEqual(1200, reader.TotalIncome());
        Assert.AreEqual(300, reader.TotalExpense());
    }

    [Test]
    public void Reset_UpdatesSnapshotAndClearsSharedEntries()
    {
        var state = new MallSessionState();
        var settlement = new SettlementService(state);
        settlement.AddIncome("배달", 1200);
        settlement.AddExpense("구매", 300);

        settlement.Reset(9000);

        Assert.AreEqual(9000, settlement.DayStartMoney);
        Assert.AreEqual(0, settlement.TotalIncome());
        Assert.AreEqual(0, settlement.TotalExpense());
    }
}
