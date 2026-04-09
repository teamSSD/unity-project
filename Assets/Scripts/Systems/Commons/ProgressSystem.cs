using UnityEngine;

public class ProgressSystem : SingletonMonoBehaviour<ProgressSystem>
{
    public PhaseData phaseData{get; private set;}

    public void Initialize()
    {
        if (phaseData == null)
        {
            phaseData = new PhaseData();
        }
        WeatherSystem.Instance?.UpdateWeather(phaseData.Day);
        Debug.Log("[ProgressSystem] Initialized");
    }

    public void ApplySaveData(PhaseData data)
    {
        phaseData = data;
        WeatherSystem.Instance?.UpdateWeather(phaseData.Day);
    }

    public event System.Action<PhaseType> OnPhaseChanged;

    public void PassPhase()
    {
        if (phaseData.Phase == PhaseType.Night)
        {
            PassDay();
            return;
        }
        phaseData.Phase++;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);
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
        phaseData.Day++;
        phaseData.Phase = PhaseType.Preparation;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);

        InventoryManager.Instance?.AdvanceDay();
        WeatherSystem.Instance?.UpdateWeather(phaseData.Day);
        SaveManager.SaveAll();

        Debug.Log($"[ProgressSystem] PassDay - Day {phaseData.Day} 시작");
    }

    public void Die()
    {
        StatsSystem.Instance.SetStamina(0);
        PassDay();
    }
    
}