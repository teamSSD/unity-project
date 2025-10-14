using UnityEngine;

[RequireComponent(typeof(FoodSchema))]
[RequireComponent(typeof(FoodBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[DisallowMultipleComponent]
class FoodModel : MonoBehaviour
{
    public FoodSchema SchemaInstance { get; private set; }
    public FoodBehavior BehaviorInstance { get; private set; }
    ScanColliderUtil scanColliderUtil;
    ClickStateUtil clickStateUtil;

    void Awake()
    {
        SchemaInstance = GetComponent<FoodSchema>();
        BehaviorInstance = GetComponent<FoodBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();

        clickStateUtil.OnDragEnd += AddToCookingTool;
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= AddToCookingTool;
    }

    void AddToCookingTool()
    {
        CookingToolModel collision = scanColliderUtil.GetOverlappingWithComponent<CookingToolModel>();
        if (collision != null && SchemaInstance.foodData.availableTool.Contains(collision.SchemaInstance.cookingToolData.id))
        {
            if (collision.AddIngredient(this.SchemaInstance))
            {
                Destroy(this.gameObject);
            }
        }
    }
}