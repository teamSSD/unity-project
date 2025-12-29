using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

class TempLoadInventoryUsecase : LoadInventoryUsecase
{
    private SearchFoodUsecase searchFoodUsecase;
    private Dictionary<string, (IngredientData data, int count)> ingredients;

    public TempLoadInventoryUsecase(SearchFoodUsecase searchFoodUsecase)
    {
        this.searchFoodUsecase = searchFoodUsecase;

        ingredients = CsvModelConverter.Parse<IngredientData>("driveAssets/dataTables/ingredient")
            .ToDictionary(
                i => i.id,
                i => (i, 10)
            );
    }
    public (IngredientData, int) Search(string id)
    {
        return ingredients[id];
    }

    public void ConsumeFood(string foodId, int amount)
    {
        if (!ingredients.ContainsKey(foodId)) return;

        var item = ingredients[foodId];
        item.count -= amount;
        ingredients[foodId] = item;
    }
    
    public int CheckStockAmount(string foodId)
    {
        return ingredients.TryGetValue(foodId, out var item)
            ? item.count
            : 0;
    }
    public List<(FoodData, IngredientData)> LoadIngredientByCategory(
        IngredientDisplayCategory category
    )
    {
        return ingredients.Values
            .Where(item => item.data.display == category)
            .Select(item => (
                searchFoodUsecase.Search(item.data.id),
                item.data
            ))
            .ToList();
    }
}