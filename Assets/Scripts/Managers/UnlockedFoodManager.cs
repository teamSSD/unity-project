using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 레시피 해금 시스템 관리 (Singleton)
/// IUnlockedFoodProvider 구현 - 해금된 레시피 목록 제공
/// </summary>
public class UnlockedFoodManager : SingletonMonoBehaviour<UnlockedFoodManager>, IUnlockedFoodProvider
{
    /// <summary>
    /// 해금된 레시피 ID 집합 (중복 방지)
    /// </summary>
    private HashSet<string> unlockedRecipeIds = new HashSet<string>();

    /// <summary>
    /// 캐시된 전체 음식 데이터
    /// </summary>
    private List<FoodData> allFoodData;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[UnlockedFoodManager] Awake completed");
    }

    /// <summary>
    /// 표준 초기화 메서드 (GameStart에서 호출)
    /// </summary>
    public void Initialize()
    {
        LoadAllFoodData();
        Debug.Log("[UnlockedFoodManager] Initialized");
    }

    private void LoadAllFoodData()
    {
        FoodData[] foods = Resources.LoadAll<FoodData>(ResourcePaths.Data.FoodData);
        allFoodData = new List<FoodData>(foods);
        Debug.Log($"[UnlockedFoodManager] Loaded {allFoodData.Count} total food data assets");
    }

    /// <summary>
    /// 해금 데이터를 ProgressSystem에서 로드
    /// </summary>
    public void LoadUnlocksFromProgress()
    {
        if (ProgressSystem.Instance?.phaseData == null)
        {
            Debug.LogWarning("[UnlockedFoodManager] ProgressSystem not ready, using default unlocks");
            UnlockDefaultRecipes();
            return;
        }

        var unlockedList = ProgressSystem.Instance.phaseData.UnlockedRecipes;
        if (unlockedList == null || unlockedList.Count == 0)
        {
            Debug.Log("[UnlockedFoodManager] No unlocked recipes in save data, using defaults");
            UnlockDefaultRecipes();
        }
        else
        {
            unlockedRecipeIds = new HashSet<string>(unlockedList);
            Debug.Log($"[UnlockedFoodManager] Loaded {unlockedRecipeIds.Count} unlocked recipes from save");
        }
    }

    /// <summary>
    /// 초기 기본 레시피 해금 (전력실 꼬치 I058 제외)
    /// </summary>
    public void UnlockDefaultRecipes()
    {
        // 시작 해금 메뉴 (2 MAIN + 2 SIDE)
        // 나머지는 배달 퀘스트로 순차 해금
        string[] defaultMains = {
            "I044", // 기계장 고기정식
            "I060"  // 옥상 오믈렛
        };

        string[] defaultSides = {
            "I046", // 루미 젤리
            "I062"  // 환기구 연어구이
        };

        foreach (var id in defaultMains)
        {
            UnlockRecipe(id);
        }

        foreach (var id in defaultSides)
        {
            UnlockRecipe(id);
        }

        PrepareForSave();
        Debug.Log($"[UnlockedFoodManager] Default recipes unlocked: {defaultMains.Length} mains + {defaultSides.Length} sides");
    }

    /// <summary>
    /// 레시피 해금
    /// </summary>
    public void UnlockRecipe(string foodId)
    {
        if (unlockedRecipeIds.Add(foodId))
        {
            Debug.Log($"[UnlockedFoodManager] Unlocked recipe: {foodId}");
        }
    }

    /// <summary>
    /// 레시피 잠금
    /// </summary>
    public void LockRecipe(string foodId)
    {
        if (unlockedRecipeIds.Remove(foodId))
        {
            Debug.Log($"[UnlockedFoodManager] Locked recipe: {foodId}");
        }
    }

    /// <summary>
    /// 모든 레시피 해금 (디버그/치트용)
    /// </summary>
    public void UnlockAll()
    {
        unlockedRecipeIds.Clear();
        foreach (var food in allFoodData)
        {
            unlockedRecipeIds.Add(food.id);
        }
        PrepareForSave();
        Debug.Log($"[UnlockedFoodManager] All {unlockedRecipeIds.Count} recipes unlocked");
    }

    /// <summary>
    /// 해금 데이터를 PhaseData에 준비 (SaveManager에서 호출)
    /// </summary>
    public void PrepareForSave()
    {
        if (ProgressSystem.Instance?.phaseData != null)
        {
            ProgressSystem.Instance.phaseData.UnlockedRecipes = unlockedRecipeIds.ToList();
        }
    }

    // IUnlockedFoodProvider 구현

    public List<FoodData> GetUnlockedMainFoods()
    {
        return allFoodData
            .Where(f => f.type == FoodType.MAIN && unlockedRecipeIds.Contains(f.id))
            .ToList();
    }

    public List<FoodData> GetUnlockedSideFoods()
    {
        return allFoodData
            .Where(f => f.type == FoodType.SIDE && unlockedRecipeIds.Contains(f.id))
            .ToList();
    }

    public bool IsUnlocked(string foodId)
    {
        return unlockedRecipeIds.Contains(foodId);
    }

}
