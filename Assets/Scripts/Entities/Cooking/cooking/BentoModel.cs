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
            foodList.Add(food);
            Debug.Log($"{food.foodData.ingredientName}을 도시락에 추가했습니다.");
            return true;
        }
        Debug.Log("4개 이상 담을 수는 없습니다.");
        return false;
    }
}
