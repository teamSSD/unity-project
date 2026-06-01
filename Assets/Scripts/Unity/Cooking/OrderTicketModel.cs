using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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

        FoodSchema main = bento.getFoodList()[0];
        List<FoodSchema> sides = bento.getFoodList();
        Vector3 spawnPosition = bento.GetBentoPosition();
        Destroy(bento.gameObject);
        onTake.Invoke(main, sides, spawnPosition);
    }
}