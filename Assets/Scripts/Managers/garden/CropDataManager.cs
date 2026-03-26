using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class CropWeightData
{
    public CropData crop;
    public float weight;
}

public class CropDataManager : MonoBehaviour
{
    public static CropDataManager Instance { get; private set; }

    [Header("Data Connection")]
    public string csvResourcePath = "garden_weight";

    public string cropSoFolderPath = "ScriptableObjects/CropData";

    [Header("Weight Data List")]
    public List<CropWeightData> cropDropTable = new List<CropWeightData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadCSVAndSetWeights();
    }

    private void LoadCSVAndSetWeights()
    {
        cropDropTable.Clear();

        CropData[] allCropSOs = Resources.LoadAll<CropData>(cropSoFolderPath);
        Debug.Log($"[CropDataManager] Loaded {allCropSOs.Length} Crop SOs from Resources/{cropSoFolderPath}");

        List<GardenWeightData> rawDataList = CsvModelConverter.Parse<GardenWeightData>(csvResourcePath);

        foreach (var data in rawDataList)
        {
            if (data.Weight > 0f)
            {
                CropData matchingCrop = Array.Find(allCropSOs, c => c.cropId == data.Id);

                if (matchingCrop != null)
                {
                    cropDropTable.Add(new CropWeightData { crop = matchingCrop, weight = data.Weight });
                }
                else
                {
                    Debug.LogWarning($"[CropDataManager] Missing SO for ID: '{data.Id}'");
                }
            }
        }

        Debug.Log($"[CropDataManager] Successfully loaded {cropDropTable.Count} crop weights.");
    }

    public CropData GetRandomCropByWeight()
    {
        if (cropDropTable.Count == 0) return null;

        float randomValue = UnityEngine.Random.Range(0f, 1f);
        float cumulativeWeight = 0f;

        foreach (CropWeightData data in cropDropTable)
        {
            cumulativeWeight += data.weight;

            if (randomValue <= cumulativeWeight)
            {
                return data.crop;
            }
        }

        return cropDropTable[cropDropTable.Count - 1].crop;
    }
}