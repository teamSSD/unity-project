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
    [SerializeField] private GameObject statManager;
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

        if (statManager != null)
            statManager.GetComponent<StatManager>().onTimeEnd += OnTimeEnd;
    }

    void Start()
    {
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
            case PhaseType.Morning:   baseSpawnInterval = 45f; spawnIntervalVariance = 10f; break;
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
        return customerDataList[GameRandom.Range(GameRandom.Variable, 0, customerDataList.Count)];
    }

    private void OnTimeEnd()
    {
        isOpen = false;
        if (currentOrderingCustomer != null)
        {
            Destroy(currentOrderingCustomer);
            currentOrderingCustomer = null;
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

        if (statManager != null)
        {
            var statMgr = statManager.GetComponent<StatManager>();
            if (statMgr != null) statMgr.onTimeEnd -= OnTimeEnd;
        }
    }

    public int GetActiveCustomerCount() => activeCustomers.Count;
    public int GetWaitingQueueSize() => spawner.MaxWaitingCustomers - spawner.AvailableWaitingSlots;
    public bool IsShopOpen() => isOpen;
    public (int total, int perfect, float avgScore, int earnings) GetSessionStats() => sessionStats.Snapshot();

    private void LogSessionSummary()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (sessionStats.TotalEarnings > 0 && SettlementManager.Instance != null && progress != null)
            SettlementManager.Instance.AddIncome(PhaseToLabel(progress.PhaseData.Phase), sessionStats.TotalEarnings);
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
