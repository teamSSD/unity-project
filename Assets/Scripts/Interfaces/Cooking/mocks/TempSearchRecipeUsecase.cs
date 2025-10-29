using System.Collections.Generic;
using System.Linq;

class TempSearchRecipeUsecase : SearchRecipeUsecase
{
    List<RecipeData> recipes;

    public TempSearchRecipeUsecase()
    {
        recipes = CsvModelConverter.Parse<RecipeData>("driveAssets/dataTables/recipe");
    }

    public RecipeData Search(List<FoodData> ingredients)
    {
        ISet<string> ingredientIdSet = new HashSet<string>(ingredients.Select(i => i.id));
        return recipes.FirstOrDefault(recipe =>
                recipe.inputInfoSet.Count == ingredientIdSet.Count
                && ingredientIdSet.SetEquals(recipe.inputInfoSet.Select(info => info.foodId)))
                ?? new RecipeData("R000", "I000", "NONE", new HashSet<RecipeIngredient>());
    }
}