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

        int passed = (phaseProvider.CurrentPhaseIndex - plantedPhase + phaseProvider.TotalPhaseCount) % phaseProvider.TotalPhaseCount;
        return passed >= crop.growPhaseCount;
    }

    public void Harvest()
    {
        if (!IsHarvestable()) return;

        //Inventory.Add(crop.cropId, crop.harvestCount);
        //Inventory.Add(crop.cropId + "_Seed", crop.seedReturnCount);

        crop = null;
    }

    public bool IsEmpty()
    {
        return crop == null;
    }
}
