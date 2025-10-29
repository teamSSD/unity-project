public class FoodSchema
{
    public FoodData foodData;
    public int Price { get; private set; }

    public FoodSchema(FoodData foodData, int price)
    {
        this.foodData = foodData;
        this.Price = price;
    }

    public bool IsSameFood(FoodSchema food)
    {
        return foodData.id == food.foodData.id;
    }
}