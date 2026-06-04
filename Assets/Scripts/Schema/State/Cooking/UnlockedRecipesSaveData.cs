using System.Collections.Generic;

/// <summary>
/// 해금 레시피 ID 목록 디스크 형식. UnlockedFoodService가 자체 보유.
/// PhaseData.UnlockedRecipes (legacy) 자리에서 분리 (H).
/// </summary>
[System.Serializable]
public class UnlockedRecipesSaveData
{
    public List<string> recipeIds = new();
}
