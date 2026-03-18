using System.Collections.Generic;

[System.Serializable]
public class InventorySaveData
{
    public List<string> foodIds;
    public List<int> amounts;

    public InventorySaveData()
    {
        foodIds = new List<string>();
        amounts = new List<int>();
    }
}
