using NUnit.Framework;
using UnityEngine;

public class StatsSystemTest
{
    private StatsSystem stats;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("StatsSystem");
        stats = go.AddComponent<StatsSystem>();
        stats.Initialize();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(stats.gameObject);
    }

    // ── Money ──

    [Test]
    public void SetMoney_BroadcastsEvent()
    {
        int received = -1;
        stats.OnMoneyChanged += (v) => received = v;

        stats.SetMoney(500);

        Assert.AreEqual(500, stats.GetMoney());
        Assert.AreEqual(500, received);
    }

    [Test]
    public void AddMoney_IncreasesValue()
    {
        stats.SetMoney(100);
        stats.AddMoney(50);

        Assert.AreEqual(150, stats.GetMoney());
    }

    [Test]
    public void SubMoney_DecreasesValue()
    {
        stats.SetMoney(100);
        stats.SubMoney(30);

        Assert.AreEqual(70, stats.GetMoney());
    }

    [Test]
    public void SetMoney_NeverNegative()
    {
        stats.SetMoney(10);
        stats.SubMoney(100);

        Assert.AreEqual(0, stats.GetMoney());
    }

    // ── Stamina ──

    [Test]
    public void SetStamina_BroadcastsEvent()
    {
        int received = -1;
        stats.OnStaminaChanged += (v) => received = v;

        stats.SetStamina(80);

        Assert.AreEqual(80, stats.GetStamina());
        Assert.AreEqual(80, received);
    }

    [Test]
    public void SetStamina_Zero_FiresExhausted()
    {
        bool exhausted = false;
        stats.OnStaminaExhausted += () => exhausted = true;

        stats.SetStamina(0);

        Assert.IsTrue(exhausted);
    }

    [Test]
    public void SubStamina_ReducesValue()
    {
        stats.SetStamina(100);
        stats.SubStamina(25);

        Assert.AreEqual(75, stats.GetStamina());
    }

    // ── Time ──

    [Test]
    public void SetTime_BroadcastsEvent()
    {
        int h = -1, m = -1;
        stats.OnTimeChanged += (hour, min) => { h = hour; m = min; };

        stats.SetTime(14, 30);

        Assert.AreEqual(14, h);
        Assert.AreEqual(30, m);
        Assert.AreEqual(14, stats.GetHour());
        Assert.AreEqual(30, stats.GetMinute());
    }

    [Test]
    public void SetTime_ClampsTo2359()
    {
        stats.SetTime(25, 0);

        Assert.AreEqual(23, stats.GetHour());
        Assert.AreEqual(59, stats.GetMinute());
    }

    [Test]
    public void AddTime_WrapsDay()
    {
        int dayChanged = 0;
        stats.OnDayChanged += (d) => dayChanged = d;

        stats.SetTime(23, 50);
        stats.AddTime(0, 20); // 23:50 + 20min = 24:10 → wraps

        Assert.AreEqual(0, stats.GetHour());
        Assert.AreEqual(10, stats.GetMinute());
        Assert.AreEqual(1, dayChanged);
    }

    // ── SaveData ──

    [Test]
    public void SaveData_RoundTrips()
    {
        stats.SetMoney(999);
        stats.SetStamina(42);
        stats.SetTime(12, 30);

        var data = stats.GetSaveData();

        var go2 = new GameObject("StatsSystem2");
        var stats2 = go2.AddComponent<StatsSystem>();
        stats2.Initialize();
        stats2.ApplySaveData(data);

        Assert.AreEqual(999, stats2.GetMoney());
        Assert.AreEqual(42, stats2.GetStamina());
        Assert.AreEqual(12, stats2.GetHour());
        Assert.AreEqual(30, stats2.GetMinute());

        Object.DestroyImmediate(go2);
    }
}
