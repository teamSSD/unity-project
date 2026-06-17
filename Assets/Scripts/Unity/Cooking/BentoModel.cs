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
    [SerializeField] private AudioClip trashcanSfx;
    [SerializeField] private Sprite receiptSprite;

    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    private List<FoodSchema> foodList = new List<FoodSchema>();
    private BentoPositionModel bentoPositionModel;

    void Awake()
    {
        BehaviorInstance = GetComponent<BentoBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragStart += ShowAvailablePositions;
        clickStateUtil.OnDragEnd += HideAvailablePositions;
        clickStateUtil.OnDragEnd += SetPosition;
    }

    private void OnDestroy()
    {
        clickStateUtil.OnDragStart -= ShowAvailablePositions;
        clickStateUtil.OnDragEnd -= HideAvailablePositions;
        clickStateUtil.OnDragEnd -= SetPosition;
        if (bentoPositionModel != null)
        {
            bentoPositionModel.isSet = false;
        }
    }

    private void ShowAvailablePositions()
    {
        var all = FindObjectsByType<BentoPositionModel>(FindObjectsSortMode.None);
        foreach (var pos in all)
        {
            if (!pos.isSet) pos.ShowHighlight();
        }
    }

    private void HideAvailablePositions()
    {
        var all = FindObjectsByType<BentoPositionModel>(FindObjectsSortMode.None);
        foreach (var pos in all) pos.HideHighlight();
    }
    public bool AddIngredient(FoodSchema food)
    {
        if (foodList.Count >= maxFoodSlots) return false;

        if (food.foodData.type != FoodType.MAIN && food.foodData.type != FoodType.SIDE)
        {
            return false;
        }

        Sprite displaySprite = null;

        if (food.foodData.type == FoodType.MAIN)
        {
            displaySprite = food.foodData.GetMainBentoImage();
        }
        else if (food.foodData.type == FoodType.SIDE)
        {
            // 등록된 요리 중 SIDE의 개수를 셉니다 (가장 먼저 들어오면 0)
            int sideCount = 0;
            foreach (var f in foodList)
            {
                if (f.foodData.type == FoodType.SIDE) sideCount++;
            }
            displaySprite = food.foodData.GetSideBentoImage(sideCount);
        }
        BehaviorInstance.AddTexture(displaySprite, Vector2.zero);
        foodList.Add(food);
        return true;
    }

    public bool AddOrderTicket(OrderTicketModel orderTicket)
    {
        if (foodList.Count >= 1)
        {
            BehaviorInstance.AddTexture(receiptSprite);
            return true;
        }
        return false;
    }

    public void SetPosition()
    {
        if (bentoPositionModel == null)
        {
            // 첫 배치 — 빈 BentoPosition에만 안착
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
            return;
        }

        // 이미 자리잡은 상태 — Trashcan 우선
        GameObject trashcan = scanColliderUtil.GetOverlappingWithTag(Tags.Trashcan.ToString());
        if (trashcan != null)
        {
            SoundManager.Instance?.Play2DSFX(trashcanSfx);
            Destroy(this.gameObject);
            return;
        }

        // 다른 빈 BentoPosition 으로 이동 (점유 자리는 자동 복귀)
        BentoPositionModel newPosition = scanColliderUtil.GetOverlappingWithComponent<BentoPositionModel>();
        if (newPosition != null && newPosition != bentoPositionModel && !newPosition.isSet)
        {
            bentoPositionModel.isSet = false;
            bentoPositionModel = newPosition;
            bentoPositionModel.isSet = true;
            BehaviorInstance.defaultPosition = bentoPositionModel.transform.position;
            SoundManager.Instance.Play2DSFX(bentoPutSfx, 0.4f);
        }
    }

    public List<FoodSchema> getFoodList()
    {
        return foodList;
    }

    public Vector3 GetBentoPosition()
    {
        if (bentoPositionModel != null)
        {
            return bentoPositionModel.transform.position;
        }
        return transform.position;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
