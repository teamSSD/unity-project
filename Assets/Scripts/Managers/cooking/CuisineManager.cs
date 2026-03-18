using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CuisineManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> cookingTools;
    [SerializeField] private GameObject refrigeratorGameObject;
    [SerializeField] private GameObject upperShelfGameObject;
    [SerializeField] private GameObject lowerShelfGameObject;
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Canvas canvas;
    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private LoadInventoryUsecase loadInventoryUsecase;
    private Refrigerator refrigerator;
    private UpperShelf upperShelf;
    private LowerShelf lowerShelf;

    void Start()
    {
        playMinigameUsecase = gameObject.GetComponent<MiniGameManager>();
        searchRecipeUsecase = RecipeDataManager.Instance;
        loadInventoryUsecase = InventoryManager.Instance;

        cookingTools.ForEach(tool =>
                tool.GetComponent<CookingToolModel>()
                        .Inject(playMinigameUsecase, searchRecipeUsecase));

        refrigerator = refrigeratorGameObject.GetComponent<Refrigerator>();
        upperShelf = upperShelfGameObject.GetComponent<UpperShelf>();
        lowerShelf = lowerShelfGameObject.GetComponent<LowerShelf>();

        FillRefrigerator(refrigeratorGameObject);
        FillUpperShelf(upperShelfGameObject);
        FillLowerShelf(lowerShelfGameObject);
    }

    private void FillRefrigerator(GameObject parent)
    {
        loadInventoryUsecase.LoadIngredientsByCategory(IngredientDisplayCategory.Refrigerator).ForEach(data =>
            {
                GameObject ingredientInstance = instantiateFood(parent, data.Item1.ingredientName);
                FoodModel foodModel = settingFoodModel(ingredientInstance, data.Item1, data.Item2);
                refrigerator.AddIngredients(foodModel);
            });
    }

    public void FillUpperShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadIngredientsByCategory(IngredientDisplayCategory.UpperShelf).ForEach(data =>
            {
                GameObject ingredientInstance = instantiateFood(parent, data.Item1.ingredientName);
                FoodModel foodModel = settingFoodModel(ingredientInstance, data.Item1, data.Item2);
                upperShelf.AddIngredients(foodModel);
            });
    }

    public void FillLowerShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadIngredientsByCategory(IngredientDisplayCategory.LowerShelf).ForEach(data =>
            {
                GameObject ingredientInstance = instantiateFood(parent, data.Item1.ingredientName);
                FoodModel foodModel = settingFoodModel(ingredientInstance, data.Item1, data.Item2);
                lowerShelf.AddIngredients(foodModel);
            });
    }

    private GameObject instantiateFood(GameObject parent, string ingredientName)
    {
        GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
        ingredientInstance.name = ingredientName;
        return ingredientInstance;
    }

    private FoodModel settingFoodModel(GameObject instance, FoodData food, IngredientData data)
    {
        FoodModel foodModel = instance.GetComponent<FoodModel>();
        foodModel.Inject(canvas, loadInventoryUsecase, food, data.defaultPrice);
        instance.transform.position = foodModel.GetDefaultPosition();
        return foodModel;
    }
}
