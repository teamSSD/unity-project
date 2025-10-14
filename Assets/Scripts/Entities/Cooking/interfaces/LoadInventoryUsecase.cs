using System.Collections.Generic;

public interface LoadInventoryUsecase
{
    List<(FoodData, int)> Load();
    void UseFood(int foodId);
    int CheckStock(int foodId);
}