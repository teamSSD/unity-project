using Game.Domain.Cooking;
using NUnit.Framework;

public class CustomerSessionStatsTest
{
    [Test]
    public void Initial_AllZero()
    {
        var stats = new CustomerSessionStats();
        Assert.AreEqual(0, stats.TotalOrders);
        Assert.AreEqual(0, stats.PerfectOrders);
        Assert.AreEqual(0f, stats.TotalAccuracyScore);
        Assert.AreEqual(0, stats.TotalEarnings);
        Assert.AreEqual(0f, stats.AverageAccuracy);
        Assert.AreEqual(0f, stats.PerfectRatePercent);
    }

    [Test]
    public void RecordOrderServed_AccumulatesOrderAndScore()
    {
        var stats = new CustomerSessionStats();
        stats.RecordOrderServed(0.8f, 1000);
        Assert.AreEqual(1, stats.TotalOrders);
        Assert.AreEqual(0.8f, stats.TotalAccuracyScore);
        Assert.AreEqual(1000, stats.TotalEarnings);
        Assert.AreEqual(0, stats.PerfectOrders);
    }

    [Test]
    public void RecordOrderServed_PerfectScore_IncrementsPerfectCount()
    {
        var stats = new CustomerSessionStats();
        stats.RecordOrderServed(1.0f, 500);
        Assert.AreEqual(1, stats.PerfectOrders);
        stats.RecordOrderServed(1.2f, 600); // > 1.0도 perfect
        Assert.AreEqual(2, stats.PerfectOrders);
    }

    [Test]
    public void AverageAccuracy_ComputesMean()
    {
        var stats = new CustomerSessionStats();
        stats.RecordOrderServed(0.6f, 0);
        stats.RecordOrderServed(0.8f, 0);
        stats.RecordOrderServed(1.0f, 0);
        Assert.AreEqual(0.8f, stats.AverageAccuracy, 0.0001f);
    }

    [Test]
    public void PerfectRatePercent_ComputesProportion()
    {
        var stats = new CustomerSessionStats();
        stats.RecordOrderServed(1.0f, 0);
        stats.RecordOrderServed(0.5f, 0);
        stats.RecordOrderServed(0.5f, 0);
        stats.RecordOrderServed(0.5f, 0);
        Assert.AreEqual(25f, stats.PerfectRatePercent, 0.0001f); // 1/4 = 25%
    }

    [Test]
    public void Snapshot_ReturnsTuple()
    {
        var stats = new CustomerSessionStats();
        stats.RecordOrderServed(1.0f, 100);
        stats.RecordOrderServed(0.5f, 50);
        var snap = stats.Snapshot();
        Assert.AreEqual(2, snap.total);
        Assert.AreEqual(1, snap.perfect);
        Assert.AreEqual(0.75f, snap.avgScore, 0.0001f);
        Assert.AreEqual(150, snap.earnings);
    }
}
