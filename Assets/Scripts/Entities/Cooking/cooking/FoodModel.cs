using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FoodBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
public class FoodModel : MonoBehaviour
{
    public FoodSchema SchemaInstance { get; private set; }
    public FoodBehavior BehaviorInstance { get; private set; }
    public event Action<FoodModel> onDestroy;
    ScanColliderUtil scanColliderUtil;
    ClickStateUtil clickStateUtil;
    LoadInventoryUsecase loadInventoryUsecase;
    bool injected = false;

    void Awake()
    {
        BehaviorInstance = GetComponent<FoodBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragEnd += AddToCookingTool;
        clickStateUtil.OnDragEnd += AddToBento;
    }

    void OnDestroy()
    {
        onDestroy?.Invoke(this);
        clickStateUtil.OnDragEnd -= AddToCookingTool;
        clickStateUtil.OnDragEnd -= AddToBento;
    }

    public void Inject(LoadInventoryUsecase loadInventoryUsecase, FoodData foodData, int price)
    {
        this.loadInventoryUsecase = loadInventoryUsecase;
        SchemaInstance = new FoodSchema(foodData, price);
        Sprite newSprite = Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName);
        gameObject.GetComponent<SpriteRenderer>().sprite = newSprite;
        PolygonCollider2D existingCollider = GetComponent<PolygonCollider2D>();
        Destroy(existingCollider);
        gameObject.AddComponent<PolygonCollider2D>();
        injected = true;
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