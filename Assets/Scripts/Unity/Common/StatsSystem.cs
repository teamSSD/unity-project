using System;
using UnityEngine;

/// <summary>
/// 글로벌 통계(시간/날짜/스태미너/돈) 접근 facade.
/// 상태는 GameSessionRoot.State.stats(BasicStats POCO)에 보관 — 이 클래스는 무상태 어댑터.
/// 이벤트 발행 + Unity 사이드이펙트(SFX) 만 담당.
/// </summary>
public class StatsSystem : SingletonMonoBehaviour<StatsSystem>
{
    public event Action<int, int> OnTimeChanged;
    public event Action<int> OnDayChanged;
    public event Action<int> OnStaminaChanged;
    public event Action<int> OnMoneyChanged;
    public event Action OnStaminaExhausted;

    [Header("SFX")]
    [SerializeField] private AudioClip cashDrawerSfx;

    private static BasicStats Stats =>
        GameSessionRoot.Instance != null ? GameSessionRoot.Instance.State.stats : null;

    public void Initialize()
    {
        if (GameSessionRoot.Instance != null)
            GameSessionRoot.Instance.State.stats = new BasicStats();
    }

    public BasicStats GetSaveData() => Stats;

    public void ApplySaveData(BasicStats data)
    {
        if (GameSessionRoot.Instance == null) return;
        GameSessionRoot.Instance.State.stats = data;
    }

    // ── Day ──
    public int GetDay() => Stats?.day ?? 0;

    public void AddDay(int value)
    {
        if (Stats == null) return;
        Stats.day += value;
        OnDayChanged?.Invoke(Stats.day);
    }

    // ── Time ──
    public int GetHour() => (Stats?.time ?? 0) / 60;
    public int GetMinute() => (Stats?.time ?? 0) % 60;

    public void AddTime(int hour, int minute)
    {
        if (Stats == null) return;
        Stats.time += hour * 60 + minute;

        if (Stats.time >= 1440)
        {
            Stats.time %= 1440;
            AddDay(1);
        }

        BroadcastTime();
    }

    public void SetTime(int hour, int minute)
    {
        if (Stats == null) return;
        Stats.time = Mathf.Clamp(hour * 60 + minute, 0, 1439);
        BroadcastTime();
    }

    private void BroadcastTime()
    {
        OnTimeChanged?.Invoke(GetHour(), GetMinute());
    }

    // ── Stamina ──
    public int GetStamina() => Stats?.stamina ?? 0;

    public void SetStamina(int value)
    {
        if (Stats == null) return;
        Stats.stamina = value;
        OnStaminaChanged?.Invoke(Stats.stamina);
        if (value <= 0)
            OnStaminaExhausted?.Invoke();
    }

    public void SubStamina(int value) => SetStamina((Stats?.stamina ?? 0) - value);

    // ── Money ──
    public int GetMoney() => Stats?.money ?? 0;

    public void SetMoney(int value)
    {
        if (Stats == null) return;
        Stats.money = Mathf.Max(0, value);
        OnMoneyChanged?.Invoke(Stats.money);
    }

    public void AddMoney(int value)
    {
        SetMoney((Stats?.money ?? 0) + value);
        SoundManager.Instance?.Play2DSFX(cashDrawerSfx);
    }

    public void SubMoney(int value)
    {
        SetMoney((Stats?.money ?? 0) - value);
        SoundManager.Instance?.Play2DSFX(cashDrawerSfx);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
