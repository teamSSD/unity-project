using System.Collections.Generic;

[System.Serializable]
public class InventoryBatchEntry
{
    public int quantity;
    public int daysRemaining;
}

[System.Serializable]
public class InventoryItemEntry
{
    public string foodId;
    public List<InventoryBatchEntry> batches = new List<InventoryBatchEntry>();
}

[System.Serializable]
public class InventorySaveData
{
    // 레거시 (하위호환)
    public List<string> foodIds = new List<string>();
    public List<int> amounts = new List<int>();

    // 신규 배치 데이터
    public List<InventoryItemEntry> items = new List<InventoryItemEntry>();
}
