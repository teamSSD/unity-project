using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FoodBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[DisallowMultipleComponent]
public class FoodModel : MonoBehaviour
{
    public FoodSchema SchemaInstance { get; private set; }
    public FoodBehavior BehaviorInstance { get; private set; }
    public GameObject descriptionPrefab;
    public event Action<FoodModel> onDestroy;
    ScanColliderUtil scanColliderUtil;
    ClickStateUtil clickStateUtil;
    HoverStateUtil hoverStateUtil;
    LoadInventoryUsecase loadInventoryUsecase;
    bool injected = false;
    float hoverClock = 0;
    float hoverThreshold = 0.4f;
    private GameObject descriptionObject = null;
    IngredientDescription ingredientDescriptionScript;
    private Canvas canvas;

    void Awake()
    {
        BehaviorInstance = GetComponent<FoodBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();

        clickStateUtil.OnDragEnd += AddToCookingTool;
        clickStateUtil.OnDragEnd += AddToBento;
    }

    void Start()
    {
        descriptionObject = Instantiate(descriptionPrefab, canvas.transform);
        ingredientDescriptionScript = descriptionObject.GetComponent<IngredientDescription>();
        ingredientDescriptionScript
            .SetTexts(
                SchemaInstance.foodData.ingredientName,
                loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id).ToString(),
                SchemaInstance.foodData.description
            );
        descriptionObject.SetActive(false);
    }

    void OnDestroy()
    {
        onDestroy?.Invoke(this);
        clickStateUtil.OnDragEnd -= AddToCookingTool;
        clickStateUtil.OnDragEnd -= AddToBento;
    }

    public void Inject(Canvas canvas, LoadInventoryUsecase loadInventoryUsecase, FoodData foodData, int price)
    {
        this.canvas = canvas;
        this.loadInventoryUsecase = loadInventoryUsecase;
        SchemaInstance = new FoodSchema(foodData, price);
        Sprite newSprite = Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName);
        gameObject.GetComponent<SpriteRenderer>().sprite = newSprite;
        PolygonCollider2D existingCollider = GetComponent<PolygonCollider2D>();
        Destroy(existingCollider);
        gameObject.AddComponent<PolygonCollider2D>();
        injected = true;
    }

    public void Update()
    {
        if (clickStateUtil.getState() == ClickState.None && hoverStateUtil.IsHovering())
        {
            if (hoverClock < hoverThreshold && hoverClock + Time.deltaTime >= hoverThreshold)
            {
                descriptionObject.SetActive(true);
                ingredientDescriptionScript
                    .UpdateCount(loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id).ToString());
                descriptionObject.transform.position = Camera.main.WorldToScreenPoint(this.transform.position + new Vector3(2, 0, 0));
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

    public void AddToCookingTool()
    {
        if (!injected)
        {
            Debug.LogWarning("Interface didn't injected.");
            return;
        }

        CookingToolModel collision = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id) <= 0)
        {
            Destroy(this.gameObject);
            return;
        }
        if (collision != null && SchemaInstance.foodData.availableTool.Contains(collision.SchemaInstance.cookingToolData.id))
        {
            bool reflected = collision.AddIngredient(this.SchemaInstance);
            if (!reflected)
            {
                loadInventoryUsecase.ConsumeFood(SchemaInstance.foodData.id, 1);
                if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id) <= 0)
                {
                    Destroy(this.gameObject);
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
        if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id) <= 0)
        {
            Destroy(this.gameObject);
            return;
        }
        if (collision != null)
        {
            bool reflected = collision.AddIngredient(this.SchemaInstance);
            if (!reflected)
            {
                loadInventoryUsecase.ConsumeFood(SchemaInstance.foodData.id, 1);
                if (loadInventoryUsecase.CheckStockAmount(SchemaInstance.foodData.id) <= 0)
                {
                    Destroy(this.gameObject);
                }
            }
        }
    }
}