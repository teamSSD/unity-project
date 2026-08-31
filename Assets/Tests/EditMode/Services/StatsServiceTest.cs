using Game.Domain.Common;
using NUnit.Framework;

public class StatsServiceTest
{
    private BasicStats stats;
    private StatsService svc;

    [SetUp]
    public void Setup()
    {
        stats = new BasicStats();
        svc = new StatsService(() => stats);
    }

    [Test]
    public void Money_SetAddSub()
    {
        svc.SetMoney(1000);
        Assert.AreEqual(1000, svc.GetMoney());
        svc.AddMoney(500);
        Assert.AreEqual(1500, svc.GetMoney());
        svc.SubMoney(300);
        Assert.AreEqual(1200, svc.GetMoney());
    }

    [Test]
    public void Money_NeverGoesNegative()
    {
        svc.SetMoney(100);
        svc.SubMoney(999);
        Assert.AreEqual(0, svc.GetMoney());
    }

    [Test]
    public void Money_FiresOnMoneyChanged()
    {
        int received = -1;
        svc.OnMoneyChanged += v => received = v;
        svc.SetMoney(700);
        Assert.AreEqual(700, received);
    }

    [Test]
    public void Time_SetAndGet()
    {
        svc.SetTime(7, 30);
        Assert.AreEqual(7, svc.GetHour());
        Assert.AreEqual(30, svc.GetMinute());
    }

    [Test]
    public void Time_AddPastDayBoundary_NoWrap()
    {
        // Night 페이즈(22:00~익일 03:00)가 하루 경계를 넘어감. wrap 발생 시
        // TimeManager.CheckBreakPoint(endTime)가 영원히 미도달 → 영업 종료 실패.
        // 따라서 AddTime은 wrap 없이 그대로 누적해야 함.
        svc.SetTime(23, 30);
        svc.AddTime(1, 0); // 24:30
        Assert.AreEqual(24, svc.GetHour());
        Assert.AreEqual(30, svc.GetMinute());
    }

    [Test]
    public void Time_SetPastDayBoundary_Accepted()
    {
        // Night endTime(익일 03:00 = 1620)까지 저장 가능해야 CheckBreakPoint 통과.
        svc.SetTime(27, 0);
        Assert.AreEqual(27, svc.GetHour());
        Assert.AreEqual(0, svc.GetMinute());
    }

    [Test]
    public void Time_ClampedAtUpperBound()
    {
        // 상한(1740=29:00) 초과분은 clamp — GetHour 무한 증가 방지.
        svc.SetTime(30, 0); // 1800 → 1740
        Assert.AreEqual(29, svc.GetHour());
        Assert.AreEqual(0, svc.GetMinute());
    }

    [Test]
    public void Stamina_SetTriggersEvent()
    {
        int newVal = -1;
        svc.OnStaminaChanged += v => newVal = v;
        svc.SetStamina(80);
        Assert.AreEqual(80, newVal);
    }

    [Test]
    public void Stamina_ExhaustedEventFires()
    {
        bool exhausted = false;
        svc.OnStaminaExhausted += () => exhausted = true;
        svc.SetStamina(0);
        Assert.IsTrue(exhausted);
    }

    [Test]
    public void Stamina_NegativeFiresExhausted()
    {
        bool exhausted = false;
        svc.OnStaminaExhausted += () => exhausted = true;
        svc.SetStamina(10);
        svc.SubStamina(20); // -10
        Assert.IsTrue(exhausted);
    }

    [Test]
    public void ApplySaveData_CopiesFields_FiresAllEvents()
    {
        int money = -1, stamina = -1, hour = -1, minute = -1;
        svc.OnMoneyChanged += v => money = v;
        svc.OnStaminaChanged += v => stamina = v;
        svc.OnTimeChanged += (h, m) => { hour = h; minute = m; };

        var loaded = new BasicStats { time = 7 * 60 + 15, stamina = 80, money = 12000 };
        svc.ApplySaveData(loaded);

        Assert.AreEqual(12000, svc.GetMoney());
        Assert.AreEqual(80, svc.GetStamina());
        Assert.AreEqual(7, svc.GetHour());
        Assert.AreEqual(15, svc.GetMinute());

        Assert.AreEqual(12000, money);
        Assert.AreEqual(80, stamina);
        Assert.AreEqual(7, hour);
        Assert.AreEqual(15, minute);
    }

    [Test]
    public void ApplySaveData_PreservesLiveInstance()
    {
        // 클로저가 GameState.stats를 가리키는 점 보존 — 새 인스턴스 할당 X
        var original = stats;
        var loaded = new BasicStats { money = 5000 };
        svc.ApplySaveData(loaded);
        Assert.AreSame(original, stats); // 원본 인스턴스 그대로
    }

    [Test]
    public void Reset_ZeroesAllAndBroadcasts()
    {
        svc.SetMoney(9999);
        svc.SetStamina(50);

        int money = -1, stamina = -1;
        svc.OnMoneyChanged += v => money = v;
        svc.OnStaminaChanged += v => stamina = v;

        svc.Reset();
        Assert.AreEqual(0, svc.GetMoney());
        Assert.AreEqual(0, svc.GetStamina());
        Assert.AreEqual(0, money);
        Assert.AreEqual(0, stamina);
    }
}
