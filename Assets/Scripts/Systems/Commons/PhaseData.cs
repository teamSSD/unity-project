using System.Collections.Generic;

[System.Serializable]
public class PhaseData
{
    public int Day;
    public PhaseType Phase;
    public List<string> UnlockedRecipes;

    // 각 페이즈별 선택된 메뉴 (Morning, Lunch, Dinner)
    public List<string> SelectedMenus;

    public PhaseData()
    {
        Day = 1;
        Phase = PhaseType.Preparation;
        UnlockedRecipes = new List<string>();
        SelectedMenus = new List<string>(); // 3개 (아침, 점심, 저녁)
    }
}