using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(OrderTicketBehavior))]
[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
public class OrderTicketModel : MonoBehaviour
{
    private OrderTicketBehavior BehaviorInstance;
    private MenuSchema menuSchema;
    [SerializeField] private AudioClip attachSfx;
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

        clickStateUtil.OnDragEnd += AddToBento;
    }

    void Start()
    {
        SoundManager.Instance.Play2DSFX(attachSfx, 0.4f);
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
                StartCoroutine(waitAndTake(RandomGeneral.getRandomNormal(0.5f, 1.5f), collision));

                GetComponent<SpriteRenderer>().enabled = false;
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

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

        // 메인 확인
        if (menuSchema.mainMenu == null || foods[0].foodData.id != menuSchema.mainMenu.id)
            return false;

        // 사이드 확인: 정확히 같은 구성이어야 함
        var expectedSides = menuSchema.sideMenus
            .Select(s => s.id).OrderBy(x => x).ToList();
        var actualSides = foods.Skip(1)
            .Select(f => f.foodData.id).OrderBy(x => x).ToList();

        return expectedSides.SequenceEqual(actualSides);
    }

    private IEnumerator waitAndTake(float time, BentoModel bento) // buy에서 수정
    {
        yield return new WaitForSeconds(time);

        FoodSchema main = bento.getFoodList()[0];
        List<FoodSchema> sides = bento.getFoodList();
        Destroy(bento.gameObject);
        onTake.Invoke(main, sides, bento.transform.position);
    }
}