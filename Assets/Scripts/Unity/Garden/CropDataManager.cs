using System.Collections.Generic;
using UnityEngine;

public class CropDataManager : MonoBehaviour
{
    public static CropDataManager Instance { get; private set; }

    private List<CropData> crops = new();
    private float totalWeight;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadData();
    }

    void LoadData()
    {
        var rows = CsvModelConverter.Parse<CropData>(CatalogProvider.Csvs?.cropData);
        foreach (var row in rows)
        {
            row.sprite = CatalogProvider.CropSprites?.Get(row.imagePath);
            if (row.sprite == null)
                Debug.LogWarning($"[CropDataManager] 스프라이트 없음: {row.imagePath}");

            crops.Add(row);
            totalWeight += row.spawnWeight;
        }
        Debug.Log($"[CropDataManager] {crops.Count}개 작물 로드 완료");
    }

    public CropData GetCropById(string cropId) => crops.Find(c => c.cropId == cropId);

    public CropData GetRandomCropByWeight()
    {
        if (crops.Count == 0) return null;
        return GameRandom.WeightedPick(GameRandom.Immutable, crops, c => c.spawnWeight);
    }
}
