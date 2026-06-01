using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RecipeDataManager와 Cooking 시스템을 연결하는 어댑터
/// RecipeDataManager의 MenuSelection을 MenuSchema로 변환하여 제공
/// </summary>
public class RecipeBookMenuProvider : ISelectMenu
{
    public List<MenuSchema> GetTodaysMenu()
    {
        if (RecipeDataManager.Instance == null || RecipeDataManager.Instance.GetAllMenusAsSchema().Count == 0)
        {
            Debug.LogWarning("[RecipeBookMenuProvider] RecipeDataManager not found or empty! Using fallback: first FoodData from catalog.");
            var allFood = CatalogProvider.Food?.All;
            if (allFood != null && allFood.Count > 0)
            {
                var fallbackMenu = new MenuSchema("디버그 메뉴", 1, allFood[0], new List<FoodData>());
                return new List<MenuSchema> { fallbackMenu };
            }
            return new List<MenuSchema>();
        }

        var menuList = RecipeDataManager.Instance.GetAllMenusAsSchema();
        foreach (var menu in menuList)
        {
            Debug.Log($"  - {menu}");
        }

        return menuList;
    }
}
