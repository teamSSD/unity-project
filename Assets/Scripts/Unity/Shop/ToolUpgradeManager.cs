using System.Collections.Generic;
using UnityEngine;

public class ToolUpgradeManager : MonoBehaviour
{
    public static ToolUpgradeManager Instance { get; private set; }

    // toolId → 레벨별 데이터 리스트 (level 0부터)
    private Dictionary<string, List<ToolUpgradeData>> upgradeTable;
    // toolId → 현재 레벨
    private Dictionary<string, int> toolLevels = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadTable();
    }

    void LoadTable()
    {
        upgradeTable = new Dictionary<string, List<ToolUpgradeData>>();
        var rows = CsvModelConverter.Parse<ToolUpgradeData>(CatalogProvider.Csvs?.toolUpgrade);
        foreach (var row in rows)
        {
            if (!upgradeTable.ContainsKey(row.toolId))
                upgradeTable[row.toolId] = new List<ToolUpgradeData>();
            upgradeTable[row.toolId].Add(row);
        }
        // toolLevels 초기화 (테이블에 있는 모든 툴 ID 등록)
        foreach (var id in upgradeTable.Keys)
        {
            if (!toolLevels.ContainsKey(id))
                toolLevels[id] = 0;
        }
    }

    public ToolUpgradeData GetCurrentData(string toolId)
    {
        if (!upgradeTable.TryGetValue(toolId, out var list)) return null;
        int level = toolLevels.TryGetValue(toolId, out var lv) ? lv : 0;
        return list.Find(d => d.level == level);
    }

    public ToolUpgradeData GetNextData(string toolId)
    {
        if (!upgradeTable.TryGetValue(toolId, out var list)) return null;
        int nextLevel = (toolLevels.TryGetValue(toolId, out var lv) ? lv : 0) + 1;
        return list.Find(d => d.level == nextLevel);
    }

    public bool IsMax(string toolId)
    {
        return GetNextData(toolId) == null;
    }

    public bool TryUpgrade(string toolId)
    {
        var next = GetNextData(toolId);
        if (next == null) return false;

        if (StatsSystem.Instance.GetMoney() < next.cost)
        {
            Debug.Log($"[ToolUpgrade] 골드 부족 ({next.cost}G 필요)");
            return false;
        }

        StatsSystem.Instance.SubMoney(next.cost);
        SettlementManager.Instance?.AddExpense("업그레이드", next.cost);
        toolLevels[toolId] = next.level;
        Debug.Log($"[ToolUpgrade] {toolId} → Lv.{next.level} 업그레이드 완료");
        return true;
    }

    public IEnumerable<string> GetAllToolIds() => upgradeTable.Keys;

    // --- 저장/로드 ---
    public ToolUpgradeSaveData GetSaveData()
    {
        var data = new ToolUpgradeSaveData();
        foreach (var kv in toolLevels)
        {
            data.ids.Add(kv.Key);
            data.levels.Add(kv.Value);
        }
        return data;
    }

    public void ApplySaveData(ToolUpgradeSaveData data)
    {
        for (int i = 0; i < data.ids.Count; i++)
            toolLevels[data.ids[i]] = data.levels[i];
    }
}

[System.Serializable]
public class ToolUpgradeSaveData
{
    public List<string> ids = new();
    public List<int> levels = new();
}
