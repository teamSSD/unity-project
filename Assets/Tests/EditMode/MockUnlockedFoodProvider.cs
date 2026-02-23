using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 테스트용 IUnlockedFoodProvider Mock 구현
/// 특정 음식만 해금된 것으로 반환
/// </summary>
public class MockUnlockedFoodProvider : IUnlockedFoodProvider
{
    private readonly HashSet<string> unlockedFoodIds;

    public MockUnlockedFoodProvider()
    {
        unlockedFoodIds = new HashSet<string>
        {
            // 메인 메뉴 (5개)
            "I034", "I039", "I044", "I049", "I053",
            // 사이드 메뉴 (4개)
            "I046", "I056", "I058", "I062"
        };
    }

    public MockUnlockedFoodProvider(params string[] foodIds)
    {
        unlockedFoodIds = new HashSet<string>(foodIds);
    }

    public List<FoodData> GetUnlockedMainFoods()
    {
        var allFoods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
        return allFoods
            .Where(f => f.type == FoodType.MAIN && unlockedFoodIds.Contains(f.id))
            .ToList();
    }

    public List<FoodData> GetUnlockedSideFoods()
    {
        var allFoods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
        return allFoods
            .Where(f => f.type == FoodType.SIDE && unlockedFoodIds.Contains(f.id))
            .ToList();
    }

    public bool IsUnlocked(string foodId)
    {
        return unlockedFoodIds.Contains(foodId);
    }

    public void UnlockFood(string foodId)
    {
        unlockedFoodIds.Add(foodId);
    }

    public void LockFood(string foodId)
    {
        unlockedFoodIds.Remove(foodId);
    }

    public void UnlockAll()
    {
        var allFoods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
        foreach (var food in allFoods)
        {
            unlockedFoodIds.Add(food.id);
        }
    }

    public void LockAll()
    {
        unlockedFoodIds.Clear();
    }
}
