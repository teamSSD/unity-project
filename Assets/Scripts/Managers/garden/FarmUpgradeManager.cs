using UnityEngine;

public class FarmUpgradeManager : MonoBehaviour
{
    public static FarmUpgradeManager Instance { get; private set; }

    [Header("Upgrade Cost")]
    public int[] upgradeCosts = { 0, 10000, 25000, 70000, 120000 };

    [Header("Tile Number Upgrade")]
    public int currentTileLevel = 0;
    public int[] tileCountValues = { 3, 4, 5, 6, 7 };

    [Header("Harvest Time Upgrade")]
    public int currentTimeLevel = 0;
    public float[] timeReductionValues = { 0f, 0.05f, 0.10f, 0.15f, 0.20f };

    [Header("Crop Amount Upgrade")]
    public int currentHarvestLevel = 0;
    public int[] harvestCountValues = { 5, 7, 9, 11, 13 };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetCurrentTileCount() => tileCountValues[currentTileLevel];
    public float GetCurrentTimeReduction() => timeReductionValues[currentTimeLevel];
    public int GetCurrentHarvestCount() => harvestCountValues[currentHarvestLevel];

    public int GetCurrentLevel(FarmUpgradeType type)
    {
        switch (type)
        {
            case FarmUpgradeType.TileCount: return currentTileLevel;
            case FarmUpgradeType.TimeReduction: return currentTimeLevel;
            case FarmUpgradeType.HarvestCount: return currentHarvestLevel;
            default: return 0;
        }
    }

    public string GetNextUpgradeValueText(FarmUpgradeType type)
    {
        switch (type)
        {
            case FarmUpgradeType.TileCount:
                if (currentTileLevel >= tileCountValues.Length - 1) return "MAX";
                return tileCountValues[currentTileLevel + 1].ToString();

            case FarmUpgradeType.TimeReduction:
                if (currentTimeLevel >= timeReductionValues.Length - 1) return "MAX";
                return (timeReductionValues[currentTimeLevel + 1] * 100f).ToString("0");

            case FarmUpgradeType.HarvestCount:
                if (currentHarvestLevel >= harvestCountValues.Length - 1) return "MAX";
                return harvestCountValues[currentHarvestLevel + 1].ToString();

            default: return "";
        }
    }

    public int GetNextUpgradeCost(int currentLevel)
    {
        if (currentLevel >= upgradeCosts.Length - 1)
        {
            return -1;
        }
        return upgradeCosts[currentLevel + 1];
    }

    public bool TryUpgradeTileCount(int playerGold) { return TryProcessUpgrade(ref currentTileLevel, playerGold, "텃밭 개수"); }
    public void OnClickUpgradeTile()
    {
        // int currentGold = InventoryManager.Instance.GetGold();
        int currentGold = 50000; // 임시 골드
        TryUpgradeTileCount(currentGold);
    }

    public bool TryUpgradeTimeReduction(int playerGold) { return TryProcessUpgrade(ref currentTimeLevel, playerGold, "재배 시간 감소"); }
    public void OnClickUpgradeTime()
    {
        int currentGold = 50000; // 임시 골드
        TryUpgradeTimeReduction(currentGold);
    }

    public bool TryUpgradeHarvestCount(int playerGold) { return TryProcessUpgrade(ref currentHarvestLevel, playerGold, "작물 수확 개수"); }
    public void OnClickUpgradeHarvest()
    {
        int currentGold = 50000; // 임시 골드
        TryUpgradeHarvestCount(currentGold);
    }

    private bool TryProcessUpgrade(ref int targetLevel, int playerGold, string upgradeName)
    {
        if (targetLevel >= upgradeCosts.Length - 1)
        {
            Debug.Log($"{upgradeName} upgrade is already max level");
            return false;
        }

        int cost = upgradeCosts[targetLevel + 1];

        if (playerGold >= cost)
        {
            // TODO: 실제 플레이어 골드 차감 로직 추가 필요
            targetLevel++;
            Debug.Log($"{upgradeName} upgrade success (current level : {targetLevel})");
            return true;
        }
        else
        {
            Debug.Log($"no gold (require gold: {cost})");
            return false;
        }
    }
}
