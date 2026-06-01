using System.Collections.Generic;

/// <summary>
/// Shop 도메인 ↔ GameSaveData 슬롯 변환.
/// ShopPersistent(storage/tool) ↔ StorageUpgradeSaveData + ToolUpgradeSaveData.
/// </summary>
public static class ShopSaveAdapter
{
    public static void Capture(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;
        var sp = GameSessionRoot.Instance.State.shop.persistent;

        save.storageUpgrades = new StorageUpgradeSaveData
        {
            types  = new List<string>(sp.storageTypes),
            levels = new List<int>(sp.storageLevels)
        };
        save.toolUpgrades = new ToolUpgradeSaveData
        {
            ids    = new List<string>(sp.toolIds),
            levels = new List<int>(sp.toolLevels)
        };
    }

    public static void Apply(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;
        var sp = GameSessionRoot.Instance.State.shop.persistent;

        if (save.storageUpgrades != null)
            MergeLevels(sp.storageTypes, sp.storageLevels, save.storageUpgrades.types, save.storageUpgrades.levels);

        if (save.toolUpgrades != null)
            MergeLevels(sp.toolIds, sp.toolLevels, save.toolUpgrades.ids, save.toolUpgrades.levels);
    }

    /// <summary>Service 초기화 시 채워진 key 유지하며 saved value만 덮어쓰기.</summary>
    private static void MergeLevels(List<string> keys, List<int> values, List<string> savedKeys, List<int> savedValues)
    {
        for (int i = 0; i < savedKeys.Count; i++)
        {
            string key = savedKeys[i];
            int value = savedValues[i];
            int idx = keys.IndexOf(key);
            if (idx >= 0) values[idx] = value;
            else { keys.Add(key); values.Add(value); }
        }
    }
}
