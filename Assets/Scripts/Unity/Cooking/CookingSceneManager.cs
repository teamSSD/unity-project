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
        searchRecipeUsecase = GameSessionRoot.Instance?.RecipeLookup;
        loadInventoryUsecase = GameSessionRoot.Instance?.Inventory;

        if (loadInventoryUsecase == null) return;

        cookingTools.ForEach(tool =>
                tool.GetComponent<CookingToolModel>()
                        .Inject(playMinigameUsecase, searchRecipeUsecase));

        refrigerator = refrigeratorGameObject.GetComponent<Refrigerator>();
        upperShelf = upperShelfGameObject.GetComponent<UpperShelf>();
        lowerShelf = lowerShelfGameObject.GetComponent<LowerShelf>();

        // Composition Root: 씬 컨트롤러가 자식에 capacity 명시 주입
        InjectStorageCapacity(refrigerator, Refrigerator.DefaultCapacity);
        InjectStorageCapacity(upperShelf,   UpperShelf.DefaultCapacity);
        InjectStorageCapacity(lowerShelf,   LowerShelf.DefaultCapacity);

        FillStorage(refrigerator, IngredientDisplayCategory.Refrigerator, refrigeratorGameObject);
        FillStorage(upperShelf, IngredientDisplayCategory.UpperShelf, upperShelfGameObject);
        FillStorage(lowerShelf, IngredientDisplayCategory.LowerShelf, lowerShelfGameObject);

        TryStartCookingTutorial();
    }

    private void TryStartCookingTutorial()
    {
        var tc = TutorialController.Instance;
        if (tc == null || !tc.CanShow(TutorialStepId.CookingIntro)) return;
        tc.Show(TutorialStepId.CookingIntro, onDone: () =>
        {
            // 마지막 파트 (Tab) dismiss 후 — Cooking mock의 상호작용 해제 + 손님 스폰 재활성.
            // Tab 자체는 RecipeBookManager가 감지해서 레시피북 자동 열림.
            var tcc = FindFirstObjectByType<TutorialCookingController>();
            tcc?.ReleaseTutorialLocks();
        });
    }

    private static void InjectStorageCapacity(BaseStorage storage, int fallback)
    {
        var upgradeSvc = GameSessionRoot.Instance?.StorageUpgrade;
        int capacity = upgradeSvc?.GetCurrentData(storage.UpgradeTypeId)?.value ?? fallback;
        storage.Inject(capacity);
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

    /// <summary>튜토리얼 mock refill. 지정 storage에 FoodData로 신규 인스턴스 spawn (price=0).</summary>
    public bool TryTutorialRefill(BaseStorage storage, FoodData food)
    {
        if (storage == null || food == null || foodPrefab == null || loadInventoryUsecase == null) return false;
        GameObject parent = null;
        if (storage == refrigerator) parent = refrigeratorGameObject;
        else if (storage == upperShelf) parent = upperShelfGameObject;
        else if (storage == lowerShelf) parent = lowerShelfGameObject;
        if (parent == null) return false;

        GameObject instance = Instantiate(foodPrefab, parent.transform);
        instance.name = food.ingredientName;
        FoodModel foodModel = instance.GetComponent<FoodModel>();
        foodModel.Inject(canvas, loadInventoryUsecase, food, 0);
        instance.transform.position = foodModel.GetDefaultPosition();
        if (!storage.AddIngredients(foodModel))
        {
            Destroy(instance);
            return false;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
