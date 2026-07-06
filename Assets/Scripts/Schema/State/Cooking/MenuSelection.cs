using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>Main 없이 Side만 있는 상태 — 도시락으로 성립 불가. Confirm 시 팝업 트리거.</summary>
    public bool HasSideOnly()
    {
        return MainMenu == null && SideMenus != null && SideMenus.Count > 0;
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
        if (SideMenus.Count < 3)
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
