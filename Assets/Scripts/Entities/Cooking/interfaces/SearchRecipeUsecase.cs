using System.Collections.Generic;

public interface SearchRecipeUsecase
{
    RecipeData Search(List<FoodData> ingredients);
}