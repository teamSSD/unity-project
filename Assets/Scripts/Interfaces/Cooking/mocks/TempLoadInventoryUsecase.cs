using System.Collections.Generic;
using System.Linq;

class TempLoadInventoryUsecase : LoadInventoryUsecase
{
    private SearchFoodUsecase searchFoodUsecase;
    private List<(IngredientData, int)> ingredients;

    public TempLoadInventoryUsecase(SearchFoodUsecase searchFoodUsecase)
    {
        this.searchFoodUsecase = searchFoodUsecase;

        ingredients = CsvModelConverter.Parse<IngredientData>("driveAssets/dataTables/ingredient")
                .ConvertAll(ingredient => (ingredient, 10));
    }
    public (IngredientData, int) Search(string id)
    {
        return ingredients.Find(ingredient => ingredient.Item1.id == id);
    }

    public void ConsumeFood(string foodId, int amount)
    {
        (IngredientData, int) found = ingredients.Find(ingredient => ingredient.Item1.id == foodId);
        found.Item2 = found.Item2 - amount;
    }
    public int CheckStockAmount(string foodId)
    {
        return ingredients.Find(ingredient => ingredient.Item1.id == foodId).Item2;
    }
    public List<(FoodData, IngredientData)> LoadRefrigeratorIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.Refrigerator)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
    public List<(FoodData, IngredientData)> LoadUpperShelfIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.UpperShelf)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
    public List<(FoodData, IngredientData)> LoadLowerShelfIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.LowerShelf)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
}