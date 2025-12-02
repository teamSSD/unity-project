using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(CookingToolBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class CookingToolModel : MonoBehaviour
{
    [SerializeField] private CookingToolData cookingToolData;
    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private SearchFoodUsecase searchFoodUsecase;
    public CookingToolSchema SchemaInstance { get; private set; }
    public CookingToolBehavior BehaviorInstance { get; private set; }
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;

    bool injected = false;

    public void Inject(
        PlayMinigameUsecase playMinigameUsecase,
        SearchRecipeUsecase searchRecipeUsecase,
        SearchFoodUsecase searchFoodUsecases)
    {
        this.playMinigameUsecase = playMinigameUsecase;
        this.searchRecipeUsecase = searchRecipeUsecase;
        this.searchFoodUsecase = searchFoodUsecases;
        injected = true;
    }

    void Awake()
    {
        BehaviorInstance = GetComponent<CookingToolBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        SchemaInstance = new CookingToolSchema(cookingToolData);

        clickStateUtil.OnDragEnd += DetectTrashcan;
        clickStateUtil.OnClicked += PlayMinigame;
        clickStateUtil.OnDragEnd += TransferIngredient;
    }

    void Update()
    {
        BehaviorInstance.isCookable = SchemaInstance.IsCookable();
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= DetectTrashcan;
        clickStateUtil.OnClicked -= PlayMinigame;
        clickStateUtil.OnDragEnd -= TransferIngredient;

    }

    public bool AddIngredient(FoodSchema food)
    {
        if (!injected)
        {
            return false;
        }
        if (SchemaInstance.IsAddable(food))
        {
            SchemaInstance.AddIngredient(food);
            BehaviorInstance.AddTexture(Resources.Load<Sprite>(ResourcePaths.Art.FOOD + food.foodData.imageName));
            return true;
        }
        return false;
    }

    public void TransferIngredient()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }
        CookingToolModel collision = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (collision != null)
        {
            bool reflected = collision.AddIngredient(SchemaInstance.GetResult());
            if (reflected)
            {
                SchemaInstance.ClearIngredient();
                BehaviorInstance.ResetTexture();
            }
        }
    }

    private void DetectTrashcan()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }
        if (scanColliderUtil.GetOverlappingWithTag(Tags.Trashcan.ToString()))
        {
            SchemaInstance.ClearIngredient();
            BehaviorInstance.ResetTexture();
        }
    }

    private void PlayMinigame()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }
        if (SchemaInstance.IsCookable())
        {
            SchemaInstance.MinigameStart();

            RecipeData response = searchRecipeUsecase.Search(
                    SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData));

            StartCoroutine(playMinigameUsecase.PlayCoroutine(
                response,
                (Vector2)this.gameObject.transform.position,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData),
                OnMinigameEnd));
        }
    }

    private void OnMinigameEnd(RecipeData recipeData, float score)
    {        
        if (!injected) return;
        FoodData foodData = searchFoodUsecase.Search(recipeData.outputId);
        SchemaInstance.Cook(foodData, recipeData, score);

        BehaviorInstance.ResetTexture();
        BehaviorInstance.AddTexture(Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName));
    }
}
