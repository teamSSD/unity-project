using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BentoBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class BentoModel : MonoBehaviour
{
    public BentoBehavior BehaviorInstance { get; private set; }
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;

    private List<Vector2> locateList = new List<Vector2>() { new Vector2(-0.4f, 0f), new Vector2(0.6f, 0.4f) , new Vector2(0.6f, 0f) , new Vector2(0.6f, -0.4f) };
    private List<FoodSchema> foodList = new List<FoodSchema>();

    void Awake()
    {
        BehaviorInstance = GetComponent<BentoBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();
    }
    public bool AddIngredient(FoodSchema food)
    {
        if (foodList.Count < 4)
        {
            BehaviorInstance.AddTexture(Resources.Load<Sprite>(ResourcePaths.Art.FOOD + food.foodData.imageName), locateList[foodList.Count]);
            foodList.Add(food);
            Debug.Log($"{food.foodData.ingredientName}을 도시락에 추가했습니다.");
            return true;
        }
        Debug.Log("4개 이상 담을 수는 없습니다.");
        return false;
    }

    public bool AddOrderTicket(OrderTicketModel orderTicket)
    {
        if (foodList.Count >= 4) 
        {
            BehaviorInstance.AddTexture(Resources.Load<Sprite>("driveAssets/art/item/cooking/cookingTool/item_reciept_default"));
            Debug.Log($"도시락 포장이 완료되었습니다.");
            Destroy(orderTicket.gameObject);
            return true;
        }
        Debug.Log("아직 도시락이 완성되지 않았습니다.");
        return false;
    }
}
