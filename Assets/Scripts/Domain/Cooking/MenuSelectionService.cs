using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Cooking
{
    /// <summary>
    /// 도시락 메뉴 선택 상태 (POCO Service). RecipeDataManager facade 후속.
    /// 아침/점심/저녁 도시락 3개 슬롯 + Save/Load (gamedata.json recipeBook 슬롯).
    /// </summary>
    public class MenuSelectionService
    {
        private MenuSelection[] menuSelections;
        private readonly List<FoodData> _allFoodData;

        public MenuSelectionService(IEnumerable<FoodData> catalog)
        {
            _allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
            InitializeMenus();
        }

        private FoodData FoodById(string id) => _allFoodData.Find(f => f.id == id);

        private void InitializeMenus()
        {
            menuSelections = new MenuSelection[3];
            menuSelections[0] = new MenuSelection("도시락 1");
            menuSelections[1] = new MenuSelection("도시락 2");
            menuSelections[2] = new MenuSelection("도시락 3");
        }

        public MenuSelection GetMenu(int index)
        {
            if (menuSelections == null) InitializeMenus();
            if (index < 0 || index >= menuSelections.Length)
            {
                Debug.LogError($"[MenuSelectionService] Invalid menu index: {index}");
                return null;
            }
            return menuSelections[index];
        }

        public List<MenuSchema> GetAllMenusAsSchema()
        {
            var menuList = new List<MenuSchema>();
            for (int i = 0; i < menuSelections.Length; i++)
            {
                var menu = menuSelections[i];
                if (menu.HasSelection())
                {
                    menuList.Add(new MenuSchema(
                        menu.Name,
                        i + 1,
                        menu.MainMenu,
                        new List<FoodData>(menu.SideMenus)
                    ));
                }
            }
            return menuList;
        }

        public void ClearAllMenus()
        {
            foreach (var menu in menuSelections) menu.Clear();
        }

        public bool HasAnySelection()
        {
            if (menuSelections == null)
            {
                InitializeMenus();
                return false;
            }
            foreach (var menu in menuSelections)
                if (menu.HasSelection()) return true;
            return false;
        }

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

        public void ApplySaveData(RecipeBookSaveData data)
        {
            if (data == null || data.selectedMenus == null) return;
            ApplyMenuList(data.selectedMenus);
        }

        /// <summary>Legacy fallback: PhaseData.SelectedMenus(piggyback) 폐기 전 데이터 호환.</summary>
        public void LoadMenusFromProgressLegacy(PhaseData pd)
        {
            if (pd?.SelectedMenus == null) return;
            ApplyMenuList(pd.SelectedMenus);
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
                    FoodData mainFood = FoodById(mainId);
                    if (mainFood != null) menuSelections[i].SetMain(mainFood);
                }

                foreach (string sideId in sideIds)
                {
                    FoodData sideFood = FoodById(sideId);
                    if (sideFood != null) menuSelections[i].AddSide(sideFood);
                }
            }
        }
    }
}
