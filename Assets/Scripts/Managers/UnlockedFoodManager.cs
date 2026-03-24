using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 레시피 해금 시스템 관리 (Singleton)
/// IUnlockedFoodProvider 구현 - 해금된 레시피 목록 제공
/// </summary>
public class UnlockedFoodManager : MonoBehaviour, IUnlockedFoodProvider
{
    private static UnlockedFoodManager instance;
    public static UnlockedFoodManager Instance => instance;

    /// <summary>
    /// 해금된 레시피 ID 집합 (중복 방지)
    /// </summary>
    private HashSet<string> unlockedRecipeIds = new HashSet<string>();

    /// <summary>
    /// 캐시된 전체 음식 데이터
    /// </summary>
    private List<FoodData> allFoodData;

    private void Awake()
    {
        // Singleton 패턴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[UnlockedFoodManager] Awake completed");
    }

    /// <summary>
    /// 표준 초기화 메서드 (GameStart에서 호출)
    /// </summary>
    public void Initialize()
    {
        LoadAllFoodData();
        LoadUnlocksFromProgress();
        Debug.Log("[UnlockedFoodManager] Initialized");
    }

    private void LoadAllFoodData()
    {
        FoodData[] foods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
        allFoodData = new List<FoodData>(foods);
        Debug.Log($"[UnlockedFoodManager] Loaded {allFoodData.Count} total food data assets");
    }

    /// <summary>
    /// 해금 데이터를 ProgressSystem에서 로드
    /// </summary>
    public void LoadUnlocksFromProgress()
    {
        if (ProgressSystem.instance?.phaseData == null)
        {
            Debug.LogWarning("[UnlockedFoodManager] ProgressSystem not ready, using default unlocks");
            UnlockDefaultRecipes();
            return;
        }

        var unlockedList = ProgressSystem.instance.phaseData.UnlockedRecipes;
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
        // 기본 메인 메뉴 (6개 전체)
        string[] defaultMains = {
            "I034", // 새벽국
            "I039", // 구룡면
            "I044", // 기계장 고기정식
            "I049", // 스트리트 스테이크 49
            "I053", // 폐건물 삼각밥
            "I060"  // 옥상 오믈렛
        };

        // 기본 사이드 메뉴 (3개, I058 전력실 꼬치 제외)
        string[] defaultSides = {
            "I046", // 루미 젤리
            "I056", // 네온 샐러드
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

        SaveUnlocks();
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
        SaveUnlocks();
        Debug.Log($"[UnlockedFoodManager] All {unlockedRecipeIds.Count} recipes unlocked");
    }

    /// <summary>
    /// 해금 데이터를 ProgressSystem에 저장
    /// </summary>
    public void SaveUnlocks()
    {
        if (ProgressSystem.instance?.phaseData != null)
        {
            ProgressSystem.instance.phaseData.UnlockedRecipes = unlockedRecipeIds.ToList();
            // 저장은 PassDay()에서만 수행 (여기서는 phaseData에 데이터만 넣음)
            Debug.Log($"[UnlockedFoodManager] Unlocked recipe data prepared for save: {unlockedRecipeIds.Count} recipes");
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
