using System.Collections.Generic;
using UnityEngine;

public class FarmUpgradeManager : MonoBehaviour
{
    public static FarmUpgradeManager Instance { get; private set; }

    private Dictionary<string, List<FarmUpgradeData>> upgradeTable;
    private Dictionary<string, int> levels = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadTable();
    }

    void LoadTable()
    {
        upgradeTable = new Dictionary<string, List<FarmUpgradeData>>();
        var rows = CsvModelConverter.Parse<FarmUpgradeData>(CatalogProvider.Csvs?.farmUpgrade);
        foreach (var row in rows)
        {
            if (!upgradeTable.ContainsKey(row.type))
            {
                upgradeTable[row.type] = new List<FarmUpgradeData>();
                levels[row.type] = 0;
            }
            upgradeTable[row.type].Add(row);
        }
    }

    public FarmUpgradeData GetCurrentData(string type)
    {
        if (!upgradeTable.TryGetValue(type, out var list)) return null;
        int lv = levels.TryGetValue(type, out var l) ? l : 0;
        return list.Find(d => d.level == lv);
    }

    public FarmUpgradeData GetNextData(string type)
    {
        if (!upgradeTable.TryGetValue(type, out var list)) return null;
        int lv = levels.TryGetValue(type, out var l) ? l : 0;
        return list.Find(d => d.level == lv + 1);
    }

    public bool IsMax(string type) => GetNextData(type) == null;

    public bool TryUpgrade(string type)
    {
        var next = GetNextData(type);
        if (next == null) return false;

        if (StatsSystem.Instance.GetMoney() < next.cost)
        {
            Debug.Log($"[FarmUpgrade] 골드 부족 ({next.cost}G 필요)");
            return false;
        }

        StatsSystem.Instance.SubMoney(next.cost);
        SettlementManager.Instance?.AddExpense("업그레이드", next.cost);
        levels[type] = next.level;
        Debug.Log($"[FarmUpgrade] {type} → Lv.{next.level} 업그레이드");
        return true;
    }

    public IEnumerable<string> GetAllTypes() => upgradeTable.Keys;

    public FarmUpgradeSaveData GetSaveData()
    {
        var data = new FarmUpgradeSaveData();
        foreach (var kv in levels)
        {
            data.types.Add(kv.Key);
            data.levels.Add(kv.Value);
        }
        return data;
    }

    public void ApplySaveData(FarmUpgradeSaveData data)
    {
        for (int i = 0; i < data.types.Count; i++)
            if (levels.ContainsKey(data.types[i]))
                levels[data.types[i]] = data.levels[i];
    }
}

[System.Serializable]
public class FarmUpgradeSaveData
{
    public List<string> types  = new();
    public List<int>    levels = new();
}
