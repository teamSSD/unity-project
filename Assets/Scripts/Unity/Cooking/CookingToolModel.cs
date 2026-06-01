using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CookingToolBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TooltipController))]
[DisallowMultipleComponent]
public class CookingToolModel : MonoBehaviour
{
    [SerializeField] private CookingToolData cookingToolData;
    [SerializeField] private string toolId;
    [SerializeField] private AudioClip trashcanSfx;

    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private CookingToolSchema SchemaInstance;
    private CookingToolBehavior BehaviorInstance;
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    private TooltipController tooltipController;
    private CookingToolDescription cookingToolDescriptionScript;

    private bool injected = false;

    public void Inject(
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
        tooltipController = GetComponent<TooltipController>();

        SchemaInstance = new CookingToolSchema(cookingToolData);

        clickStateUtil.OnDragEnd += DetectTrashcan;
        clickStateUtil.OnClicked += PlayMinigame;
        clickStateUtil.OnDragEnd += TransferIngredient;
        clickStateUtil.OnDragEnd += AddToBento;
    }

    void Start()
    {
        if (tooltipController != null && tooltipController.GetTooltipObject() != null)
        {
            cookingToolDescriptionScript = tooltipController.GetTooltipObject().GetComponent<CookingToolDescription>();
            cookingToolDescriptionScript.setName(SchemaInstance.cookingToolData.cookerName);
            tooltipController.RegisterContentUpdater(UpdateTooltipContent);
        }
    }

    void Update()
    {
        BehaviorInstance.isCookable = SchemaInstance.IsCookable();
    }

    /// <summary>
    /// Update tooltip content when displayed
    /// </summary>
    private void UpdateTooltipContent()
    {
        if (cookingToolDescriptionScript == null || SchemaInstance == null) return;

        if (SchemaInstance.GetResult() == null)
        {
            // Show ingredients
            var ingredientNames = SchemaInstance.Ingredients
                .Select(i => i.foodData.ingredientName)
                .ToList();

            cookingToolDescriptionScript.setIngredients(ingredientNames);
            tooltipController.RequestPositionNear(GetComponent<SpriteRenderer>().bounds);
        }
        else
        {
            cookingToolDescriptionScript.setResult(SchemaInstance.GetResult().foodData.ingredientName);
            tooltipController.RequestPositionNear(GetComponent<SpriteRenderer>().bounds);
        }
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= DetectTrashcan;
        clickStateUtil.OnClicked -= PlayMinigame;
        clickStateUtil.OnDragEnd -= TransferIngredient;
        clickStateUtil.OnDragEnd -= AddToBento;
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
            string toolId = SchemaInstance.cookingToolData.id;
            Sprite variantSprite = food.foodData.GetImageForTool(toolId);
            BehaviorInstance.AddTexture(variantSprite);
            return true;
        }
        return false;
    }
    public void AddToBento()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        BentoModel collision = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (collision != null)
        {
            if (SchemaInstance.GetResult() != null)
            {
                bool reflected = collision.AddIngredient(SchemaInstance.GetResult());
                if (reflected)
                {
                    SchemaInstance.ClearIngredient();
                    BehaviorInstance.ResetTexture();
                }
            }
        }
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
            if (SchemaInstance.GetResult() != null)
            {
                bool reflected = collision.AddIngredient(SchemaInstance.GetResult());
                if (reflected)
                {
                    SchemaInstance.ClearIngredient();
                    BehaviorInstance.ResetTexture();
                }
            }
            else if (SchemaInstance.Ingredients.Count != 0
                    && SchemaInstance.Ingredients.All(ingredient => collision.SchemaInstance.IsAddable(ingredient)))
            {
                SchemaInstance.Ingredients.ForEach(ingredient =>collision.AddIngredient(ingredient));
                SchemaInstance.ClearIngredient();
                BehaviorInstance.ResetTexture();
            }
        }
    }

    public string GetToolId()
    {
        return SchemaInstance.cookingToolData.id;
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

            SoundManager.Instance.Play2DSFX(trashcanSfx, 0.6f);
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
            BehaviorInstance.locked = true;
            SchemaInstance.MinigameStart();

            RecipeData response = searchRecipeUsecase.Search(
                SchemaInstance.cookingToolData.id,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData));

            playMinigameUsecase.PlayAsync(
                toolId,
                response,
                (Vector2)this.gameObject.transform.position,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData),
                OnMinigameEnd,
                this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private void OnMinigameEnd(RecipeData recipeData, float score)
    {
        if (!injected) return;
        BehaviorInstance.locked = false;
        FoodData foodData = recipeData.outputFood;
        int chainDepth = SearchDataUtil.GetChainDepth(foodData.id);
        SchemaInstance.Cook(foodData, recipeData, score, chainDepth);

        BehaviorInstance.ResetTexture();
        string toolId = SchemaInstance.cookingToolData.id;
        Sprite variantSprite = foodData.GetImageForTool(toolId);
        BehaviorInstance.AddTexture(variantSprite);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
