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
        if(crop == null) return false;

        int passed = phaseProvider.CurrentPhaseIndex - plantedPhase;
        return passed >= crop.growPhaseCount;
    }

    public bool Harvest(out string harvestedCropId, out int harvestedCrops, out int returnedSeeds)
    {
        harvestedCropId = "";
        harvestedCrops = 0;
        returnedSeeds = 0;

        if (!IsHarvestable()) return false;

        harvestedCropId = crop.cropId;

        harvestedCrops = FarmUpgradeManager.Instance.GetCurrentHarvestCount();

        returnedSeeds = crop.seedReturnCount;

        crop = null;
        return true;
    }

    public bool IsEmpty()
    {
        return crop == null;
    }

    public CropData GetCurrentCrop()
    {
        return crop;
    }

    public int GetPassedPhases()
    {
        if (crop == null) return 0;
        return phaseProvider.CurrentPhaseIndex - plantedPhase;
    }
}
