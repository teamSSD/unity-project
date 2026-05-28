using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : SingletonMonoBehaviour<InventoryManager>, LoadInventoryUsecase
{
    private Dictionary<FoodData, List<InventoryBatch>> inventory = new Dictionary<FoodData, List<InventoryBatch>>();
    private List<FoodData> allFoodData;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[InventoryManager] Initialized");
    }

    public void Initialize()
    {
        LoadAllFoodData();
    }

    /// <summary>
    /// New Game 전용: 기존 세이브 무시하고 초기 인벤토리로 리셋
    /// </summary>
    public void ResetToDefault()
    {
        LoadAllFoodData();
        inventory.Clear();
        InitializeDefaultInventory();
    }

    private void LoadAllFoodData()
    {
        var catalog = CatalogProvider.Food?.All;
        allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
        Debug.Log($"[InventoryManager] Loaded {allFoodData.Count} food data from catalog");
    }

    // 시작 메뉴(옥상오믈렛, 기계장, 루미젤리, 환기구연어) 원재료
    private static readonly Dictionary<string, int> startingIngredients = new()
    {
        { "I007", 3 }, // 고추장
        { "I008", 3 }, // 레몬
        { "I009", 3 }, // 인공고기
        { "I010", 3 }, // 루미계란
        { "I017", 3 }, // 스틸루트
        { "I019", 3 }, // 검은된장
        { "I020", 3 }, // 조명시럽
        { "I022", 3 }, // 연기잎버터
        { "I025", 3 }, // 청양잎
        { "I026", 3 }, // 루미잎
        { "I027", 3 }, // 빛토마토
        { "I031", 3 }, // 인공연어
        { "I068", 3 }, // 신문지
    };

    private void InitializeDefaultInventory()
    {
        foreach (FoodData food in allFoodData)
        {
            if (food.type == FoodType.INGREDIENT)
            {
                int expDays = GetExpirationDays(food);
                if (startingIngredients.TryGetValue(food.id, out int qty))
                {
                    var batch = new InventoryBatch { quantity = qty, daysRemaining = expDays };
                    inventory[food] = new List<InventoryBatch> { batch };
                }
                else
                {
                    inventory[food] = new List<InventoryBatch>();
                }
            }
        }
        Debug.Log($"[InventoryManager] Initialized inventory: {startingIngredients.Count} ingredients with starting stock");
    }

    // ========== 공개 API (시그니처 유지) ==========

    public void AddFood(FoodData food, int amount)
    {
        if (food == null || amount <= 0) return;

        int expDays = GetExpirationDays(food);
        var batch = new InventoryBatch { quantity = amount, daysRemaining = expDays };

        if (!inventory.ContainsKey(food))
            inventory[food] = new List<InventoryBatch>();

        inventory[food].Add(batch);
        SortBatches(inventory[food]);
    }

    public void ConsumeFood(FoodData food, int amount)
    {
        if (food == null || amount <= 0) return;
        if (!inventory.TryGetValue(food, out var batches)) return;

        int remaining = amount;
        for (int i = 0; i < batches.Count && remaining > 0; i++)
        {
            int take = Mathf.Min(remaining, batches[i].quantity);
            batches[i].quantity -= take;
            remaining -= take;
        }

        batches.RemoveAll(b => b.quantity <= 0);
        if (batches.Count == 0)
            inventory.Remove(food);
    }

    public int CheckStockAmount(FoodData food)
    {
        if (food == null || !inventory.TryGetValue(food, out var batches)) return 0;
        int total = 0;
        foreach (var b in batches) total += b.quantity;
        return total;
    }

    public List<InventoryBatch> GetBatches(FoodData food)
    {
        if (food == null || !inventory.TryGetValue(food, out var batches)) return new List<InventoryBatch>();
        return new List<InventoryBatch>(batches);
    }

    public List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category)
    {
        return inventory
            .Where(kv => kv.Value.Count > 0 && kv.Key.ingredient != null && kv.Key.ingredient.display == category)
            .Select(kv => (kv.Key, kv.Key.ingredient))
            .ToList();
    }

    /// <summary>
    /// 새 종류가 카테고리 슬롯 한계에 막히는지 검사.
    /// 이미 보유 중인 종류는 수량 누적이라 항상 true.
    /// 카테고리 정의가 없으면 통과.
    /// </summary>
    public bool CanAcceptType(FoodData food)
    {
        if (food == null) return false;
        if (food.ingredient == null) return true;

        // 이미 1개 이상 보유 중이면 슬롯 추가 소모 없음
        if (inventory.TryGetValue(food, out var batches) && batches.Count > 0) return true;

        string upgradeType = UpgradeTypeFromCategory(food.ingredient.display);
        if (string.IsNullOrEmpty(upgradeType)) return true;

        var stg = StorageUpgradeManager.Instance;
        int max = stg != null ? stg.GetCurrentData(upgradeType)?.value ?? int.MaxValue : int.MaxValue;
        int unique = LoadIngredientsByCategory(food.ingredient.display).Count;
        return unique < max;
    }

    private static string UpgradeTypeFromCategory(IngredientDisplayCategory c) => c switch
    {
        IngredientDisplayCategory.UpperShelf   => "upperShelf",
        IngredientDisplayCategory.LowerShelf   => "lowerShelf",
        IngredientDisplayCategory.Refrigerator => "refrigerator",
        _ => null,
    };

    /// <summary>
    /// 텃밭 수확물을 인벤토리에 추가 (cropId → FoodData 검색 후 AddFood)
    /// </summary>
    public void AddHarvestedCrop(string cropId, int amount)
    {
        FoodData food = allFoodData?.Find(f => f.id == cropId);
        if (food != null)
        {
            AddFood(food, amount);
            Debug.Log($"[InventoryManager] Harvested {cropId} x{amount}");
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] Cannot find FoodData for cropId: {cropId}");
        }
    }

    // ========== 유통기한 ==========

    /// <summary>
    /// 하루 경과 처리: 모든 배치의 daysRemaining 감소, 만료 배치 제거.
    /// ProgressSystem.PassDay()에서 호출.
    /// </summary>
    public void AdvanceDay()
    {
        int expiredTotal = 0;
        var emptyKeys = new List<FoodData>();

        foreach (var kv in inventory)
        {
            foreach (var batch in kv.Value)
                batch.daysRemaining--;

            int removed = kv.Value.RemoveAll(b => b.daysRemaining <= 0);
            if (removed > 0)
                expiredTotal += removed;

            if (kv.Value.Count == 0)
                emptyKeys.Add(kv.Key);
        }

        foreach (var key in emptyKeys)
            inventory.Remove(key);

        if (expiredTotal > 0)
            Debug.Log($"[InventoryManager] AdvanceDay: {expiredTotal} batch(es) expired");
    }

    // ========== 저장/로드 ==========

    public InventorySaveData GetSaveData()
    {
        var data = new InventorySaveData();
        foreach (var kv in inventory)
        {
            if (kv.Value.Count == 0) continue;

            var entry = new InventoryItemEntry { foodId = kv.Key.id };
            foreach (var batch in kv.Value)
            {
                entry.batches.Add(new InventoryBatchEntry
                {
                    quantity = batch.quantity,
                    daysRemaining = batch.daysRemaining
                });
            }
            data.items.Add(entry);
        }
        return data;
    }

    public void ApplySaveData(InventorySaveData data)
    {
        inventory.Clear();

        // 신규 배치 데이터가 있으면 사용
        if (data.items != null && data.items.Count > 0)
        {
            foreach (var entry in data.items)
            {
                FoodData food = allFoodData.Find(f => f.id == entry.foodId);
                if (food == null) continue;

                var batches = new List<InventoryBatch>();
                foreach (var be in entry.batches)
                {
                    batches.Add(new InventoryBatch
                    {
                        quantity = be.quantity,
                        daysRemaining = be.daysRemaining
                    });
                }
                SortBatches(batches);
                inventory[food] = batches;
            }
        }
        // 레거시 폴백: foodIds + amounts → 단일 배치로 변환
        else if (data.foodIds != null && data.foodIds.Count > 0)
        {
            for (int i = 0; i < data.foodIds.Count; i++)
            {
                FoodData food = allFoodData.Find(f => f.id == data.foodIds[i]);
                if (food == null) continue;

                int expDays = GetExpirationDays(food);
                var batch = new InventoryBatch
                {
                    quantity = data.amounts[i],
                    daysRemaining = expDays
                };
                inventory[food] = new List<InventoryBatch> { batch };
            }
            Debug.Log("[InventoryManager] Migrated legacy save data to batch format");
        }
    }

    // ========== 내부 유틸 ==========

    private int GetExpirationDays(FoodData food)
    {
        if (food.ingredient != null && food.ingredient.expirationDay > 0)
            return food.ingredient.expirationDay;
        return 10; // 기본값
    }

    /// <summary>daysRemaining 오름차순 정렬 (FIFO 소비용)</summary>
    private void SortBatches(List<InventoryBatch> batches)
    {
        batches.Sort((a, b) => a.daysRemaining.CompareTo(b.daysRemaining));
    }
}

/// <summary>
/// 인벤토리 배치: 동일 시점에 획득한 동일 아이템 묶음
/// </summary>
[System.Serializable]
public class InventoryBatch
{
    public int quantity;
    public int daysRemaining;
}
