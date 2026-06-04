using System;
using UnityEngine;

/// <summary>
/// 게임 진행(Day/Phase) POCO Service. ProgressSystem facade 폐기 후 직접 사용.
/// 상태: PhaseData (GameState.phase) + cumulativePhaseIndex.
/// 이벤트: OnPhaseChanged.
/// Side-effect (Scene, Weather, Save 등) 많아서 Game.Unity에 위치 — Game.Domain 두면 인터페이스 폭증.
/// TimePhaseProvider 구현하여 TimeManager가 phase index 조회 가능.
/// </summary>
public class ProgressService : TimePhaseProvider
{
    public event Action<PhaseType> OnPhaseChanged;

    private readonly Func<PhaseData> _phaseAccessor;
    private int _cumulativePhaseIndex;

    public ProgressService(Func<PhaseData> phaseAccessor)
    {
        _phaseAccessor = phaseAccessor;
    }

    public PhaseData PhaseData => _phaseAccessor?.Invoke();
    public int CurrentPhaseIndex => _cumulativePhaseIndex;
    public int TotalPhaseCount => 5;
    void TimePhaseProvider.NextPhase() => PassPhase();

    public int PhaseStartMinutes => PhaseData != null ? PhaseToStartMinutes(PhaseData.Phase) : 0;
    public int PhaseEndMinutes   => PhaseData != null ? PhaseToEndMinutes(PhaseData.Phase) : 1440;

    public void Initialize()
    {
        var session = GameSessionRoot.Instance;
        if (session == null) return;
        session.State.phase = new PhaseData();
        _cumulativePhaseIndex = 0;
        WeatherSystem.Instance?.UpdateWeather(0);
    }

    /// <summary>
    /// 디스크 로드된 PhaseData를 GameState.phase로 교체 + 파생 상태 동기화.
    /// </summary>
    public void ApplySaveData(PhaseData data)
    {
        var session = GameSessionRoot.Instance;
        if (session == null || data == null) return;
        session.State.phase = data;
        _cumulativePhaseIndex = data.Day * TotalPhaseCount + (int)data.Phase;
        WeatherSystem.Instance?.UpdateWeather(data.Day);
        OnPhaseChanged?.Invoke(data.Phase);
    }

    /// <summary>Night 종료 시 Settlement 씬 로드 후 true 반환.</summary>
    public bool PassPhase()
    {
        var pd = PhaseData;
        if (pd == null) return false;
        if (pd.Phase == PhaseType.Night)
        {
            SceneLoader.LoadScene(SceneNames.Settlement);
            return true;
        }
        pd.Phase++;
        _cumulativePhaseIndex++;
        SetPhaseTime(pd.Phase);
        OnPhaseChanged?.Invoke(pd.Phase);
        return false;
    }

    public void PassDay()
    {
        var pd = PhaseData;
        if (pd == null) return;
        var stats = GameSessionRoot.Instance?.Stats;
        stats?.SubMoney(SettlementManager.ManagementFee);

        pd.Day++;
        pd.Phase = PhaseType.Preparation;
        _cumulativePhaseIndex++;
        SetPhaseTime(pd.Phase);
        OnPhaseChanged?.Invoke(pd.Phase);

        stats?.SetStamina(100);
        GameRandom.InitDay(pd.Day);
        GameSessionRoot.Instance?.Inventory?.AdvanceDay();
        WeatherSystem.Instance?.UpdateWeather(pd.Day);
        SettlementManager.Instance?.Reset();
        SaveManager.SaveAll();
    }

    public void Die()
    {
        GameSessionRoot.Instance?.Stats?.SetStamina(0);
        PassDay();
    }

    private static void SetPhaseTime(PhaseType phase)
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats == null) return;
        switch (phase)
        {
            case PhaseType.Preparation: stats.SetTime(5, 0);  break;
            case PhaseType.Morning:     stats.SetTime(7, 0);  break;
            case PhaseType.Afternoon:   stats.SetTime(12, 0); break;
            case PhaseType.Evening:     stats.SetTime(17, 0); break;
            case PhaseType.Night:       stats.SetTime(22, 0); break;
        }
    }

    private static int PhaseToStartMinutes(PhaseType p) => p switch
    {
        PhaseType.Preparation => 300,
        PhaseType.Morning     => 420,
        PhaseType.Afternoon   => 720,
        PhaseType.Evening     => 1020,
        PhaseType.Night       => 1320,
        _                     => 0
    };

    private static int PhaseToEndMinutes(PhaseType p) => p switch
    {
        PhaseType.Preparation => 420,
        PhaseType.Morning     => 720,
        PhaseType.Afternoon   => 1020,
        PhaseType.Evening     => 1320,
        PhaseType.Night       => 1740,
        _                     => 1440
    };
}
