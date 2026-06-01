using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CookingSceneManager : MonoBehaviour
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
        searchRecipeUsecase = RecipeLookupService.Instance;
        loadInventoryUsecase = InventoryManager.Instance;

        if (loadInventoryUsecase == null) return;

        cookingTools.ForEach(tool =>
                tool.GetComponent<CookingToolModel>()
                        .Inject(playMinigameUsecase, searchRecipeUsecase));

        refrigerator = refrigeratorGameObject.GetComponent<Refrigerator>();
        upperShelf = upperShelfGameObject.GetComponent<UpperShelf>();
        lowerShelf = lowerShelfGameObject.GetComponent<LowerShelf>();

        FillStorage(refrigerator, IngredientDisplayCategory.Refrigerator, refrigeratorGameObject);
        FillStorage(upperShelf, IngredientDisplayCategory.UpperShelf, upperShelfGameObject);
        FillStorage(lowerShelf, IngredientDisplayCategory.LowerShelf, lowerShelfGameObject);
    }

    private void FillStorage(BaseStorage storage, IngredientDisplayCategory category, GameObject parent)
    {
        var items = loadInventoryUsecase.LoadIngredientsByCategory(category);
        foreach (var data in items)
        {
            if (storage.IsFull) break;
            GameObject ingredientInstance = instantiateFood(parent, data.Item1.ingredientName);
            FoodModel foodModel = settingFoodModel(ingredientInstance, data.Item1, data.Item2);
            if (!storage.AddIngredients(foodModel))
                Destroy(ingredientInstance);
        }
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
