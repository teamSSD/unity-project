using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages customer flow in the cooking scene
/// Refactored to use component-based architecture with CustomerSpawner,
/// OrderTicketController, and CustomerLifecycle
/// </summary>
[RequireComponent(typeof(CustomerSpawner))]
[RequireComponent(typeof(OrderTicketController))]
public class CustomerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject statManager;
    [SerializeField] private AudioClip doorSfx;
    [SerializeField] private List<CustomerData> customerDataList;

    [Header("Prefabs")]
    [SerializeField] private GameObject orderingCustomerPrefab;
    [SerializeField] private GameObject waitingCustomerPrefab;
    [SerializeField] private GameObject takingCustomerPrefab;
    [SerializeField] private GameObject receiptPrefab;
    [SerializeField] private Canvas worldCanvas;

    [Header("Spawn Settings (페이즈별 자동 설정)")]
    private float baseSpawnInterval = 30f;
    private float spawnIntervalVariance = 8f;

    // Components
    private CustomerSpawner spawner;
    private OrderTicketController ticketController;

    // State
    private List<MenuSchema> salesMenus;
    private List<CustomerLifecycle> activeCustomers = new List<CustomerLifecycle>();
    // 배달 ticket 추적 — onTake 람다 구독 해제용 (OnDestroy에서 -= 가능하도록 mapping 보관)
    private readonly Dictionary<OrderTicketModel, Action<FoodSchema, List<FoodSchema>, Vector3>> deliveryTicketHandlers = new();
    private int nextOrderNumber = 1;
    private bool isOpen = true;

    // Session statistics
    private int totalOrders = 0;
    private int perfectOrders = 0;
    private float totalAccuracyScore = 0f;
    private int totalEarnings = 0;

    // Spawn timing
    private float timer = 0;
    private float nextSpawnTime;
    private GameObject currentOrderingCustomer;

    // Events
    public event Action OnGameEnd;

    void Awake()
    {
        // Get components
        spawner = GetComponent<CustomerSpawner>();
        ticketController = GetComponent<OrderTicketController>();

        // Inject prefab dependencies
        spawner.Inject(orderingCustomerPrefab, waitingCustomerPrefab, takingCustomerPrefab, worldCanvas);
        ticketController.Inject(receiptPrefab);

        // Load today's menu
        ISelectMenu menuProvider = new RecipeBookMenuProvider();
        salesMenus = menuProvider.GetTodaysMenu();

        if (salesMenus.Count == 0)
        {
            Debug.LogError("[CustomerManager] No menus available! Cannot start cooking scene without menu selection.");
            isOpen = false;
            return;
        }

        // Setup
        // Stamina는 GameStart의 StatsSystem.Initialize()에서 로드됨
        // CustomerManager는 stamina를 초기화하지 않음

        // Listen to time end event
        if (statManager != null)
        {
            statManager.GetComponent<StatManager>().onTimeEnd += OnTimeEnd;
        }

    }

    void Start()
    {
        ApplyPhaseSettings();
        CreateDeliveryTickets();
        nextSpawnTime = GameRandom.Normal(GameRandom.Variable, 5f, 3f); // 첫 손님은 빠르게

        var subScene = FindFirstObjectByType<SubSceneController>();
        if (subScene != null)
            OnGameEnd += subScene.ReturnToIdle;
    }

    private void ApplyPhaseSettings()
    {
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData?.Phase ?? PhaseType.Morning;
        switch (phase)
        {
            case PhaseType.Morning:
                baseSpawnInterval = 45f;
                spawnIntervalVariance = 10f;
                break;
            case PhaseType.Afternoon:
                baseSpawnInterval = 30f;
                spawnIntervalVariance = 8f;
                break;
            case PhaseType.Evening:
                baseSpawnInterval = 22f;
                spawnIntervalVariance = 5f;
                break;
            case PhaseType.Night:
                baseSpawnInterval = 15f;
                spawnIntervalVariance = 4f;
                break;
        }
    }

    private void CreateDeliveryTickets()
    {
        if (GameSessionRoot.Instance?.Order == null) return;

        foreach (var order in GameSessionRoot.Instance?.Order.GetOrders())
        {
            if (order.state != DeliveryOrderState.Ordered) continue;

            var ticket = ticketController.CreateTicket(
                order.menuSchema,
                onTicketTaken: null,
                onCustomerExit: null
            );
            ticket.IsDelivery = true;
            ticket.QuestId = order.questId;

            // 배달 표시로 영수증 재설정
            ticket.GetComponent<Receipt>().Set(order.menuSchema, isDelivery: true);

            // 배달 onTake: 가격 계산 + Cooked 전환
            // 람다를 변수에 저장해 OnDestroy에서 -=로 정상 해제 (이전: 익명 람다 → 누수)
            var capturedOrder = order;
            Action<FoodSchema, List<FoodSchema>, Vector3> handler = (main, sides, pos) =>
            {
                int totalPrice = 0;
                if (sides != null)
                    foreach (var food in sides)
                        if (food != null) totalPrice += food.Price;

                GameSessionRoot.Instance?.Order.MarkCookedWithPrice(
                    capturedOrder.questId, totalPrice);
            };
            ticket.onTake += handler;
            deliveryTicketHandlers[ticket] = handler;
        }
    }

    void Update()
    {
        if (!isOpen) return;

        HandleCustomerSpawning();
    }

    /// <summary>
    /// Handle spawning of new customers based on timer
    /// </summary>
    private void HandleCustomerSpawning()
    {
        timer += Time.deltaTime;

        if (timer < nextSpawnTime) return;

        timer -= nextSpawnTime;
        nextSpawnTime = GameRandom.Normal(GameRandom.Variable, baseSpawnInterval, spawnIntervalVariance);

        // Don't spawn if there's already an ordering customer or waiting queue is full
        if (currentOrderingCustomer != null || spawner.IsWaitingQueueFull())
        {
            return;
        }

        // Pick random menu and customer
        MenuSchema menu = PickRandomMenu();
        if (menu == null) return;

        CustomerData customerData = PickRandomCustomerData();

        // Create customer lifecycle
        CustomerLifecycle lifecycle = new CustomerLifecycle(
            menu,
            customerData,
            spawner,
            ticketController
        );

        // Setup events (명명 메서드 — Cleanup에서 정상 해제. lifecycle은 sender 인자로 전달됨)
        lifecycle.OnCustomerServed += OnCustomerServed;
        lifecycle.OnCustomerLeft += OnCustomerLeft;

        // Start customer order
        currentOrderingCustomer = lifecycle.StartOrder();
        activeCustomers.Add(lifecycle);

        // Play door sound
        if (doorSfx != null)
        {
            SoundManager.Instance.Play2DSFX(doorSfx, 0.7f);
        }

    }

    /// <summary>
    /// Called when customer is served with food (CustomerLifecycle.OnCustomerServed 핸들러)
    /// </summary>
    private void OnCustomerServed(CustomerLifecycle lifecycle, MenuValidator.ValidationResult validation, int reward)
    {
        // Track statistics
        totalOrders++;
        totalAccuracyScore += validation.AccuracyScore;

        if (validation.AccuracyScore >= 1.0f)
        {
            perfectOrders++;
        }

        totalEarnings += reward;

        Debug.Log($"[CustomerManager] Order served - Grade: {MenuValidator.GetGrade(validation.AccuracyScore)} " +
                  $"({validation.AccuracyScore:F2}) - Total Orders: {totalOrders}");

        // Show visual feedback
        if (ValidationFeedbackUI.Instance != null)
        {
            ValidationFeedbackUI.Instance.ShowFeedback(
                MenuValidator.GetGrade(validation.AccuracyScore),
                validation.AccuracyScore,
                reward,
                validation.FeedbackMessage
            );
        }

        // Cleanup lifecycle
        OnCustomerCompleted(lifecycle);
    }

    /// <summary>
    /// Called when customer leaves without food (timeout)
    /// </summary>
    private void OnCustomerLeft(CustomerLifecycle lifecycle)
    {
        OnCustomerCompleted(lifecycle);
    }

    /// <summary>
    /// Called when customer lifecycle completes (served or left).
    /// 매니저가 lifecycle에 구독한 이벤트도 여기서 해제 (누수 방지).
    /// </summary>
    private void OnCustomerCompleted(CustomerLifecycle lifecycle)
    {
        // Unsubscribe events first — lifecycle.Cleanup() 호출 전에 해제하여 재진입 가능성 차단
        lifecycle.OnCustomerServed -= OnCustomerServed;
        lifecycle.OnCustomerLeft -= OnCustomerLeft;

        lifecycle.Cleanup();
        activeCustomers.Remove(lifecycle);

        // Clear ordering customer reference if it was from this lifecycle
        currentOrderingCustomer = null;

        // Check for game end
        CheckGameEnd();

    }

    /// <summary>
    /// Pick a random menu from available menus
    /// </summary>
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

    /// <summary>
    /// Pick a random customer data
    /// </summary>
    private CustomerData PickRandomCustomerData()
    {
        if (customerDataList.Count == 0)
        {
            Debug.LogWarning("[CustomerManager] No customer data available!");
            return null;
        }

        return customerDataList[GameRandom.Range(GameRandom.Variable, 0, customerDataList.Count)];
    }

    /// <summary>
    /// Called when time ends
    /// </summary>
    private void OnTimeEnd()
    {
        isOpen = false;

        // Destroy current ordering customer if exists
        if (currentOrderingCustomer != null)
        {
            Destroy(currentOrderingCustomer);
            currentOrderingCustomer = null;
        }

        CheckGameEnd();

    }

    /// <summary>
    /// Check if game should end (shop closed + no active tickets)
    /// </summary>
    private void CheckGameEnd()
    {
        if (!isOpen && !ticketController.HasActiveCustomerTickets())
        {
            LogSessionSummary();
            OnGameEnd?.Invoke();
        }
    }

    /// <summary>
    /// Cleanup on destroy. 잔존 lifecycle + 배달 ticket 람다 핸들러 모두 해제.
    /// </summary>
    private void OnDestroy()
    {
        // Cleanup all active customers (구독한 이벤트 -= + lifecycle 정리)
        foreach (var lifecycle in activeCustomers)
        {
            lifecycle.OnCustomerServed -= OnCustomerServed;
            lifecycle.OnCustomerLeft -= OnCustomerLeft;
            lifecycle.Cleanup();
        }
        activeCustomers.Clear();

        // 배달 ticket onTake 람다 해제 (CreateDeliveryTickets에서 등록한 것들)
        foreach (var kv in deliveryTicketHandlers)
        {
            if (kv.Key != null) kv.Key.onTake -= kv.Value;
        }
        deliveryTicketHandlers.Clear();

        // Unsubscribe from events
        if (statManager != null)
        {
            var statMgr = statManager.GetComponent<StatManager>();
            if (statMgr != null)
            {
                statMgr.onTimeEnd -= OnTimeEnd;
            }
        }
    }

    /// <summary>
    /// Get active customer count (for debugging/UI)
    /// </summary>
    public int GetActiveCustomerCount()
    {
        return activeCustomers.Count;
    }

    /// <summary>
    /// Get waiting queue status (for debugging/UI)
    /// </summary>
    public int GetWaitingQueueSize()
    {
        return spawner.MaxWaitingCustomers - spawner.AvailableWaitingSlots;
    }

    /// <summary>
    /// Check if shop is open
    /// </summary>
    public bool IsShopOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// Get session statistics
    /// </summary>
    public (int total, int perfect, float avgScore, int earnings) GetSessionStats()
    {
        float avgScore = totalOrders > 0 ? totalAccuracyScore / totalOrders : 0f;
        return (totalOrders, perfectOrders, avgScore, totalEarnings);
    }

    /// <summary>
    /// Log session summary
    /// </summary>
    private void LogSessionSummary()
    {
        float avgScore = totalOrders > 0 ? totalAccuracyScore / totalOrders : 0f;
        float perfectRate = totalOrders > 0 ? (float)perfectOrders / totalOrders * 100f : 0f;


        var progress = GameSessionRoot.Instance?.Progress;
        if (totalEarnings > 0 && SettlementManager.Instance != null && progress != null)
            SettlementManager.Instance.AddIncome(PhaseToLabel(progress.PhaseData.Phase), totalEarnings);
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
