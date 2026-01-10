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
        searchRecipeUsecase = new TempSearchRecipeUsecase();
        loadInventoryUsecase = new TempLoadInventoryUsecase();

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
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(canvas, loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                refrigerator.AddIngredients(foodModel);
                ingredientInstance.transform.position = foodModel.BehaviorInstance.defaultPosition;
            });
    }

    public void FillUpperShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadIngredientsByCategory(IngredientDisplayCategory.UpperShelf).ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(canvas, loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                upperShelf.AddIngredients(foodModel);
                ingredientInstance.transform.position = foodModel.BehaviorInstance.defaultPosition;
            });
    }

    public void FillLowerShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadIngredientsByCategory(IngredientDisplayCategory.LowerShelf).ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(canvas, loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                lowerShelf.AddIngredients(foodModel);
                ingredientInstance.transform.position = foodModel.BehaviorInstance.defaultPosition;
            });
    }
}
