using System.Collections.Generic;

class TempSearchFoodUsecase : SearchFoodUsecase
{
    List<FoodData> foodDatas;

    public TempSearchFoodUsecase()
    {
        foodDatas = CsvModelConverter.Parse<FoodData>("driveAssets/dataTables/food");
    }

    /* null을 반환할 수 있음*/
    public FoodData Search(string id)
    {
        return foodDatas.Find(foodData => foodData.id == id);
    }
}