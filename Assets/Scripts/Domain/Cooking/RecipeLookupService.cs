using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Domain.Cooking
{
    /// <summary>
    /// 레시피 검색 (POCO Service). 도구 + 재료 조합 → RecipeData 조회.
    /// 미니게임ID ↔ 도구ID 매핑. RecipeLookupService(global MonoBehaviour) facade 후속.
    /// </summary>
    public class RecipeLookupService : SearchRecipeUsecase
    {
        private readonly Dictionary<string, RecipeData> recipeLookup = new Dictionary<string, RecipeData>();
        private readonly List<RecipeData> allRecipes;

        private static readonly Dictionary<string, string> minigameIdToToolId = new Dictionary<string, string>
        {
            { "", "" },
            { "M001", "T001" },
            { "M002", "T002" },
            { "M004", "T003" },
            { "M005", "T003" },
            { "M006", "T004" },
            { "M007", "T005" }
        };

        public RecipeLookupService(IEnumerable<RecipeData> catalog)
        {
            allRecipes = catalog != null ? new List<RecipeData>(catalog) : new List<RecipeData>();
            BuildLookupTable();
        }

        private void BuildLookupTable()
        {
            recipeLookup.Clear();
            foreach (var recipe in allRecipes)
            {
                if (!minigameIdToToolId.TryGetValue(recipe.minigameId, out string toolId))
                {
                    Debug.LogWarning($"[RecipeLookupService] No toolId mapping for minigameId '{recipe.minigameId}' in recipe '{recipe.id}'");
                    continue;
                }
                string key = GenerateLookupKey(toolId, recipe.inputs.Select(i => i.food));
                if (!recipeLookup.ContainsKey(key))
                    recipeLookup.Add(key, recipe);
                else
                    Debug.LogWarning($"[RecipeLookupService] Duplicate recipe key '{key}': {recipe.id} conflicts with {recipeLookup[key].id}");
            }
        }

        public RecipeData Search(string toolId, List<FoodData> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return null;
            string searchKey = GenerateLookupKey(toolId, ingredients);
            if (recipeLookup.TryGetValue(searchKey, out RecipeData result)) return result;
            if (recipeLookup.TryGetValue(":", out RecipeData fallback)) return fallback;
            return null;
        }

        private string GenerateLookupKey(string toolId, IEnumerable<FoodData> ingredients)
        {
            var sortedIds = ingredients.Select(f => f.id).OrderBy(id => id);
            return $"{toolId}:{string.Join("_", sortedIds)}";
        }

        public string GetToolIdForMinigame(string minigameId)
        {
            if (minigameIdToToolId.TryGetValue(minigameId, out string toolId)) return toolId;
            return null;
        }

        public List<RecipeData> GetAllRecipes()
        {
            return allRecipes != null ? new List<RecipeData>(allRecipes) : new List<RecipeData>();
        }

        public List<RecipeData> GetRecipesForTool(string toolId)
        {
            if (allRecipes == null) return new List<RecipeData>();
            return allRecipes.Where(r =>
            {
                if (minigameIdToToolId.TryGetValue(r.minigameId, out string mappedToolId))
                    return mappedToolId == toolId;
                return false;
            }).ToList();
        }
    }
}
