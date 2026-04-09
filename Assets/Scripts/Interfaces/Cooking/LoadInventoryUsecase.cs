using System.Collections.Generic;

public interface LoadInventoryUsecase
{
    // /* Deprecated */ (IngredientData, int) Search(string id);// 특정 음식(또는 재료)의 재고 확인
    int CheckStockAmount(FoodData food);
    
    // 재고 소모
    void ConsumeFood(FoodData food, int amount);
    
    // 재고 추가
    void AddFood(FoodData food, int amount);

    // 카테고리별 로드 (IngredientData가 있는 FoodData만 필터링)
    List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category);
}
