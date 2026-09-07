using System.Collections.Generic;
using Game.Schema.State;
using UnityEngine;

namespace Game.Domain.Cooking
{
    /// <summary>
    /// 도시락 메뉴 선택 상태 (POCO Service). RecipeDataManager facade 후속.
    /// 아침/점심/저녁 도시락 3개 슬롯 + Save/Load (gamedata.json recipeBook 슬롯).
    /// </summary>
    public class MenuSelectionService
    {
        private readonly MenuSelectionState _state;
        private MenuSelection[] Menus => _state.Menus;
        private readonly List<FoodData> _allFoodData;

        public MenuSelectionService(MenuSelectionState state, IEnumerable<FoodData> catalog)
        {
            _state = state ?? throw new System.ArgumentNullException(nameof(state));
            _allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
            if (Menus == null || Menus.Length != 3) InitializeMenus();
        }

        private FoodData FoodById(string id) => _allFoodData.Find(f => f.id == id);

        private void InitializeMenus()
        {
            _state.Menus = new MenuSelection[3];
            Menus[0] = new MenuSelection("도시락 1");
            Menus[1] = new MenuSelection("도시락 2");
            Menus[2] = new MenuSelection("도시락 3");
        }

        public MenuSelection GetMenu(int index)
        {
            if (index < 0 || index >= Menus.Length)
            {
                Debug.LogError($"[MenuSelectionService] Invalid menu index: {index}");
                return null;
            }
            return Menus[index];
        }

        public List<MenuSchema> GetAllMenusAsSchema()
        {
            var menuList = new List<MenuSchema>();
            for (int i = 0; i < Menus.Length; i++)
            {
                var menu = Menus[i];
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
            foreach (var menu in Menus) menu.Clear();
        }

        public bool HasAnySelection()
        {
            foreach (var menu in Menus)
                if (menu.HasSelection()) return true;
            return false;
        }

        /// <summary>Side만 있고 Main 없는 슬롯의 (0-based) 인덱스 반환. Confirm 검증용.</summary>
        public List<int> GetInvalidSlotIndices()
        {
            var invalid = new List<int>();
            for (int i = 0; i < Menus.Length; i++)
            {
                if (Menus[i] != null && Menus[i].HasSideOnly())
                    invalid.Add(i);
            }
            return invalid;
        }

        public RecipeBookSaveData GetSaveData()
        {
            var data = new RecipeBookSaveData();
            for (int i = 0; i < Menus.Length; i++)
            {
                var menu = Menus[i];
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
            InitializeMenus();
            for (int i = 0; i < Mathf.Min(savedMenus.Count, Menus.Length); i++)
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
                    if (mainFood != null) Menus[i].SetMain(mainFood);
                }

                foreach (string sideId in sideIds)
                {
                    FoodData sideFood = FoodById(sideId);
                    if (sideFood != null) Menus[i].AddSide(sideFood);
                }
            }
        }
    }
}
