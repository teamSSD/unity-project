using System;
using System.Collections.Generic;
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

        clickStateUtil.OnDragEnd += AddToCookingTool;
        clickStateUtil.OnDragEnd += AddToBento;
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
        clickStateUtil.OnDragEnd -= AddToCookingTool;
        clickStateUtil.OnDragEnd -= AddToBento;
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

    public void AddToCookingTool()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        CookingToolModel collision = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
        {
            Destroy(this.gameObject);
            return;
        }
        if (collision != null && SchemaInstance.foodData.availableTools.Contains(collision.GetToolId()))
        {
            bool reflected = collision.AddIngredient(this.SchemaInstance);
            if (reflected)
            {
                loadInventoryUsecase.ConsumeFood(SchemaInstance.foodData, 1);
                gameObject.transform.position = BehaviorInstance.defaultPosition;
                if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
    public void AddToBento()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        BentoModel collision = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
        {
            Destroy(this.gameObject);
            return;
        }
        if (collision != null)
        {
            bool reflected = collision.AddIngredient(this.SchemaInstance);
            if (reflected)
            {
                loadInventoryUsecase.ConsumeFood(SchemaInstance.foodData, 1);
                gameObject.transform.position = BehaviorInstance.defaultPosition;
                if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData) <= 0)
                {
                    Destroy(this.gameObject);
                }
            }
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
