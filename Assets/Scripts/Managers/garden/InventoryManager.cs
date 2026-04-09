using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private TempLoadInventoryUsecase inventoryUsecase;
    private SearchFoodUsecase searchFoodUsecase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // 임시로 의존성 주입
            inventoryUsecase = new TempLoadInventoryUsecase(searchFoodUsecase);
        }
        else Destroy(gameObject);
    }

    public void AddHarvestedCrop(string cropId, int amount)
    {
        if (inventoryUsecase == null) return;

        var data = inventoryUsecase.Search(cropId);

        if (data.Item1 != null)
        {
            inventoryUsecase.addFood(data.Item1, amount);

            Debug.Log($"[Inventory] Added {cropId} x{amount}. Current total: {inventoryUsecase.CheckStockAmount(cropId)}");
        }
        else
        {
            Debug.LogWarning($"[Inventory] Cannot find ingredient data for {cropId}");
        }
    }
}