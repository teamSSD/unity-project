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
    private readonly FoodData[] catalog;

    // 기본 ID 세트로 Resources에서 로드
    public MockUnlockedFoodProvider()
        : this(Resources.LoadAll<FoodData>(ResourcePaths.Data.FoodData)) { }

    // 특정 ID 목록 + Resources에서 로드
    public MockUnlockedFoodProvider(params string[] foodIds)
        : this(Resources.LoadAll<FoodData>(ResourcePaths.Data.FoodData), foodIds) { }

    // 카탈로그 주입 (Resources 불필요 — 순수 단위 테스트용)
    public MockUnlockedFoodProvider(FoodData[] catalog, params string[] foodIds)
    {
        this.catalog = catalog;
        unlockedFoodIds = foodIds.Length > 0
            ? new HashSet<string>(foodIds)
            : new HashSet<string>
            {
                // 메인 메뉴 (5개)
                "I034", "I039", "I044", "I049", "I053",
                // 사이드 메뉴 (4개)
                "I046", "I056", "I058", "I062"
            };
    }

    public List<FoodData> GetUnlockedMainFoods() =>
        catalog.Where(f => f.type == FoodType.MAIN && unlockedFoodIds.Contains(f.id)).ToList();

    public List<FoodData> GetUnlockedSideFoods() =>
        catalog.Where(f => f.type == FoodType.SIDE && unlockedFoodIds.Contains(f.id)).ToList();

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
        foreach (var food in catalog) unlockedFoodIds.Add(food.id);
    }

    public void LockAll()
    {
        unlockedFoodIds.Clear();
    }
}
