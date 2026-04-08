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
        Debug.Log("[ProgressSystem] Initialized");
    }

    public void ApplySaveData(PhaseData data)
    {
        phaseData = data;
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
            case PhaseType.Preparation: StatsSystem.SetTime(5, 0);  break;
            case PhaseType.Morning:     StatsSystem.SetTime(7, 0);  break;
            case PhaseType.Afternoon:   StatsSystem.SetTime(12, 0); break;
            case PhaseType.Evening:     StatsSystem.SetTime(17, 0); break;
            case PhaseType.Night:       StatsSystem.SetTime(22, 0); break;
        }
    }

    public void PassDay()
    {
        phaseData.Day++;
        phaseData.Phase = PhaseType.Preparation;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);

        InventoryManager.Instance?.AdvanceDay();
        SaveManager.SaveAll();

        Debug.Log($"[ProgressSystem] PassDay - Day {phaseData.Day} 시작");
    }

    public void Die()
    {
        StatsSystem.SetStamina(0);
        PassDay();
    }
    
}