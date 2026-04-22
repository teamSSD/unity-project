using System.Collections.Generic;
using UnityEngine;

public class FarmUpgradeManager : MonoBehaviour
{
    public static FarmUpgradeManager Instance { get; private set; }

    private List<FarmUpgradeData> upgradeTable;
    private int farmLevel = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadTable();
    }

    void LoadTable() => upgradeTable = CsvModelConverter.Parse<FarmUpgradeData>(ResourcePaths.DataTable.FarmUpgrade);

    public FarmUpgradeData GetCurrentData() => upgradeTable?.Find(d => d.level == farmLevel);
    public FarmUpgradeData GetNextData()    => upgradeTable?.Find(d => d.level == farmLevel + 1);
    public bool IsMax() => GetNextData() == null;

    public bool TryUpgrade()
    {
        var next = GetNextData();
        if (next == null) return false;

        if (StatsSystem.Instance.GetMoney() < next.cost)
        {
            Debug.Log($"[FarmUpgrade] 골드 부족 ({next.cost}G 필요)");
            return false;
        }

        StatsSystem.Instance.SubMoney(next.cost);
        SettlementManager.Instance?.AddExpense("업그레이드", next.cost);
        farmLevel = next.level;
        Debug.Log($"[FarmUpgrade] 농장 → Lv.{farmLevel} 업그레이드");
        return true;
    }

    public FarmUpgradeSaveData GetSaveData()         => new FarmUpgradeSaveData { level = farmLevel };
    public void ApplySaveData(FarmUpgradeSaveData d) => farmLevel = d.level;
}

[System.Serializable]
public class FarmUpgradeSaveData
{
    public int level = 0;
}
