using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TempSearchRecipeUsecase : SearchRecipeUsecase
{
    // Key 구조: "도구ID:재료1_재료2_재료3"
    private readonly Dictionary<string, RecipeData> recipeLookup = new Dictionary<string, RecipeData>();
    private readonly Dictionary<string, string> minigameIdToToolId = new Dictionary<string, string>
    {
        { "", "" },
        {"M001", "T001"},
        {"M002", "T002"},
        {"M004", "T003"},
        {"M005", "T003"},
        {"M006", "T004"},
        {"M007", "T005"}
    };

    public TempSearchRecipeUsecase()
    {
        var allRecipes = Resources.LoadAll<RecipeData>("ScriptableObjects/RecipeData");

        foreach (var recipe in allRecipes)
        {
            string key = GenerateLookupKey(minigameIdToToolId[recipe.minigameId], recipe.inputs.Select(i => i.food));
            
            if (!recipeLookup.ContainsKey(key))
            {
                recipeLookup.Add(key, recipe);
            }
            else
            {
                Debug.LogWarning($"중복된 레시피(도구+재료) 발견: {recipe.id}와 {recipeLookup[key].id}");
            }
        }
    }

    // 호출 시 조리 중인 도구의 ID를 함께 전달받아야 합니다.
    public RecipeData Search(string toolId, List<FoodData> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0) return null;

        // 도구 ID와 재료 리스트로 검색 키 생성
        string searchKey = GenerateLookupKey(toolId, ingredients);
        return recipeLookup.TryGetValue(searchKey, out var result) ? result : recipeLookup[":"];
    }

    // 도구 ID와 재료들을 조합해 고유 키 생성
    private string GenerateLookupKey(string toolId, IEnumerable<FoodData> ingredients)
    {
        var sortedIds = ingredients
            .Select(f => f.id)
            .OrderBy(id => id);

        // 형식 예: "Pot:item_egg_item_water"
        return $"{toolId}:{string.Join("_", sortedIds)}";
    }
}