using System.Collections.Generic;

[System.Serializable]
public class PhaseData
{
    public int Day;
    public PhaseType Phase;
    public List<string> UnlockedRecipes;

    public PhaseData()
    {
        Day = 1;
        Phase = PhaseType.Preparation;
        UnlockedRecipes = new List<string>();
    }
}