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
        // RecipeDataManager 접근
        if (RecipeDataManager.Instance == null)
        {
            Debug.LogError("[RecipeBookMenuProvider] RecipeDataManager not found! Returning empty menu.");
            return new List<MenuSchema>();
        }

        var menuList = RecipeDataManager.Instance.GetAllMenusAsSchema();

        if (menuList.Count == 0)
        {
            Debug.LogError("[RecipeBookMenuProvider] No menus selected! Please select menus before entering Cooking scene.");
        }
        else
        {
            Debug.Log($"[RecipeBookMenuProvider] GetTodaysMenu() returned {menuList.Count} menus");
            foreach (var menu in menuList)
            {
                Debug.Log($"  - {menu}");
            }
        }

        return menuList;
    }
}
