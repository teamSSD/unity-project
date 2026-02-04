using System;
using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    public static event Action<int,int> OnTimeChanged;
    public static event Action<int> OnDayChanged;
    public static event Action<int> OnStaminaChanged;
    public static event Action<int> OnMoneyChanged;
    public static event Action OnStaminaExhausted;
    public static event Action OnTimePaused;
    public static event Action OnTimeResumed;

    private static bool isTimePaused = false;
    private static Action breakAction;
    private static int breakTargetTime = -1;
    private static bool initialized = false;
    
    private static BasicStats basicStats = new BasicStats();

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        DataSaveUtil.LoadData(basicStats, "stats/basic");

        isTimePaused = false;
        breakAction = null;
        breakTargetTime = -1;
    }
    public static void ResetEvents()
    {
        OnTimeChanged = null;
        OnDayChanged = null;
        OnStaminaChanged = null;
        OnMoneyChanged = null;
        OnStaminaExhausted = null;
        OnTimePaused = null;
        OnTimeResumed = null;

        isTimePaused = false;
        breakAction = null;
        breakTargetTime = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void OnDomainReload()
    {
        StatsSystem.ResetEvents();
    }

    public static int GetDay() => basicStats.day;

    public static void AddDay(int value)
    {
        basicStats.day += value;
        OnDayChanged?.Invoke(basicStats.day);
    }
    public static int GetHour() => basicStats.time / 60;
    public static int GetMinute() => basicStats.time % 60;
    public static void PauseTime()
    {
        if (!isTimePaused)
        {
            isTimePaused = true;
            OnTimePaused?.Invoke();
        }
    }

    public static void ResumeTime()
    {
        if (isTimePaused)
        {
            isTimePaused = false;
            OnTimeResumed?.Invoke();
        }
    }

    public static bool IsTimePaused() => isTimePaused;

    // Break Point 등록
    public static void RegisterBreakPoint(int hour, int minute, Action action)
    {
        breakTargetTime = hour * 60 + minute;
        breakAction = action;
        OnTimeChanged += HandleBreakPoint;
    }

    private static void HandleBreakPoint(int hour, int minute)
    {
        int now = hour * 60 + minute;

        if (now == breakTargetTime)
        {
            PauseTime();
            breakAction?.Invoke();
            OnTimeChanged -= HandleBreakPoint;
        }
    }

    // Time change
    public static void AddTime(int hour, int minute)
    {
        if (isTimePaused) return;

        basicStats.time += hour * 60 + minute;

        if (basicStats.time >= 1440)
        {
            basicStats.time %= 1440;
            AddDay(1);
        }

        BroadcastTime();
    }

    public static void SetTime(int hour, int minute)
    {
        basicStats.time = Mathf.Clamp(hour * 60 + minute, 0, 1439);
        BroadcastTime();
    }

    private static void BroadcastTime()
    {
        OnTimeChanged?.Invoke(GetHour(), GetMinute());
    }
    public static int GetStamina() { return basicStats.stamina; }
    public static void SetStamina(int value)
    {
        basicStats.stamina = value;
        OnStaminaChanged?.Invoke(basicStats.stamina);
        if (value <= 0)
        {
            OnStaminaExhausted?.Invoke();
        }
    }
    public static void SubStamina(int value) { SetStamina(basicStats.stamina - value); }
    public static int GetMoney() => basicStats.money;
    public static void SetMoney(int value)
    {
        basicStats.money = Mathf.Max(0, value);
        OnMoneyChanged?.Invoke(basicStats.money);
    }
    public static void AddMoney(int value) { SetMoney(basicStats.money + value); }
    public static void SubMoney(int value) { SetMoney(basicStats.money - value); }

    public static void flush()
    {
        DataSaveUtil.SaveData(basicStats, "stats/basic");
    }
}