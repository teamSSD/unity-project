using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(SpriteStackRenderer))]
public class CookingTool : MonoBehaviour
{
    public CookingToolData cookingToolData;
    public Vector3 defaultPosition;
    public int maxIngredientSize = 7;

    private Camera mainCamera;
    private float speed = 10f;
    ClickStateUtil clickStateUtil;
    HoverStateUtil hoverStateUtil;
    private Animator animator;
    private ScanColliderUtil scanColliderUtil;
    private SpriteStackRenderer spriteStackRenderer;
    private bool dragging = false;
    private List<IngredientData> ingredients;
    public GameObject manager;
    public IngredientData failure;
    private IngredientData result = null;

    [Header("Ingredient Visuals")]
    [SerializeField] private Transform ingredientVisualRoot;
    [SerializeField] private Vector2 stackStart = new Vector2(0f, 0.1f);
    [SerializeField] private Vector2 stackStep = new Vector2(0.08f, 0.04f);
    [SerializeField] private float iconScale = 2f;
    [SerializeField] private int baseSortingOrder = 10;
    private RecipeSystem recipeSystem;
    void Start()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        spriteStackRenderer = GetComponent<SpriteStackRenderer>();

        mainCamera = Camera.main;

        clickStateUtil.OnClicked += ClickRoutine;
        clickStateUtil.OnDragging += DraggingRoutine;
        clickStateUtil.OnDragEnd += DragEndRoutine;
        clickStateUtil.OnNone += NoneClickRoutine;

        animator = GetComponent<Animator>();
        hoverStateUtil.OnHovering += HoverRoutine;
        hoverStateUtil.OnNone += NoneHoverRoutine;

        ingredients = new List<IngredientData>(maxIngredientSize);
        recipeSystem = manager.GetComponent<RecipeSystem>();

        if (ingredientVisualRoot == null)
        {
            var go = new GameObject("IngredientVisualRoot");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            ingredientVisualRoot = go.transform;
        }

        RefreshIngredientVisuals();
    }

    private void ClickRoutine()
    {
        if (result != null || ingredients.Count == 0) return;

        RecipeData recipeData = recipeSystem.Search(ingredients, cookingToolData.id);

        TempMinigameData tempMinigameData = (recipeData == null)
                ? cookingToolData.minigames[Random.Range(0, cookingToolData.minigames.Count)]
                : recipeData.minigame;

        Debug.Log($"호건이가~ 좋아하는~ 랜더어엄~ 게임!\n{tempMinigameData.minigameName}!!!!");

        ingredients.Clear();
        result = (recipeData == null) ? failure : recipeData.outputFood;
        RefreshIngredientVisuals();
    }

    private void HoverRoutine()
    {
        if (!dragging)
        {
            animator.SetBool("Hovering", true);
        }
    }

    private void NoneHoverRoutine()
    {
        animator.SetBool("Hovering", false);
    }

    private void DraggingRoutine()
    {
        dragging = true;
        animator.SetBool("Hovering", false);
        Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void DragEndRoutine()
    {
        if (scanColliderUtil.GetOverlappingWithTag("Trashcan") != null)
        {
            ingredients.Clear();
            result = null;
            RefreshIngredientVisuals();
            return;
        }
        if (result != null)
        {
            CookingTool other = scanColliderUtil.GetOverlappingWithComponent<CookingTool>();
            if (other != null && other.AddIngredient(result))
            {
                result = null;
                RefreshIngredientVisuals();
            }

        }
    }

    private void NoneClickRoutine()
    {
        dragging = false;
        if (transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }

    private bool Addable(IngredientData ingredientData)
    {
        if (ingredients.Count >= maxIngredientSize || ingredients.Contains(ingredientData))
        {
            return false;
        }
        if (!cookingToolData.availableIngredientIds.Contains(ingredientData.id))
        {
            return false;
        }
        if (result != null && !cookingToolData.availableIngredientIds.Contains(result.id))
        {
            return false;
        }

        return true;
    }

    public bool AddIngredient(IngredientData ingredientData)
    {
        if (!Addable(ingredientData)) return false;

        if (result != null) ingredients.Add(result);

        ingredients.Add(ingredientData);
        RefreshIngredientVisuals();
        return true;
    }

    private void RefreshIngredientVisuals()
    {
        if (result != null)
        {
            spriteStackRenderer.DrawSingle(result.defaultImage);
            return;
        }

        spriteStackRenderer.DrawMany(ingredients
            .Where(d => d != null)
            .Select(d => d.defaultImage));
    }
}

