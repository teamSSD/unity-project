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

    [Header("Bento Settings")]
    [SerializeField] private int maxFoodSlots = 4;

    [Header("Audio")]
    [SerializeField] private AudioClip bentoPutSfx;

    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    private List<FoodSchema> foodList = new List<FoodSchema>();
    private BentoPositionModel bentoPositionModel;

    void Awake()
    {
        BehaviorInstance = GetComponent<BentoBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragEnd += SetPosition;
    }

    private void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= SetPosition;
        if (bentoPositionModel != null)
        {
            bentoPositionModel.isSet = false;
        }
    }
    public bool AddIngredient(FoodSchema food)
    {
        if (foodList.Count >= maxFoodSlots) return false;

        if (foodList.Count == 0 && food.foodData.type != FoodType.MAIN)
        {
            Debug.Log("해당 음식은 메인 음식이 아닙니다.");
            return false;
        }
        else if (foodList.Count > 0 && food.foodData.type != FoodType.SIDE)
        {
            Debug.Log("해당 음식은 사이드 음식이 아닙니다.");
            return false;
        }

        Sprite displaySprite = food.foodData.GetBentoImage(foodList.Count);
        BehaviorInstance.AddTexture(displaySprite, Vector2.zero);
        foodList.Add(food);
        return true;
    }

    public bool AddOrderTicket(OrderTicketModel orderTicket)
    {
        if (foodList.Count >= 1)
        {
            BehaviorInstance.AddTexture(Resources.Load<Sprite>("driveAssets/art/item/cooking/cookingTool/item_reciept_default"));
            Debug.Log($"도시락 포장이 완료되었습니다.");
            return true;
        }
        Debug.Log("아직 도시락이 완성되지 않았습니다.");
        return false;
    }

    public void SetPosition()
    {
        if (bentoPositionModel == null)
        {
            bentoPositionModel = scanColliderUtil.GetOverlappingWithComponent<BentoPositionModel>();
            if (bentoPositionModel != null && !bentoPositionModel.isSet)
            {
                BehaviorInstance.defaultPosition = bentoPositionModel.transform.position;
                bentoPositionModel.isSet = true;
                SoundManager.Instance.Play2DSFX(bentoPutSfx, 0.4f);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }
        else
        {
            GameObject gameObject = scanColliderUtil.GetOverlappingWithTag(Tags.Trashcan.ToString());
            if (gameObject != null)
            {
                Destroy(this.gameObject);
            }
        }
    }

    public List<FoodSchema> getFoodList()
    {
        return foodList;
    }
}
