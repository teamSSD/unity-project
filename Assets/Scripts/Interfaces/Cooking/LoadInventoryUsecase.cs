using System.Collections.Generic;

public interface LoadInventoryUsecase
{
    void ConsumeFood(string foodId, int amount);
    int CheckStockAmount(string foodId);
    List<(FoodData, IngredientData)> LoadIngredientByCategory(IngredientDisplayCategory category);
}