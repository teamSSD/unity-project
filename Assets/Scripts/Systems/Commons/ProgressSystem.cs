using System.IO;
using UnityEngine;

public class ProgressSystem : MonoBehaviour
{
    public static ProgressSystem instance {get; private set;}
    public PhaseData phaseData{get; private set;}

    private readonly string path = "saves/progress";

    public bool IsLoadable()
    {
        return DataSaveUtil.HasFile<PhaseData>(path);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        if (phaseData == null)
        {
            phaseData = new PhaseData();
        }
        DataSaveUtil.LoadData(phaseData, path);

        // UnlockedFoodManager 초기화 (해금 데이터 로드)
        if (UnlockedFoodManager.Instance != null)
        {
            UnlockedFoodManager.Instance.LoadUnlocksFromProgress();
        }
        else
        {
            Debug.LogWarning("[ProgressSystem] UnlockedFoodManager not found during initialization");
        }

        // 여기서 딱 아침으로 초기화하면 될듯
    }

    public event System.Action<PhaseType> OnPhaseChanged;

    public void PassPhase() // 시간 처리 누락
    {
        if (phaseData.Phase == PhaseType.Night)
        {
            PassDay();
            return;
        }
        phaseData.Phase++;
        OnPhaseChanged?.Invoke(phaseData.Phase);
    }

    public void PassDay() // 시간 처리 누락
    {
        phaseData.Day++;
        phaseData.Phase = PhaseType.Preparation;
        OnPhaseChanged?.Invoke(phaseData.Phase);

        // 하루 단위로 저장
        flush();
        StatsSystem.flush();
        InventoryManager.Instance?.flush();
    }

    public void Die()
    {
        StatsSystem.SetStamina(0);
        PassDay();
    }
    
    public void flush()
    {
        DataSaveUtil.SaveData(phaseData, path);
    }
}