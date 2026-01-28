using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CookingToolBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class CookingToolModel : MonoBehaviour
{
    [SerializeField] private CookingToolData cookingToolData;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject descriptionPrefab;
    [SerializeField] private string toolId;
    [SerializeField] private AudioClip trashcanSfx;
    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private CookingToolSchema SchemaInstance;
    private CookingToolBehavior BehaviorInstance;
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    private HoverStateUtil hoverStateUtil;
    private float hoverThreshold = 1.0f;
    private GameObject descriptionObject;
    private CookingToolDescription cookingToolDescriptionScript;
    private float hoverClock = 0;

    bool injected = false;

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
        hoverStateUtil = GetComponent<HoverStateUtil>();

        SchemaInstance = new CookingToolSchema(cookingToolData);

        clickStateUtil.OnDragEnd += DetectTrashcan;
        clickStateUtil.OnClicked += PlayMinigame;
        clickStateUtil.OnDragEnd += TransferIngredient;
        clickStateUtil.OnDragEnd += AddToBento;
    }

    void Start()
    {
        descriptionObject = Instantiate(descriptionPrefab, canvas.transform);
        cookingToolDescriptionScript = descriptionObject.GetComponent<CookingToolDescription>();
        cookingToolDescriptionScript.setName(SchemaInstance.cookingToolData.cookerName);
        descriptionObject.SetActive(false);
    }

    void Update()
    {
        BehaviorInstance.isCookable = SchemaInstance.IsCookable();

        if (clickStateUtil.getState() == ClickState.None && hoverStateUtil.IsHovering())
        {
            if (hoverClock < hoverThreshold && hoverClock + Time.deltaTime >= hoverThreshold)
            {
                descriptionObject.SetActive(true);
                if (SchemaInstance.GetResult() == null)
                {
                    var ingredientNames = SchemaInstance.Ingredients
                        .Select(i => i.foodData.ingredientName)
                        .ToList();

                    cookingToolDescriptionScript.setIngredients(ingredientNames);

                    var screenPos = Camera.main.WorldToScreenPoint(transform.position);
                    float offsetX = screenPos.x < Screen.width * 0.5f ? 3f : -3f;
                    descriptionObject.transform.position =
                        Camera.main.WorldToScreenPoint(transform.position + new Vector3(offsetX, 0, 0));
                }
                else
                {
                    cookingToolDescriptionScript.setResult(SchemaInstance.GetResult().foodData.ingredientName);
                }
            }
            hoverClock += Time.deltaTime;
            return;
        }
        hoverClock = 0;
        if (descriptionObject != null)
        {
            descriptionObject.SetActive(false);
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
            BehaviorInstance.AddTexture(food.foodData.image);
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
        BehaviorInstance.locked = true;
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }
        if (SchemaInstance.IsCookable())
        {
            SchemaInstance.MinigameStart();

            RecipeData response = searchRecipeUsecase.Search(
                SchemaInstance.cookingToolData.id,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData));

            StartCoroutine(playMinigameUsecase.PlayCoroutine(
                toolId,
                response,
                (Vector2)this.gameObject.transform.position,
                SchemaInstance.Ingredients.ConvertAll(ingredient => ingredient.foodData),
                OnMinigameEnd));
        }
    }

    private void OnMinigameEnd(RecipeData recipeData, float score)
    {
        if (!injected) return;
        BehaviorInstance.locked = false;
        FoodData foodData = recipeData.outputFood;
        SchemaInstance.Cook(foodData, recipeData, score);

        BehaviorInstance.ResetTexture();
        BehaviorInstance.AddTexture(foodData.image);
    }
}
