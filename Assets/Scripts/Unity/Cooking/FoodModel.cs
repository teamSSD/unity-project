using System;
using System.Collections.Generic;
using Game.Domain.Cooking;
using UnityEngine;

[RequireComponent(typeof(FoodBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(TooltipController))]
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class FoodModel : MonoBehaviour
{
    private FoodSchema SchemaInstance;
    private FoodBehavior BehaviorInstance;
    public event Action<FoodModel> onDestroy;

    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    private TooltipController tooltipController;
    private CookingToolDescription tooltipScript;
    private LoadInventoryUsecase loadInventoryUsecase;

    private bool injected = false;

    void Awake()
    {
        BehaviorInstance = GetComponent<FoodBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();
        tooltipController = GetComponent<TooltipController>();

        clickStateUtil.OnDragEnd += OnFoodDropped;
    }

    void Start()
    {
        if (tooltipController != null && tooltipController.GetTooltipObject() != null)
        {
            tooltipScript = tooltipController.GetTooltipObject().GetComponent<CookingToolDescription>();
            tooltipController.RegisterContentUpdater(UpdateTooltipContent);
        }
    }

    void OnDestroy()
    {
        onDestroy?.Invoke(this);
        clickStateUtil.OnDragEnd -= OnFoodDropped;
    }

    public void Inject(Canvas canvas, LoadInventoryUsecase loadInventoryUsecase, FoodData foodData, int price)
    {
        this.loadInventoryUsecase = loadInventoryUsecase;
        SchemaInstance = new FoodSchema(foodData, price);
        Sprite newSprite = foodData.image;
        gameObject.GetComponent<SpriteRenderer>().sprite = newSprite;

        // Recreate collider with simplified and expanded hit area for easier clicking
        PolygonCollider2D existingCollider = GetComponent<PolygonCollider2D>();
        Destroy(existingCollider);
        PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();

        // Only process small colliders (large ones don't need expansion)
        Bounds bounds = collider.bounds;
        float area = bounds.size.x * bounds.size.y;

        if (area < 2.0f) // Only process small sprites based on area
        {
            for (int pathIndex = 0; pathIndex < collider.pathCount; pathIndex++)
            {
                Vector2[] path = collider.GetPath(pathIndex);

                // Step 1: Compute convex hull to remove pointy inward vertices
                Vector2[] hull = ComputeConvexHull(path);

                // Step 2: Expand from centroid
                Vector2[] expandedPath = ExpandPolygonFromCentroid(hull, 0.15f);

                collider.SetPath(pathIndex, expandedPath);
            }
        }

        injected = true;
    }

    /// <summary>
    /// Compute convex hull using Gift Wrapping (Jarvis March) algorithm
    /// </summary>
    private Vector2[] ComputeConvexHull(Vector2[] points)
    {
        if (points.Length < 3) return points;

        int leftmost = FindLeftmostPointIndex(points);
        var hull = GiftWrapHull(points, leftmost);

        return hull.Count >= 3 ? hull.ToArray() : points;
    }

    private static int FindLeftmostPointIndex(Vector2[] points)
    {
        int leftmost = 0;
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].x < points[leftmost].x ||
                (points[i].x == points[leftmost].x && points[i].y < points[leftmost].y))
            {
                leftmost = i;
            }
        }
        return leftmost;
    }

    private List<Vector2> GiftWrapHull(Vector2[] points, int start)
    {
        var hull = new List<Vector2>();
        int current = start;
        int next;

        do
        {
            hull.Add(points[current]);
            next = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (i == current) continue;
                if (next == current || IsCounterClockwise(points[current], points[i], points[next]))
                    next = i;
            }
            current = next;
        } while (current != start);

        return hull;
    }

    /// <summary>
    /// Check if point c is counter-clockwise from line a->b
    /// </summary>
    private bool IsCounterClockwise(Vector2 a, Vector2 b, Vector2 c)
    {
        // Cross product: (b - a) × (c - a)
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return cross > 0;
    }

    /// <summary>
    /// Expand polygon outward from its centroid
    /// </summary>
    private Vector2[] ExpandPolygonFromCentroid(Vector2[] path, float offset)
    {
        Vector2[] expandedPath = new Vector2[path.Length];

        // Calculate centroid
        Vector2 centroid = Vector2.zero;
        foreach (Vector2 point in path)
        {
            centroid += point;
        }
        centroid /= path.Length;

        // Expand each vertex away from centroid
        for (int i = 0; i < path.Length; i++)
        {
            Vector2 direction = (path[i] - centroid).normalized;
            expandedPath[i] = path[i] + direction * offset;
        }

        return expandedPath;
    }

    /// <summary>
    /// Update tooltip content when displayed
    /// </summary>
    private void UpdateTooltipContent()
    {
        if (tooltipScript == null || SchemaInstance == null) return;

        string name = SchemaInstance.foodData.ingredientName;
        int count = loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData);
        tooltipScript.SetTexts("이름 : " + name, "수량 : " + count);
        tooltipController.RequestPositionNear(GetComponent<SpriteRenderer>().bounds, forceRight: true);
    }

    /// <summary>
    /// 드래그 종료 시 단일 dispatched 진입점. 이전엔 AddToCookingTool/AddToBento 두 핸들러가
    /// 각자 OnDragEnd에 구독해 독립 스캔했음 (review_2026-07_deepdig.md D "다중-구독" 온상).
    /// 이제 target을 우선순위대로 해석 → 매칭되면 accept 시도 → 성공 시 공통 side effect.
    ///
    /// 우선순위: CookingTool(사용 가능 도구) → Bento. 겹침이 동시일 경우 이전 구독 순서(Tool 먼저)와 동일.
    /// </summary>
    private void OnFoodDropped()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
        {
            Destroy(this.gameObject);
            return;
        }

        // 1) CookingTool — 규칙(availableTools 포함) 통과 시 accept 시도.
        var tool = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (tool != null
            && IngredientPlacementRules.CanFoodEnterTool(SchemaInstance.foodData, tool.GetToolId())
            && tool.AddIngredient(this.SchemaInstance))
        {
            ConsumeAndReposition();
            return;
        }

        // 2) Bento — 규칙(MAIN/SIDE) 통과 시 accept 시도. raw INGREDIENT는 여기서 조기 거부.
        var bento = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (bento != null
            && IngredientPlacementRules.CanFoodEnterBento(SchemaInstance.foodData)
            && bento.AddIngredient(this.SchemaInstance))
        {
            ConsumeAndReposition();
        }
    }

    private void ConsumeAndReposition()
    {
        loadInventoryUsecase.ConsumeFood(SchemaInstance.foodData, 1);
        gameObject.transform.position = BehaviorInstance.defaultPosition;
        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    public void SetDefaultPosition(Vector3 position)
    {
        BehaviorInstance.defaultPosition = position;
    }

    public Vector3 GetDefaultPosition()
    {
        return BehaviorInstance.defaultPosition;
    }
}
