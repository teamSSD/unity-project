using System;
using UnityEngine;

public class StatsSystem : SingletonMonoBehaviour<StatsSystem>
{
    public event Action<int, int> OnTimeChanged;
    public event Action<int> OnDayChanged;
    public event Action<int> OnStaminaChanged;
    public event Action<int> OnMoneyChanged;
    public event Action OnStaminaExhausted;

    private BasicStats basicStats = new BasicStats();

    public void Initialize()
    {
        basicStats = new BasicStats();
        Debug.Log("[StatsSystem] Initialized");
    }

    public BasicStats GetSaveData() => basicStats;

    public void ApplySaveData(BasicStats data)
    {
        basicStats = data;
        Debug.Log($"[StatsSystem] Save data applied - Stamina: {basicStats.stamina}, Money: {basicStats.money}, Day: {basicStats.day}, Time: {basicStats.time}");
    }

    // ── Day ──
    public int GetDay() => basicStats.day;

    public void AddDay(int value)
    {
        basicStats.day += value;
        OnDayChanged?.Invoke(basicStats.day);
    }

    // ── Time ──
    public int GetHour() => basicStats.time / 60;
    public int GetMinute() => basicStats.time % 60;

    public void AddTime(int hour, int minute)
    {
        basicStats.time += hour * 60 + minute;

        if (basicStats.time >= 1440)
        {
            basicStats.time %= 1440;
            AddDay(1);
        }

        BroadcastTime();
    }

    public void SetTime(int hour, int minute)
    {
        basicStats.time = Mathf.Clamp(hour * 60 + minute, 0, 1439);
        BroadcastTime();
    }

    private void BroadcastTime()
    {
        OnTimeChanged?.Invoke(GetHour(), GetMinute());
    }

    // ── Stamina ──
    public int GetStamina() => basicStats.stamina;

    public void SetStamina(int value)
    {
        basicStats.stamina = value;
        OnStaminaChanged?.Invoke(basicStats.stamina);
        if (value <= 0)
            OnStaminaExhausted?.Invoke();
    }

    public void SubStamina(int value) => SetStamina(basicStats.stamina - value);

    // ── Money ──
    public int GetMoney() => basicStats.money;

    public void SetMoney(int value)
    {
        basicStats.money = Mathf.Max(0, value);
        OnMoneyChanged?.Invoke(basicStats.money);
    }

    public void AddMoney(int value) => SetMoney(basicStats.money + value);
    public void SubMoney(int value) => SetMoney(basicStats.money - value);
}
