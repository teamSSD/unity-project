using System.Collections.Generic;

public interface SearchRecipeUsecase
{
    (FoodData, RecipeData) Search(List<FoodData> ingredients);
}