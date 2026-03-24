using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 레시피 데이터 종합 관리 (Singleton)
/// - 아침/점심/저녁 메뉴 선택 데이터 저장
/// - 레시피 검색 기능 (SearchRecipeUsecase 구현)
/// - DontDestroyOnLoad로 씬 전환 시에도 유지
/// - Pure Data Manager (UI 없음)
/// </summary>
public class RecipeDataManager : MonoBehaviour, SearchRecipeUsecase
{
    private static RecipeDataManager instance;
    public static RecipeDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RecipeDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RecipeDataManager");
                    instance = go.AddComponent<RecipeDataManager>();
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 메뉴 선택 데이터 (0=아침, 1=점심, 2=저녁)
    /// </summary>
    private MenuSelection[] menuSelections;

    // ========== Recipe Search (SearchRecipeUsecase) ==========
    private Dictionary<string, RecipeData> recipeLookup = new Dictionary<string, RecipeData>();
    private List<RecipeData> allRecipes;

    // TODO: Make this data-driven by adding toolId to RecipeData or CookingToolData
    private readonly Dictionary<string, string> minigameIdToToolId = new Dictionary<string, string>
    {
        { "", "" },
        { "M001", "T001" },
        { "M002", "T002" },
        { "M004", "T003" },
        { "M005", "T003" },
        { "M006", "T004" },
        { "M007", "T005" }
    };

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

        Debug.Log("[RecipeDataManager] Initialized");
    }

    public void Initialize()
    {
        InitializeMenus();
        LoadAllRecipes();
        BuildLookupTable();
        LoadMenusFromProgress(); // 저장된 메뉴 데이터 로드
    }

    private void InitializeMenus()
    {
        menuSelections = new MenuSelection[3];
        menuSelections[0] = new MenuSelection("도시락 1");
        menuSelections[1] = new MenuSelection("도시락 2");
        menuSelections[2] = new MenuSelection("도시락 3");
    }

    /// <summary>
    /// 메뉴 선택 데이터 가져오기
    /// </summary>
    public MenuSelection GetMenu(int index)
    {
        if (menuSelections == null) InitializeMenus();
        
        if (index < 0 || index >= menuSelections.Length)
        {
            Debug.LogError($"[RecipeDataManager] Invalid menu index: {index}");
            return null;
        }
        return menuSelections[index];
    }

    /// <summary>
    /// 선택된 모든 메뉴를 MenuSchema 리스트로 반환 (Cooking 씬용)
    /// </summary>
    public List<MenuSchema> GetAllMenusAsSchema()
    {
        var menuList = new List<MenuSchema>();

        for (int i = 0; i < menuSelections.Length; i++)
        {
            var menu = menuSelections[i];
            if (menu.HasSelection())
            {
                var menuSchema = new MenuSchema(
                    menu.Name,
                    i + 1, // orderNumber (1, 2, 3)
                    menu.MainMenu,
                    new List<FoodData>(menu.SideMenus) // 복사본
                );
                menuList.Add(menuSchema);
            }
        }

        return menuList;
    }

    /// <summary>
    /// 모든 메뉴 선택 초기화
    /// </summary>
    public void ClearAllMenus()
    {
        foreach (var menu in menuSelections)
        {
            menu.Clear();
        }
        Debug.Log("[RecipeDataManager] All menus cleared");
    }

    /// <summary>
    /// 선택된 메뉴가 있는지 확인
    /// </summary>
    public bool HasAnySelection()
    {
        if (menuSelections == null)
        {
            InitializeMenus();  // Create if needed
            return false;  // Empty menu = no selection
        }

        foreach (var menu in menuSelections)
        {
            if (menu.HasSelection())
                return true;
        }
        return false;
    }

    // ========== Recipe Search Methods ==========

    private void LoadAllRecipes()
    {
        RecipeData[] recipes = Resources.LoadAll<RecipeData>("ScriptableObjects/RecipeData");
        allRecipes = new List<RecipeData>(recipes);
        Debug.Log($"[RecipeDataManager] Loaded {allRecipes.Count} recipes");
    }

    private void BuildLookupTable()
    {
        recipeLookup.Clear();

        foreach (var recipe in allRecipes)
        {
            if (!minigameIdToToolId.TryGetValue(recipe.minigameId, out string toolId))
            {
                Debug.LogWarning($"[RecipeDataManager] No toolId mapping for minigameId '{recipe.minigameId}' in recipe '{recipe.id}'");
                continue;
            }

            string key = GenerateLookupKey(toolId, recipe.inputs.Select(i => i.food));

            if (!recipeLookup.ContainsKey(key))
            {
                recipeLookup.Add(key, recipe);
            }
            else
            {
                Debug.LogWarning($"[RecipeDataManager] Duplicate recipe key '{key}': {recipe.id} conflicts with {recipeLookup[key].id}");
            }
        }

        Debug.Log($"[RecipeDataManager] Built lookup table with {recipeLookup.Count} entries");
    }

    /// <summary>
    /// SearchRecipeUsecase 구현: 도구와 재료로 레시피 검색
    /// </summary>
    /// <param name="toolId">조리 도구 ID (e.g., "T001")</param>
    /// <param name="ingredients">재료 리스트</param>
    /// <returns>매칭되는 레시피 또는 null</returns>
    public RecipeData Search(string toolId, List<FoodData> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
        {
            Debug.LogWarning("[RecipeDataManager] Search called with null or empty ingredients");
            return null;
        }

        string searchKey = GenerateLookupKey(toolId, ingredients);

        if (recipeLookup.TryGetValue(searchKey, out RecipeData result))
        {
            return result;
        }

        // Fallback to default empty recipe if exists
        if (recipeLookup.TryGetValue(":", out RecipeData fallback))
        {
            Debug.Log($"[RecipeDataManager] No recipe found for '{searchKey}', using fallback");
            return fallback;
        }

        Debug.LogWarning($"[RecipeDataManager] No recipe found for '{searchKey}'");
        return null;
    }

    /// <summary>
    /// 검색 키 생성: "toolId:ingredient1_ingredient2_ingredient3" (재료 ID 정렬됨)
    /// </summary>
    private string GenerateLookupKey(string toolId, IEnumerable<FoodData> ingredients)
    {
        var sortedIds = ingredients
            .Select(f => f.id)
            .OrderBy(id => id);

        return $"{toolId}:{string.Join("_", sortedIds)}";
    }

    /// <summary>
    /// 모든 레시피 가져오기 (디버깅/UI용)
    /// </summary>
    public List<RecipeData> GetAllRecipes()
    {
        return allRecipes != null ? new List<RecipeData>(allRecipes) : new List<RecipeData>();
    }

    /// <summary>
    /// 특정 도구로 만들 수 있는 레시피 가져오기
    /// </summary>
    public List<RecipeData> GetRecipesForTool(string toolId)
    {
        if (allRecipes == null) return new List<RecipeData>();

        return allRecipes.Where(r =>
        {
            if (minigameIdToToolId.TryGetValue(r.minigameId, out string mappedToolId))
            {
                return mappedToolId == toolId;
            }
            return false;
        }).ToList();
    }

    /// <summary>
    /// 선택된 메뉴 데이터를 ProgressSystem에 저장
    /// </summary>
    public void flush()
    {
        if (ProgressSystem.instance?.phaseData == null)
        {
            Debug.LogWarning("[RecipeDataManager] Cannot flush: ProgressSystem not available");
            return;
        }

        // menuSelections를 직렬화하여 저장
        var selectedMenuData = new List<string>();

        for (int i = 0; i < menuSelections.Length; i++)
        {
            var menu = menuSelections[i];
            if (menu.HasSelection())
            {
                // 형식: "MainMenuId|Side1,Side2,Side3"
                string mainId = menu.MainMenu?.id ?? "";
                string sidesIds = string.Join(",", menu.SideMenus.ConvertAll(f => f.id));
                selectedMenuData.Add($"{mainId}|{sidesIds}");
            }
            else
            {
                selectedMenuData.Add(""); // 빈 메뉴
            }
        }

        ProgressSystem.instance.phaseData.SelectedMenus = selectedMenuData;
        // 저장은 PassDay()에서만 수행 (여기서는 phaseData에 데이터만 넣음)

        Debug.Log($"[RecipeDataManager] Menu data prepared for save: {string.Join(" / ", selectedMenuData)}");
    }

    /// <summary>
    /// ProgressSystem에서 메뉴 데이터 로드
    /// </summary>
    public void LoadMenusFromProgress()
    {
        if (ProgressSystem.instance?.phaseData?.SelectedMenus == null)
        {
            Debug.Log("[RecipeDataManager] No saved menu data to load");
            return;
        }

        var savedMenus = ProgressSystem.instance.phaseData.SelectedMenus;

        for (int i = 0; i < Mathf.Min(savedMenus.Count, menuSelections.Length); i++)
        {
            string menuData = savedMenus[i];
            if (string.IsNullOrEmpty(menuData)) continue;

            // 파싱: "MainMenuId|Side1,Side2,Side3"
            string[] parts = menuData.Split('|');
            if (parts.Length != 2) continue;

            string mainId = parts[0];
            string[] sideIds = parts[1].Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            // 메인 메뉴 설정
            if (!string.IsNullOrEmpty(mainId))
            {
                FoodData mainFood = Resources.Load<FoodData>($"ScriptableObjects/FoodData/{mainId}");
                if (mainFood != null)
                {
                    menuSelections[i].SetMain(mainFood);
                }
            }

            // 사이드 메뉴 설정
            foreach (string sideId in sideIds)
            {
                FoodData sideFood = Resources.Load<FoodData>($"ScriptableObjects/FoodData/{sideId}");
                if (sideFood != null)
                {
                    menuSelections[i].AddSide(sideFood);
                }
            }
        }

        Debug.Log($"[RecipeDataManager] Loaded menu data from progress");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

/// <summary>
/// 메뉴 선택 데이터 (아침/점심/저녁 각각)
/// </summary>
[System.Serializable]
public class MenuSelection
{
    public string Name;
    public FoodData MainMenu;
    public List<FoodData> SideMenus;

    public MenuSelection(string name)
    {
        Name = name;
        SideMenus = new List<FoodData>();
    }

    public bool HasSelection()
    {
        return MainMenu != null;
    }

    public bool IsComplete()
    {
        return MainMenu != null;
    }

    public void Clear()
    {
        MainMenu = null;
        SideMenus.Clear();
    }

    public void SetMain(FoodData food)
    {
        MainMenu = food;
    }

    public void AddSide(FoodData food)
    {
        if (SideMenus.Count < 3) // 사이드 최대 3개
        {
            SideMenus.Add(food);
        }
    }

    public void RemoveSide(FoodData food)
    {
        SideMenus.Remove(food);
    }

    public override string ToString()
    {
        string mainName = MainMenu?.ingredientName ?? "None";
        string sideNames = string.Join(", ", SideMenus.ConvertAll(f => f.ingredientName));
        return $"{Name}: Main={mainName}, Sides=[{sideNames}]";
    }
}
