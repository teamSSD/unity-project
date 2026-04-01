using System.IO;
using UnityEngine;

public class ProgressSystem : SingletonMonoBehaviour<ProgressSystem>
{
    public PhaseData phaseData{get; private set;}

    public bool IsLoadable()
    {
        string path = Application.persistentDataPath + "/saves/progress";
        return DataSaveUtil.HasFile<PhaseData>(path);
    }

    public void Initialize()
    {
        if (phaseData == null)
        {
            phaseData = new PhaseData();
        }
        string path = Application.persistentDataPath + "/saves/progress";
        phaseData = DataSaveUtil.LoadData(phaseData, path);

        Debug.Log("[ProgressSystem] Initialized");
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

    /// <summary>
    /// 하루가 지날 때 호출됩니다.
    ///
    /// ⚠️ 경고: 이 함수에서만 모든 게임 데이터를 저장합니다!
    /// 다른 곳에서 flush()를 호출하지 마세요!
    ///
    /// 저장되는 데이터:
    /// - ProgressSystem (Day, Phase, UnlockedRecipes, SelectedMenus)
    /// - StatsSystem (stamina, day, time, money)
    /// - InventoryManager (inventory)
    /// - RecipeDataManager (menus)
    /// - DeliveryNpcDialogueInteraction (quest stages)
    /// - OrderManager (delivery orders)
    /// </summary>
    public void PassDay()
    {
        phaseData.Day++;
        phaseData.Phase = PhaseType.Preparation;
        SetPhaseTime(phaseData.Phase);
        OnPhaseChanged?.Invoke(phaseData.Phase);

        // ⚠️ 모든 게임 데이터 저장 (New Game 시작 시와 여기서만 저장!)
        flush();
        StatsSystem.flush();
        InventoryManager.Instance?.flush();
        RecipeDataManager.Instance?.flush();
        DeliveryNpcDialogueInteraction.flush();
        OrderManager.Instance?.flush();

        Debug.Log($"[ProgressSystem] PassDay - Day {phaseData.Day} 시작, 모든 데이터 저장 완료");
    }

    public void Die()
    {
        StatsSystem.SetStamina(0);
        PassDay();
    }
    
    public void flush()
    {
        string path = Application.persistentDataPath + "/saves/progress";
        DataSaveUtil.SaveData(phaseData, path);
    }
}