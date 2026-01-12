using System.Collections.Generic;

class TempSearchFoodUsecase : SearchFoodUsecase
{
    List<FoodData> foodDatas;

    public TempSearchFoodUsecase()
    {
        foodDatas = CsvModelConverter.Parse<FoodData>("driveAssets/dataTables/food");
    }

    /* null??諛섑솚?????덉쓬*/
    public FoodData Search(string id)
    {
        return foodDatas.Find(foodData => foodData.id == id);
    }
}