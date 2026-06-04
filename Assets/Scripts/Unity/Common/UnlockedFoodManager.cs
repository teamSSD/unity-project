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
    }

    private void LoadAllFoodData()
    {
        var catalog = CatalogProvider.Food?.All;
        allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
    }

    /// <summary>
    /// 자체 SaveData로 직렬화 (SaveManager가 호출). piggyback (PhaseData.UnlockedRecipes) 폐기.
    /// </summary>
    public UnlockedRecipesSaveData GetSaveData()
    {
        return new UnlockedRecipesSaveData { recipeIds = unlockedRecipeIds.ToList() };
    }

    /// <summary>
    /// 디스크에서 로드된 UnlockedRecipesSaveData 적용. 비어있으면 기본값 해금.
    /// </summary>
    public void ApplySaveData(UnlockedRecipesSaveData data)
    {
        if (data != null && data.recipeIds != null && data.recipeIds.Count > 0)
            unlockedRecipeIds = new HashSet<string>(data.recipeIds);
        else
            UnlockDefaultRecipes();
    }

    /// <summary>
    /// Legacy fallback: PhaseData.UnlockedRecipes (piggyback)에서 로드.
    /// UnlockedRecipesSaveData가 비어있을 때만 호출.
    /// </summary>
    public void LoadUnlocksFromProgressLegacy()
    {
        var unlockedList = GameSessionRoot.Instance?.Progress?.PhaseData?.UnlockedRecipes;
        if (unlockedList != null && unlockedList.Count > 0)
            unlockedRecipeIds = new HashSet<string>(unlockedList);
        else
            UnlockDefaultRecipes();
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
        // PrepareForSave 제거 — 자체 GetSaveData()가 직접 dump
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
    }

    // IUnlockedFoodProvider 구현

    public List<FoodData> GetAllMainFoods()
    {
        return allFoodData
            .Where(f => f.type == FoodType.MAIN)
            .OrderBy(f => f.id)
            .ToList();
    }

    public List<FoodData> GetAllSideFoods()
    {
        return allFoodData
            .Where(f => f.type == FoodType.SIDE)
            .OrderBy(f => f.id)
            .ToList();
    }

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
