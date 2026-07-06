using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(OrderTicketBehavior))]
[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(SpriteRenderer))]
public class OrderTicketModel : MonoBehaviour
{
    private OrderTicketBehavior BehaviorInstance;
    private MenuSchema menuSchema;
    [SerializeField] private AudioClip attachSfx;
    [Tooltip("도시락에 부착될 때의 local position (도시락 기준)")]
    [SerializeField] private Vector3 attachedLocalPosition = Vector3.zero;
    [Tooltip("부착 시 영수증을 도시락 위로 띄울 sortingOrder offset (도시락 SR 기준 +offset). 음식 아이콘들보다 높아야 함.")]
    [SerializeField] private int attachedSortingOrderOffset = 100;
    [Tooltip("부착 시 영수증의 world scale 배율 (1 = 원래 크기). 드래그/rest 시엔 원 크기 유지.")]
    [SerializeField] private float attachedScale = 0.7f;
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    public bool IsAttached {get; private set;} = false;
    public bool IsDelivery { get; set; }
    public string QuestId { get; set; }
    public event Action OnAttached = () => {};

    public event Action<FoodSchema, List<FoodSchema>, Vector3> onTake = (_, __, ___) => { };
    void Awake()
    {
        BehaviorInstance = GetComponent<OrderTicketBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();
    }

    void Start()
    {
        // OnDragEnd 등록은 Start에서 — DragSortingBump.OnEnable(Awake 후 Start 전)이 먼저 등록되어야
        // invoke 순서가 [DSB → AddToBento]가 되고, AddToBento에서 set한 sortingOrder가 살아남는다.
        // (Awake에 두면 [AddToBento → DSB]가 되어 DSB가 sortingOrder를 0으로 reset해버림.)
        clickStateUtil.OnDragEnd += AddToBento;

        SoundManager.Instance?.Play2DSFX(attachSfx, 0.4f);
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= AddToBento;
    }

    public void AddToBento()
    {
        BentoModel collision = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (collision != null)
        {
            if (IsDelivery && !ValidateExactMatch(collision))
                return;

            bool affected = collision.AddOrderTicket(this);
            if (affected)
            {
                IsAttached = true;
                collision.BehaviorInstance.locked = true; // 완성된 도시락은 이동 불가
                WaitAndTakeAsync(GameRandom.NormalRange(GameRandom.Variable, 0.5f, 1.5f), collision).Forget();

                // 드래그한 영수증을 도시락 자식으로 reparent.
                // worldPositionStays=true → 시각 크기(world scale) 보존. localScale은 자동 보정.
                transform.SetParent(collision.transform, worldPositionStays: true);
                transform.localPosition = attachedLocalPosition;
                transform.localRotation = Quaternion.identity;
                // 부착 시 영수증 world 크기를 attachedScale 배율로 축소.
                transform.localScale *= attachedScale;

                // 영수증이 도시락 위에 보이게 — SortingGroup으로 자식 TMP까지 일괄 정렬.
                var sg = GetComponent<SortingGroup>() ?? gameObject.AddComponent<SortingGroup>();
                var bentoSr = collision.GetComponent<SpriteRenderer>();
                if (bentoSr != null)
                {
                    sg.sortingLayerID = bentoSr.sortingLayerID;
                    sg.sortingOrder = bentoSr.sortingOrder + attachedSortingOrderOffset;
                }


                // 부착된 영수증은 더 이상 드래그/클릭 못함.
                clickStateUtil.enabled = false;
                foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

                OnAttached.Invoke();
                SoundManager.Instance.Play2DSFX(attachSfx, 0.4f);
            }
        }
    }

    public void SetMenu(MenuSchema menuSchema)
    {
        this.menuSchema = menuSchema;
    }

    public void SetDefaultPosition(Vector3 position)
    {
        BehaviorInstance.defaultPosition = position;
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }

    private bool ValidateExactMatch(BentoModel bento)
    {
        var foods = bento.getFoodList();
        if (foods.Count == 0) return false;

        var expected = menuSchema.mainMenus.Select(m => m.id)
            .Concat(menuSchema.sideMenus.Select(s => s.id))
            .OrderBy(x => x).ToList();
        var actual = foods.Select(f => f.foodData.id).OrderBy(x => x).ToList();

        return expected.SequenceEqual(actual);
    }

    private async UniTaskVoid WaitAndTakeAsync(float time, BentoModel bento)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: this.GetCancellationTokenOnDestroy());
        if (bento == null) return;

        var foodList = bento.getFoodList();
        if (foodList.Count == 0) { Destroy(bento.gameObject); return; }

        FoodSchema main = foodList[0];
        // 메인을 제외한 사이드만 분리 (이전: 전체 리스트 → 메인이 사이드로도 카운트되는 버그)
        List<FoodSchema> sides = foodList.Count > 1
            ? foodList.GetRange(1, foodList.Count - 1)
            : new List<FoodSchema>();
        Vector3 spawnPosition = bento.GetBentoPosition();
        Destroy(bento.gameObject);
        onTake.Invoke(main, sides, spawnPosition);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
