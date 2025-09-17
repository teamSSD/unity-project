using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
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
    private bool dragging = false;
    private List<IngredientData> ingredients;

    [Header("Ingredient Visuals")]
    [SerializeField] private Transform ingredientVisualRoot;
    [SerializeField] private Vector2 stackStart = new Vector2(0f, 0.1f);
    [SerializeField] private Vector2 stackStep = new Vector2(0.08f, 0.04f);
    [SerializeField] private float iconScale = 2f;
    [SerializeField] private int baseSortingOrder = 10;
    void Start()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        clickStateUtil.OnClicked += ClickRoutine;
        clickStateUtil.OnDragging += DraggingRoutine;
        clickStateUtil.OnNone += NoneClickRoutine;

        hoverStateUtil = GetComponent<HoverStateUtil>();
        animator = GetComponent<Animator>();
        hoverStateUtil.OnHovering += HoverRoutine;
        hoverStateUtil.OnNone += NoneHoverRoutine;

        mainCamera = Camera.main;

        ingredients = new List<IngredientData>(maxIngredientSize);

        if (ingredientVisualRoot == null)
        {
            var go = new GameObject("IngredientVisualRoot");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            ingredientVisualRoot = go.transform;
        }
    }

    private void ClickRoutine()
    {
        Debug.Log("호건이가~ 좋아하는~ 랜더어엄~ 게임!");
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
        return cookingToolData.availableIngredientIds.Contains(ingredientData.id);
    }

    public bool AddIngredient(IngredientData ingredientData)
    {
        if (!Addable(ingredientData))
        {
            return false;
        }
        ingredients.Add(ingredientData);
        RefreshIngredientVisuals();
        return true;
    }

    private void RefreshIngredientVisuals()
    {
        for (int i = ingredientVisualRoot.childCount - 1; i >= 0; i--)
            Destroy(ingredientVisualRoot.GetChild(i).gameObject);

        var toolSr = GetComponent<SpriteRenderer>();
        int visibleIndex = 0;

        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientData data = ingredients[i];
            if (data == null || data.defaultImage == null)
                continue;

            var iconGO = new GameObject($"IngredientIcon_{i}");
            iconGO.transform.SetParent(ingredientVisualRoot, false);

            var sr = iconGO.AddComponent<SpriteRenderer>();
            if (toolSr != null)
            {
                sr.sortingLayerID = toolSr.sortingLayerID;
                sr.sortingLayerName = toolSr.sortingLayerName;
            }

            sr.sprite = data.defaultImage;

            Vector3 localPos = new Vector3(
                stackStart.x + stackStep.x * visibleIndex,
                stackStart.y + stackStep.y * visibleIndex,
                -0.001f * visibleIndex
            );

            sr.transform.localPosition = localPos;
            sr.transform.localScale = Vector3.one * iconScale;
            sr.sortingOrder = baseSortingOrder + visibleIndex;

            visibleIndex++;
        }
    }
}

