using System.Collections.Generic;

public interface SearchRecipeUsecase
{
    RecipeData Search(string toolId, List<FoodData> ingredients);
}