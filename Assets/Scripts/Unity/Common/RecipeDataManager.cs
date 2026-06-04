using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 메뉴 선택 데이터 관리 (Singleton)
/// - 아침/점심/저녁 도시락 선택 데이터 저장
/// - Save/Load (gamedata.json recipeBook 슬롯)
/// - 레시피 검색은 RecipeLookupService로 분리
/// </summary>
public class RecipeDataManager : SingletonMonoBehaviour<RecipeDataManager>
{
    private MenuSelection[] menuSelections;

    protected override void OnSingletonAwake()
    {
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
    /// 도시락 선택을 자체 SaveData로 직렬화 (SaveManager가 호출).
    /// piggyback 패턴 (PhaseData.SelectedMenus) 폐기, self-contained.
    /// </summary>
    public RecipeBookSaveData GetSaveData()
    {
        var data = new RecipeBookSaveData();
        for (int i = 0; i < menuSelections.Length; i++)
        {
            var menu = menuSelections[i];
            if (menu.HasSelection())
            {
                string mainId = menu.MainMenu?.id ?? "";
                string sidesIds = string.Join(",", menu.SideMenus.ConvertAll(f => f.id));
                data.selectedMenus.Add($"{mainId}|{sidesIds}");
            }
            else
            {
                data.selectedMenus.Add("");
            }
        }
        return data;
    }

    /// <summary>
    /// 디스크에서 로드된 RecipeBookSaveData 적용.
    /// </summary>
    public void ApplySaveData(RecipeBookSaveData data)
    {
        if (data == null || data.selectedMenus == null) return;
        ApplyMenuList(data.selectedMenus);
    }

    /// <summary>
    /// Legacy fallback: PhaseData.SelectedMenus(piggyback)에서 로드. RecipeBookSaveData가 비어있을 때.
    /// </summary>
    public void LoadMenusFromProgressLegacy()
    {
        if (GameSessionRoot.Instance?.Progress?.PhaseData?.SelectedMenus == null) return;
        ApplyMenuList(GameSessionRoot.Instance?.Progress.PhaseData.SelectedMenus);
    }

    private void ApplyMenuList(List<string> savedMenus)
    {
        for (int i = 0; i < Mathf.Min(savedMenus.Count, menuSelections.Length); i++)
        {
            string menuData = savedMenus[i];
            if (string.IsNullOrEmpty(menuData)) continue;

            string[] parts = menuData.Split('|');
            if (parts.Length != 2) continue;

            string mainId = parts[0];
            string[] sideIds = parts[1].Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (!string.IsNullOrEmpty(mainId))
            {
                FoodData mainFood = CatalogProvider.Food?.GetById(mainId);
                if (mainFood != null) menuSelections[i].SetMain(mainFood);
            }

            foreach (string sideId in sideIds)
            {
                FoodData sideFood = CatalogProvider.Food?.GetById(sideId);
                if (sideFood != null) menuSelections[i].AddSide(sideFood);
            }
        }
    }
}
