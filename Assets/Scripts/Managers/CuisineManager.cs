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
    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private SearchFoodUsecase searchFoodUsecase;
    private LoadInventoryUsecase loadInventoryUsecase;
    private Refrigerator refrigerator;
    private UpperShelf upperShelf;
    private LowerShelf lowerShelf;

    void Start()
    {
        playMinigameUsecase = new TempPlayMinigameUsecase();
        searchRecipeUsecase = new TempSearchRecipeUsecase();
        searchFoodUsecase = new TempSearchFoodUsecase();
        loadInventoryUsecase = new TempLoadInventoryUsecase(searchFoodUsecase);

        cookingTools.ForEach(tool =>
                tool.GetComponent<CookingToolModel>()
                        .Inject(playMinigameUsecase, searchRecipeUsecase, searchFoodUsecase));

        refrigerator = refrigeratorGameObject.GetComponent<Refrigerator>();
        upperShelf = upperShelfGameObject.GetComponent<UpperShelf>();
        lowerShelf = lowerShelfGameObject.GetComponent<LowerShelf>();

        FillRefrigerator(refrigeratorGameObject);
        FillUpperShelf(upperShelfGameObject);
        FillLowerShelf(lowerShelfGameObject);
    }

    private void FillRefrigerator(GameObject parent)
    {
        loadInventoryUsecase.LoadRefrigeratorIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                refrigerator.AddIngredients(foodModel);
            });
    }

    public void FillUpperShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadUpperShelfIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                upperShelf.AddIngredients(foodModel);
            });
    }

    public void FillLowerShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadLowerShelfIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                lowerShelf.AddIngredients(foodModel);
            });
    }
}
