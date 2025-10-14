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
    public CookingToolSchema SchemaInstance { get; private set; }
    public CookingToolBehavior BehaviorInstance { get; private set; }
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;

    bool injected = false;

    public void InjectInterface(
        PlayMinigameUsecase playMinigameUsecase,
        SearchRecipeUsecase searchRecipeUsecase)
    {
        this.playMinigameUsecase = playMinigameUsecase;
        this.searchRecipeUsecase = searchRecipeUsecase;
        injected = true;
    }

    void Awake()
    {
        BehaviorInstance = GetComponent<CookingToolBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragEnd += DetectTrashcan;
        clickStateUtil.OnClicked += PlayMinigame;
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= DetectTrashcan;
        clickStateUtil.OnClicked -= PlayMinigame;

    }

    public bool AddIngredient(FoodSchema food)
    {
        if (!injected) return false;
        if (SchemaInstance.IsAddable(food))
        {
            AddIngredient(food);
            return true;
        }
        return false;
    }

    private void DetectTrashcan()
    {
        if (!injected) return;
        if (scanColliderUtil.GetOverlappingWithTag(Tags.Trashcan.ToString()))
        {
            SchemaInstance.ClearIngredient();
        }
    }

    private void PlayMinigame()
    {
        if (!injected) return;
        if (SchemaInstance.IsCookable())
        {
            SchemaInstance.MinigameStart();
            playMinigameUsecase.PlayCoroutine(
                (Vector2) this.gameObject.transform.position,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData),
                OnMinigameEnd);
        }
    }

    private void OnMinigameEnd(float score)
    {
        if (!injected) return;
        (FoodData, RecipeData) response = searchRecipeUsecase.Search(
            SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData));
        SchemaInstance.Cook(response.Item1, response.Item2, score);
    }
}
