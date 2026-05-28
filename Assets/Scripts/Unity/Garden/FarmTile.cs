using UnityEngine;

public class FarmTile
{
    CropData crop;
    int plantedPhase;
    TimePhaseProvider phaseProvider;

    public FarmTile(TimePhaseProvider provider)
    {
        phaseProvider = provider;
    }

    public void Plant(CropData data)
    {
        crop = data;
        plantedPhase = phaseProvider.CurrentPhaseIndex;
    }

    public bool IsHarvestable()
    {
        if (crop == null) return false;

        int passed = phaseProvider.CurrentPhaseIndex - plantedPhase;
        float timeReduction = FarmUpgradeManager.Instance?.GetCurrentData("timeReduction")?.value ?? 0f;
        int requiredPhases = Mathf.CeilToInt(crop.growPhaseCount * (1f - timeReduction));
        return passed >= requiredPhases;
    }

    public bool Harvest(out string harvestedCropId, out int harvestedCrops, int harvestCount)
    {
        harvestedCropId = "";
        harvestedCrops  = 0;

        if (!IsHarvestable()) return false;

        harvestedCropId = crop.cropId;
        harvestedCrops  = harvestCount;

        crop = null;
        return true;
    }

    public bool IsEmpty() => crop == null;

    public CropData GetCurrentCrop() => crop;

    public int GetPassedPhases()
    {
        if (crop == null) return 0;
        return phaseProvider.CurrentPhaseIndex - plantedPhase;
    }

    // ── 저장/로드 ──

    public FarmTileSaveData GetSaveData()
    {
        return new FarmTileSaveData
        {
            cropId = crop?.cropId ?? "",
            plantedPhase = plantedPhase
        };
    }

    public void ApplySaveData(FarmTileSaveData data)
    {
        if (string.IsNullOrEmpty(data.cropId))
        {
            crop = null;
            return;
        }
        crop = GameSessionRoot.Instance?.CropCatalog.GetCropById(data.cropId);
        plantedPhase = data.plantedPhase;
    }
}

[System.Serializable]
public class FarmTileSaveData
{
    public string cropId = "";
    public int plantedPhase;
}
