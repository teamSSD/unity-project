using System;
using System.Collections.Generic;
using Game.Domain.Cooking;
using UnityEngine;

/// <summary>
/// Cooking 씬 손님 흐름 코디네이터.
/// 책임 분리: CustomerSpawner / OrderTicketController / DeliveryTicketCoordinator / CustomerSessionStats.
/// 본 클래스는 spawn 루프 + lifecycle 콜백 + 영업 종료만 담당.
/// </summary>
[RequireComponent(typeof(CustomerSpawner))]
[RequireComponent(typeof(OrderTicketController))]
[RequireComponent(typeof(DeliveryTicketCoordinator))]
public class CustomerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioClip doorSfx;
    [SerializeField] private List<CustomerData> customerDataList;

    [Header("Prefabs")]
    [SerializeField] private GameObject orderingCustomerPrefab;
    [SerializeField] private GameObject waitingCustomerPrefab;
    [SerializeField] private GameObject takingCustomerPrefab;
    [SerializeField] private GameObject receiptPrefab;
    [SerializeField] private Canvas worldCanvas;

    // Spawn 페이즈별 자동 설정
    private float baseSpawnInterval = 30f;
    private float spawnIntervalVariance = 8f;

    // Components
    private CustomerSpawner spawner;
    private OrderTicketController ticketController;
    private DeliveryTicketCoordinator deliveryCoord;

    // State
    private List<MenuSchema> salesMenus;
    private readonly List<CustomerLifecycle> activeCustomers = new();
    private readonly CustomerSessionStats sessionStats = new();
    private int nextOrderNumber = 1;
    private bool isOpen = true;

    // Spawn timing
    private float timer = 0;
    private float nextSpawnTime;
    private GameObject currentOrderingCustomer;

    public event Action OnGameEnd;

    void Awake()
    {
        spawner = GetComponent<CustomerSpawner>();
        ticketController = GetComponent<OrderTicketController>();
        deliveryCoord = GetComponent<DeliveryTicketCoordinator>();

        spawner.Inject(orderingCustomerPrefab, waitingCustomerPrefab, takingCustomerPrefab, worldCanvas);
        ticketController.Inject(receiptPrefab);

        salesMenus = new RecipeBookMenuProvider().GetTodaysMenu();
        if (salesMenus.Count == 0)
        {
            Debug.LogError("[CustomerManager] No menus available! Cannot start cooking scene without menu selection.");
            isOpen = false;
            return;
        }
    }

    void Start()
    {
        // TimeManager 구독은 Start에서 — 모든 Awake 완료 후라 Instance 보장.
        // (이전엔 StatManager.OnEnable에서 중계했는데, 구독 race 가능성으로 직접 구독.)
        var time = TimeManager.Instance;
        if (time != null) time.OnTimeEnd += OnTimeEnd;

        ApplyPhaseSettings();
        deliveryCoord.CreateAll();
        nextSpawnTime = GameRandom.Normal(GameRandom.Variable, 5f, 3f); // 첫 손님은 빠르게

        var subScene = FindFirstObjectByType<SubSceneController>();
        if (subScene != null) OnGameEnd += subScene.ReturnToIdle;
    }

    private void ApplyPhaseSettings()
    {
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData?.Phase ?? PhaseType.Morning;
        switch (phase)
        {
            case PhaseType.Morning:   baseSpawnInterval = 1f; spawnIntervalVariance = 1f; break;
            case PhaseType.Afternoon: baseSpawnInterval = 30f; spawnIntervalVariance = 8f;  break;
            case PhaseType.Evening:   baseSpawnInterval = 22f; spawnIntervalVariance = 5f;  break;
            case PhaseType.Night:     baseSpawnInterval = 15f; spawnIntervalVariance = 4f;  break;
        }
    }

    void Update()
    {
        if (!isOpen) return;
        HandleCustomerSpawning();
    }

    private void HandleCustomerSpawning()
    {
        timer += Time.deltaTime;
        if (timer < nextSpawnTime) return;

        timer -= nextSpawnTime;
        nextSpawnTime = GameRandom.Normal(GameRandom.Variable, baseSpawnInterval, spawnIntervalVariance);

        if (currentOrderingCustomer != null || spawner.IsWaitingQueueFull()) return;

        MenuSchema menu = PickRandomMenu();
        if (menu == null) return;

        CustomerData customerData = PickRandomCustomerData();
        if (customerData == null) return; // 풀 비었거나 모두 제외 — 다음 tick 재시도

        CustomerLifecycle lifecycle = new CustomerLifecycle(menu, customerData, spawner, ticketController);
        lifecycle.OnCustomerServed += OnCustomerServed;
        lifecycle.OnCustomerLeft += OnCustomerLeft;

        currentOrderingCustomer = lifecycle.StartOrder();
        activeCustomers.Add(lifecycle);

        if (doorSfx != null) SoundManager.Instance.Play2DSFX(doorSfx, 0.7f);
    }

    private void OnCustomerServed(CustomerLifecycle lifecycle, MenuValidator.ValidationResult validation, int reward)
    {
        sessionStats.RecordOrderServed(validation.AccuracyScore, reward);

        Debug.Log($"[CustomerManager] Order served - Grade: {MenuValidator.GetGrade(validation.AccuracyScore)} " +
                  $"({validation.AccuracyScore:F2}) - Total Orders: {sessionStats.TotalOrders}");

        if (ValidationFeedbackUI.Instance != null)
        {
            ValidationFeedbackUI.Instance.ShowFeedback(
                MenuValidator.GetGrade(validation.AccuracyScore),
                validation.AccuracyScore,
                reward,
                validation.FeedbackMessage
            );
        }

        OnCustomerCompleted(lifecycle);
    }

    private void OnCustomerLeft(CustomerLifecycle lifecycle) => OnCustomerCompleted(lifecycle);

    /// <summary>매니저가 lifecycle에 구독한 이벤트 해제 + Cleanup. 누수 방지.</summary>
    private void OnCustomerCompleted(CustomerLifecycle lifecycle)
    {
        lifecycle.OnCustomerServed -= OnCustomerServed;
        lifecycle.OnCustomerLeft -= OnCustomerLeft;

        lifecycle.Cleanup();
        activeCustomers.Remove(lifecycle);
        currentOrderingCustomer = null;

        CheckGameEnd();
    }

    private MenuSchema PickRandomMenu()
    {
        if (salesMenus.Count == 0)
        {
            Debug.LogError("[CustomerManager] Cannot pick menu - salesMenus is empty!");
            return null;
        }
        MenuSchema menu = salesMenus[GameRandom.Range(GameRandom.Variable, 0, salesMenus.Count)];
        menu.orderNumber = nextOrderNumber;
        nextOrderNumber++;
        return menu;
    }

    private CustomerData PickRandomCustomerData()
    {
        if (customerDataList.Count == 0)
        {
            Debug.LogWarning("[CustomerManager] No customer data available!");
            return null;
        }

        var excluded = CollectExcludedNpcIds();
        var pool = new List<CustomerData>(customerDataList.Count);
        foreach (var cd in customerDataList)
        {
            if (cd == null) continue;
            // npcId 없으면 NPC 매핑 없는 손님 — 중복/퀘스트 체크 대상 아님, 항상 후보
            if (!string.IsNullOrEmpty(cd.npcId) && excluded.Contains(cd.npcId)) continue;
            pool.Add(cd);
        }

        if (pool.Count == 0) return null; // 모두 제외 — 다음 tick 재시도
        return pool[GameRandom.Range(GameRandom.Variable, 0, pool.Count)];
    }

    /// <summary>활성 손님(중복 방지) + 활성 주문(퀘스트 진행중) NPC id 집합.</summary>
    private HashSet<string> CollectExcludedNpcIds()
    {
        var set = new HashSet<string>();
        foreach (var lc in activeCustomers)
        {
            string id = lc.GetNpcId();
            if (!string.IsNullOrEmpty(id)) set.Add(id);
        }

        var session = GameSessionRoot.Instance;
        var orderSvc = session != null ? session.Order : null;
        if (orderSvc == null) return set;

        var npcCatalog = CatalogProvider.DeliveryNpc;
        foreach (var o in orderSvc.GetOrders())
        {
            if (o.state == DeliveryOrderState.Delivered) continue;
            // questId="quest_<groupId>" → 그룹 전체 멤버 제외 (페어/그룹 quest 대응)
            string groupId = (o.questId != null && o.questId.StartsWith("quest_"))
                ? o.questId[6..] : null;
            if (!string.IsNullOrEmpty(groupId) && npcCatalog != null)
            {
                foreach (var id in npcCatalog.GetAllIdsByGroupId(groupId)) set.Add(id);
            }
            else if (!string.IsNullOrEmpty(o.npcId)) set.Add(o.npcId);
        }
        return set;
    }

    private void OnTimeEnd()
    {
        Debug.Log("[CustomerManager] OnTimeEnd — closing shop");
        isOpen = false;

        // Ordering 손님: 즉시 destroy (걸어나가지 않음). lifecycle도 같이 정리.
        if (currentOrderingCustomer != null)
        {
            Destroy(currentOrderingCustomer);
            currentOrderingCustomer = null;
        }

        // 주문 전 / waiting customer 미생성 상태로 멈춘 lifecycle은 제거 — ticket도 없으니 안전.
        // (활성 ticket 가진 waiting 손님은 인내심 게이지(2배 가속) → 자연 timeout 흐름 유지.)
        for (int i = activeCustomers.Count - 1; i >= 0; i--)
        {
            var lc = activeCustomers[i];
            if (lc.IsStuckPreOrder())
            {
                lc.OnCustomerServed -= OnCustomerServed;
                lc.OnCustomerLeft -= OnCustomerLeft;
                lc.Cleanup();
                activeCustomers.RemoveAt(i);
            }
        }

        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (!isOpen && !ticketController.HasActiveCustomerTickets())
        {
            LogSessionSummary();
            OnGameEnd?.Invoke();
        }
    }

    private void OnDestroy()
    {
        foreach (var lifecycle in activeCustomers)
        {
            lifecycle.OnCustomerServed -= OnCustomerServed;
            lifecycle.OnCustomerLeft -= OnCustomerLeft;
            lifecycle.Cleanup();
        }
        activeCustomers.Clear();

        var time = TimeManager.Instance;
        if (time != null) time.OnTimeEnd -= OnTimeEnd;
    }

    public int GetActiveCustomerCount() => activeCustomers.Count;
    public int GetWaitingQueueSize() => spawner.MaxWaitingCustomers - spawner.AvailableWaitingSlots;
    public bool IsShopOpen() => isOpen;
    public (int total, int perfect, float avgScore, int earnings) GetSessionStats() => sessionStats.Snapshot();

    private void LogSessionSummary()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (sessionStats.TotalEarnings > 0 && GameSessionRoot.Instance?.Settlement != null && progress != null)
            GameSessionRoot.Instance?.Settlement.AddIncome(PhaseToLabel(progress.PhaseData.Phase), sessionStats.TotalEarnings);
    }

    private static string PhaseToLabel(PhaseType p) => p switch
    {
        PhaseType.Morning   => "아침 영업",
        PhaseType.Afternoon => "점심 영업",
        PhaseType.Evening   => "저녁 영업",
        PhaseType.Night     => "야간 영업",
        _                   => "영업"
    };

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
