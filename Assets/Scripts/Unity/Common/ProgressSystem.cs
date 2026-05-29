using UnityEngine;

public class ProgressSystem : SingletonMonoBehaviour<ProgressSystem>, TimePhaseProvider
{
    // 상태는 GameSessionRoot.State.phase에 보관. 이 facade는 backward-compat 접근만 제공.
    public PhaseData phaseData =>
        GameSessionRoot.Instance != null ? GameSessionRoot.Instance.State.phase : null;

    private int cumulativePhaseIndex;
    public int CurrentPhaseIndex => cumulativePhaseIndex;
    public int TotalPhaseCount => 5; // Preparation ~ Night

    public void Initialize()
    {
        if (GameSessionRoot.Instance != null)
            GameSessionRoot.Instance.State.phase = new PhaseData();
        cumulativePhaseIndex = 0;
        WeatherSystem.Instance?.UpdateWeather(phaseData?.Day ?? 0);
        Debug.Log("[ProgressSystem] Initialized");
    }

    public void ApplySaveData(PhaseData data)
    {
        if (GameSessionRoot.Instance != null)
            GameSessionRoot.Instance.State.phase = data;
        cumulativePhaseIndex = data.Day * TotalPhaseCount + (int)data.Phase;
        WeatherSystem.Instance?.UpdateWeather(data.Day);
    }

    public event System.Action<PhaseType> OnPhaseChanged;

    // TimePhaseProvider.NextPhase — PassPhase 위임
    void TimePhaseProvider.NextPhase() => PassPhase();

    // Night 종료 시 Settlement 씬 로드 후 true 반환 (호출자가 추가 씬 전환 생략)
    public bool PassPhase()
    {
        if (phaseData.Phase == PhaseType.Night)
        {
            SceneLoader.LoadScene(SceneNames.Settlement);
            return true;
        }
        phaseData.Phase++;
        cumulativePhaseIndex++;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);
        return false;
    }

    private void SetPhaseTime(PhaseType phase)
    {
        switch (phase)
        {
            case PhaseType.Preparation: StatsSystem.Instance.SetTime(5, 0);  break;
            case PhaseType.Morning:     StatsSystem.Instance.SetTime(7, 0);  break;
            case PhaseType.Afternoon:   StatsSystem.Instance.SetTime(12, 0); break;
            case PhaseType.Evening:     StatsSystem.Instance.SetTime(17, 0); break;
            case PhaseType.Night:       StatsSystem.Instance.SetTime(22, 0); break;
        }
    }

    public void PassDay()
    {
        StatsSystem.Instance.SubMoney(SettlementManager.ManagementFee);

        phaseData.Day++;
        phaseData.Phase = PhaseType.Preparation;
        cumulativePhaseIndex++;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);

        StatsSystem.Instance.SetStamina(100);
        GameRandom.InitDay(phaseData.Day);
        InventoryManager.Instance?.AdvanceDay();
        WeatherSystem.Instance?.UpdateWeather(phaseData.Day);
        SettlementManager.Instance?.Reset();
        SaveManager.SaveAll();

        Debug.Log($"[ProgressSystem] PassDay - Day {phaseData.Day} 시작");
    }

    public void Die()
    {
        StatsSystem.Instance.SetStamina(0);
        PassDay();
    }

    public int PhaseStartMinutes => phaseData != null ? PhaseToStartMinutes(phaseData.Phase) : 0;
    public int PhaseEndMinutes   => phaseData != null ? PhaseToEndMinutes(phaseData.Phase) : 1440;

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