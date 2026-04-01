using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : SingletonMonoBehaviour<InventoryManager>, LoadInventoryUsecase
{
    private Dictionary<FoodData, int> inventory = new Dictionary<FoodData, int>();
    private List<FoodData> allFoodData;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[InventoryManager] Initialized");
    }

    public void Initialize()
    {
        LoadAllFoodData();
        LoadInventoryFromDisk();
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
        FoodData[] foods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
        allFoodData = new List<FoodData>(foods);
        Debug.Log($"[InventoryManager] Loaded {allFoodData.Count} food data assets");
    }

    private void LoadInventoryFromDisk()
    {
        string path = Application.persistentDataPath + "/saves/inventory";

        if (DataSaveUtil.HasFile<InventorySaveData>(path))
        {
            InventorySaveData saveData = DataSaveUtil.LoadData(new InventorySaveData(), path);
            DeserializeInventory(saveData);
            Debug.Log($"[InventoryManager] Loaded inventory from disk: {inventory.Count} items");
        }
        else
        {
            InitializeDefaultInventory();
        }
    }

    private void InitializeDefaultInventory()
    {
        // 모든 INGREDIENT 타입 재료를 99개씩 지급
        foreach (FoodData food in allFoodData)
        {
            if (food.type == FoodType.INGREDIENT)
            {
                inventory[food] = 99;
            }
        }
        flush();
        Debug.Log($"[InventoryManager] Initialized default inventory with {inventory.Count} ingredients (all x99)");
    }

    public void flush()
    {
        string path = Application.persistentDataPath + "/saves/inventory";
        InventorySaveData saveData = SerializeInventory();
        DataSaveUtil.SaveData(saveData, path);
    }

    private InventorySaveData SerializeInventory()
    {
        InventorySaveData data = new InventorySaveData();
        foreach (var entry in inventory)
        {
            if (entry.Value > 0)
            {
                data.foodIds.Add(entry.Key.id);
                data.amounts.Add(entry.Value);
            }
        }
        return data;
    }

    private void DeserializeInventory(InventorySaveData data)
    {
        inventory.Clear();
        for (int i = 0; i < data.foodIds.Count; i++)
        {
            FoodData food = allFoodData.Find(f => f.id == data.foodIds[i]);
            if (food != null)
            {
                inventory[food] = data.amounts[i];
            }
        }
    }

    public int CheckStockAmount(FoodData food)
    {
        return (food != null && inventory.TryGetValue(food, out int count)) ? count : 0;
    }

    public void ConsumeFood(FoodData food, int amount)
    {
        if (food != null && inventory.ContainsKey(food))
        {
            inventory[food] = Mathf.Max(0, inventory[food] - amount);
        }
    }

    public void AddFood(FoodData food, int amount)
    {
        if (food == null) return;
        if (inventory.ContainsKey(food))
            inventory[food] += amount;
        else
            inventory[food] = amount;
    }

    public List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category)
    {
        return inventory.Keys
            .Where(f => f.ingredient != null && f.ingredient.display == category)
            .Select(f => (f, f.ingredient))
            .ToList();
    }

}
