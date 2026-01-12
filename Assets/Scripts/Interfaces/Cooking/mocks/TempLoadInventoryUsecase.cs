using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TempLoadInventoryUsecase : LoadInventoryUsecase
{
    // Key를 FoodData로 변경하여 모든 아이템을 수용
    private readonly Dictionary<FoodData, int> inventory = new Dictionary<FoodData, int>();

    public TempLoadInventoryUsecase()
    {
        // 1. 모든 음식 SO 로드 (Baking 시점에 IngredientData와 연결됨)
        var allFoods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");

        // 2. 초기 재고 설정 (테스트용)
        foreach (var food in allFoods)
        {
            inventory[food] = 10;
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
        if (inventory.ContainsKey(food)) inventory[food] += amount;
        else inventory[food] = amount;
    }

    // [핵심] 이제 Mapping 로직 없이 food.ingredient로 즉시 접근
    public List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category)
    {
        return inventory.Keys
            // 1. 식재료 정보(IngredientData)가 있고
            .Where(f => f.ingredient != null 
                // 2. 해당 카테고리가 일치하는 것만 필터링
                && f.ingredient.display == category) 
            .Select(f => (f, f.ingredient))
            .ToList();
    }
}