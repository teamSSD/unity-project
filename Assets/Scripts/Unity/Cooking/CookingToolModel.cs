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
        clickStateUtil.OnDragEnd += OnToolDropped;
    }

    void Start()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.RegisterCollider($"cooking.tool.{GetToolId()}", GetComponent<Collider2D>());
#endif
        if (tooltipController != null && tooltipController.GetTooltipObject() != null)
        {
            cookingToolDescriptionScript = tooltipController.GetTooltipObject().GetComponent<CookingToolDescription>();
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

        string title = "요리 도구 : " + SchemaInstance.cookingToolData.cookerName;
        string body;
        if (SchemaInstance.GetResult() == null)
        {
            var names = SchemaInstance.Ingredients.Select(i => i.foodData.ingredientName);
            body = "내용물 : " + string.Join(", ", names);
        }
        else
        {
            body = "내용물 : " + SchemaInstance.GetResult().foodData.ingredientName + "(요리됨)";
        }
        cookingToolDescriptionScript.SetTexts(title, body);
        tooltipController.RequestPositionNear(GetComponent<SpriteRenderer>().bounds, forceRight: true);
    }

    void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.UnregisterCollider($"cooking.tool.{GetToolId()}", GetComponent<Collider2D>());
#endif
        clickStateUtil.OnDragEnd -= DetectTrashcan;
        clickStateUtil.OnClicked -= PlayMinigame;
        clickStateUtil.OnDragEnd -= OnToolDropped;
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
    /// <summary>
    /// 드래그 종료 시 단일 dispatched 진입점. 이전엔 TransferIngredient + AddToBento 두 핸들러가
    /// 각자 OnDragEnd에 구독해 독립 스캔했음 (review_2026-07_deepdig.md D "다중-구독" 온상).
    /// FoodModel.OnFoodDropped와 동일 패턴.
    ///
    /// 우선순위: CookingTool(전이) → Bento. 이전 구독 순서(TransferIngredient → AddToBento) 보존.
    /// </summary>
    private void OnToolDropped()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        // 1) 다른 CookingTool로 전이 시도 — 이전 TransferIngredient.
        var otherTool = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (otherTool != null && TryTransferToTool(otherTool)) return;

        // 2) Bento로 결과물 전이 시도 — 이전 AddToBento. 완성된 요리(GetResult()!=null)만.
        var bento = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (bento != null) TryTransferToBento(bento);
    }

    private bool TryTransferToTool(CookingToolModel target)
    {
        var cooked = SchemaInstance.GetResult();
        if (cooked != null)
        {
            if (target.AddIngredient(cooked))
            {
                ClearSelfAfterTransfer();
                return true;
            }
        }
        else if (SchemaInstance.Ingredients.Count != 0
                && SchemaInstance.Ingredients.All(ing => target.SchemaInstance.IsAddable(ing)))
        {
            // IsAddable 사전 검증이 all-pass 이므로 개별 AddIngredient도 모두 성공 가정.
            SchemaInstance.Ingredients.ForEach(ing => target.AddIngredient(ing));
            ClearSelfAfterTransfer();
            return true;
        }
        return false;
    }

    private bool TryTransferToBento(BentoModel target)
    {
        var cooked = SchemaInstance.GetResult();
        if (cooked == null) return false;
        if (target.AddIngredient(cooked))
        {
            ClearSelfAfterTransfer();
            return true;
        }
        return false;
    }

    private void ClearSelfAfterTransfer()
    {
        SchemaInstance.ClearIngredient();
        BehaviorInstance.ResetTexture();
    }

    public string GetToolId()
    {
        return SchemaInstance.cookingToolData.id;
    }

#if AFTERTASTE_E2E
    /// <summary>WebGL E2E 조리 상태 관측값.</summary>
    public int E2EIngredientCount => SchemaInstance?.Ingredients?.Count ?? 0;
    public bool E2EIsCookable => SchemaInstance?.IsCookable() ?? false;
    public string E2EResultFoodId => SchemaInstance?.GetResult()?.foodData?.id ?? string.Empty;
    public string[] E2EIngredientFoodIds => SchemaInstance?.Ingredients?
        .Where(food => food?.foodData != null)
        .Select(food => food.foodData.id)
        .ToArray() ?? System.Array.Empty<string>();

    /// <summary>실제 도구 드롭과 동일한 전이 규칙/시각 갱신 경로를 호출한다.</summary>
    public bool E2ETransferToTool(CookingToolModel target) =>
        injected && target != null && !UILockManager.IsLocked && TryTransferToTool(target);
#endif

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
        // 튜토리얼 등 UI 잠금 중엔 조리 시작 불가.
        if (UILockManager.IsLocked) return;
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
