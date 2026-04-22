using System.Collections.Generic;
using UnityEngine;

public class StorageUpgradeManager : MonoBehaviour
{
    public static StorageUpgradeManager Instance { get; private set; }

    private List<StorageUpgradeData> upgradeTable;
    private int storageLevel = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadTable();
    }

    void LoadTable()
    {
        upgradeTable = CsvModelConverter.Parse<StorageUpgradeData>(ResourcePaths.DataTable.StorageUpgrade);
    }

    public StorageUpgradeData GetCurrentData()
    {
        return upgradeTable?.Find(d => d.level == storageLevel);
    }

    public StorageUpgradeData GetNextData()
    {
        return upgradeTable?.Find(d => d.level == storageLevel + 1);
    }

    public bool IsMax() => GetNextData() == null;

    public bool TryUpgrade()
    {
        var next = GetNextData();
        if (next == null) return false;

        if (StatsSystem.Instance.GetMoney() < next.cost)
        {
            Debug.Log($"[StorageUpgrade] 골드 부족 ({next.cost}G 필요)");
            return false;
        }

        StatsSystem.Instance.SubMoney(next.cost);
        SettlementManager.Instance?.AddExpense("업그레이드", next.cost);
        storageLevel = next.level;
        Debug.Log($"[StorageUpgrade] 창고 → Lv.{storageLevel} 업그레이드 완료");
        return true;
    }

    // --- 저장/로드 ---
    public StorageUpgradeSaveData GetSaveData() => new StorageUpgradeSaveData { level = storageLevel };

    public void ApplySaveData(StorageUpgradeSaveData data) => storageLevel = data.level;
}

[System.Serializable]
public class StorageUpgradeSaveData
{
    public int level = 0;
}
