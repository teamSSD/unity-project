using System.Collections.Generic;
using Game.Domain.Common;
using UnityEngine;

/// <summary>
/// 인벤토리 facade (Phase 4-B). 로직과 상태는 GameSessionRoot.Inventory(POCO Service)에 위치.
/// 이 클래스는 backward-compat API + Initialize 시 카탈로그 로딩 트리거 담당.
/// </summary>
public class InventoryManager : SingletonMonoBehaviour<InventoryManager>, LoadInventoryUsecase
{
    private static InventoryService Svc => GameSessionRoot.Instance?.Inventory;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[InventoryManager] Initialized");
    }

    /// <summary>GameStart의 NewGame/LoadGame 진입 시 호출 — InventoryService는 GameSessionRoot Awake 시점에 이미 생성됨.</summary>
    public void Initialize()
    {
        // No-op: 카탈로그 + 서비스는 GameSessionRoot가 wiring.
    }

    public void ResetToDefault() => Svc?.ResetToDefault();

    public void AddFood(FoodData food, int amount) => Svc?.AddFood(food, amount);
    public void ConsumeFood(FoodData food, int amount) => Svc?.ConsumeFood(food, amount);
    public int CheckStockAmount(FoodData food) => Svc?.CheckStockAmount(food) ?? 0;
    public List<InventoryBatch> GetBatches(FoodData food) => Svc?.GetBatches(food) ?? new List<InventoryBatch>();
    public List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category)
        => Svc?.LoadIngredientsByCategory(category) ?? new List<(FoodData, IngredientData)>();
    public bool CanAcceptType(FoodData food) => Svc?.CanAcceptType(food) ?? false;
    public void AddHarvestedCrop(string cropId, int amount) => Svc?.AddHarvestedCrop(cropId, amount);
    public void AdvanceDay() => Svc?.AdvanceDay();

    public InventorySaveData GetSaveData() => Svc?.GetSaveData() ?? new InventorySaveData();
    public void ApplySaveData(InventorySaveData data) => Svc?.ApplySaveData(data);
}
