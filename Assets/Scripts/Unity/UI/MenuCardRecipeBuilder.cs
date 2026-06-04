using System.Collections.Generic;

/// <summary>
/// 레시피 체인 DFS bottom-up + 재료 BFS 수집. MenuCardController에서 추출.
/// </summary>
public static class MenuCardRecipeBuilder
{
    /// <summary>최종 음식 ID 기준 레시피 체인 (가장 기본 단계 → 최종 요리 순).</summary>
    public static List<RecipeData> BuildRecipeChain(string foodId)
    {
        var result = new List<RecipeData>();
        var visited = new HashSet<string>();
        CollectRecipes(foodId, result, visited);
        return result;
    }

    private static void CollectRecipes(string foodId, List<RecipeData> result, HashSet<string> visited)
    {
        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(foodId);
        if (recipe == null) return;
        if (!visited.Add(recipe.id)) return;

        foreach (var input in recipe.inputs)
            CollectRecipes(input.food.id, result, visited);

        result.Add(recipe);
    }

    /// <summary>음식에서 사용된 원재료(INGREDIENT)만 BFS로 추출 (중복 제거).</summary>
    public static List<FoodData> GetUniqueIngredients(string foodId)
    {
        var ingredients = new List<FoodData>();
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(foodId);

        while (queue.Count > 0)
        {
            string currentId = queue.Dequeue();
            if (!visited.Add(currentId)) continue;

            FoodData data = SearchDataUtil.GetFoodDataById(currentId);
            if (data == null) continue;

            if (data.type == FoodType.INGREDIENT)
            {
                ingredients.Add(data);
            }
            else
            {
                RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(currentId);
                if (recipe != null)
                {
                    foreach (var input in recipe.inputs)
                        queue.Enqueue(input.food.id);
                }
            }
        }
        return ingredients;
    }
}
