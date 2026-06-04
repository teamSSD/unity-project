using System.Collections.Generic;

/// <summary>
/// 도시락 선택 디스크 직렬화 형식. MenuSelectionService가 자체 보유.
/// PhaseData.SelectedMenus (legacy) 자리에서 분리 (H).
/// </summary>
[System.Serializable]
public class RecipeBookSaveData
{
    public List<string> selectedMenus = new(); // "mainId|side1,side2,..."
}
