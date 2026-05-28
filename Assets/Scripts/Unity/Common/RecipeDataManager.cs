using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 메뉴 선택 데이터 관리 (Singleton)
/// - 아침/점심/저녁 도시락 선택 데이터 저장
/// - Save/Load (ProgressSystem.PhaseData에 피기백)
/// - 레시피 검색은 RecipeLookupService로 분리
/// </summary>
public class RecipeDataManager : SingletonMonoBehaviour<RecipeDataManager>
{
    private MenuSelection[] menuSelections;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[RecipeDataManager] Initialized");
    }

    public void Initialize()
    {
        InitializeMenus();
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

    // ========== 위임: RecipeLookupService ==========

    public string GetToolIdForMinigame(string minigameId)
    {
        return RecipeLookupService.Instance?.GetToolIdForMinigame(minigameId);
    }

    /// <summary>
    /// 선택된 메뉴 데이터를 PhaseData에 준비 (SaveManager에서 호출)
    /// </summary>
    public void PrepareForSave()
    {
        if (ProgressSystem.Instance?.phaseData == null)
        {
            Debug.LogWarning("[RecipeDataManager] Cannot prepare: ProgressSystem not available");
            return;
        }

        var selectedMenuData = new List<string>();

        for (int i = 0; i < menuSelections.Length; i++)
        {
            var menu = menuSelections[i];
            if (menu.HasSelection())
            {
                string mainId = menu.MainMenu?.id ?? "";
                string sidesIds = string.Join(",", menu.SideMenus.ConvertAll(f => f.id));
                selectedMenuData.Add($"{mainId}|{sidesIds}");
            }
            else
            {
                selectedMenuData.Add("");
            }
        }

        ProgressSystem.Instance.phaseData.SelectedMenus = selectedMenuData;
    }

    /// <summary>
    /// ProgressSystem에서 메뉴 데이터 로드
    /// </summary>
    public void LoadMenusFromProgress()
    {
        if (ProgressSystem.Instance?.phaseData?.SelectedMenus == null)
        {
            Debug.Log("[RecipeDataManager] No saved menu data to load");
            return;
        }

        var savedMenus = ProgressSystem.Instance.phaseData.SelectedMenus;

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
                FoodData mainFood = CatalogProvider.Food?.GetById(mainId);
                if (mainFood != null)
                {
                    menuSelections[i].SetMain(mainFood);
                }
            }

            // 사이드 메뉴 설정
            foreach (string sideId in sideIds)
            {
                FoodData sideFood = CatalogProvider.Food?.GetById(sideId);
                if (sideFood != null)
                {
                    menuSelections[i].AddSide(sideFood);
                }
            }
        }

        Debug.Log($"[RecipeDataManager] Loaded menu data from progress");
    }

}
