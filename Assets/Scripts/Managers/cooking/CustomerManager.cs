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

    [Header("Spawn Settings")]
    [SerializeField] private float baseSpawnInterval = 5f;
    [SerializeField] private float spawnIntervalVariance = 3f;

    // Components
    private CustomerSpawner spawner;
    private OrderTicketController ticketController;

    // State
    private List<MenuSchema> salesMenus;
    private List<CustomerLifecycle> activeCustomers = new List<CustomerLifecycle>();
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

        Debug.Log($"[CustomerManager] Initialized with {salesMenus.Count} menus");
    }

    void Start()
    {
        CreateDeliveryTickets();
        nextSpawnTime = RandomNormal.Get(baseSpawnInterval, spawnIntervalVariance);

        var subScene = FindFirstObjectByType<SubSceneController>();
        if (subScene != null)
            OnGameEnd += subScene.ReturnToIdle;
    }

    private void CreateDeliveryTickets()
    {
        if (OrderManager.Instance == null) return;

        foreach (var order in OrderManager.Instance.GetOrders())
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
            var capturedOrder = order;
            ticket.onTake += (main, sides, pos) =>
            {
                int totalPrice = 0;
                if (sides != null)
                    foreach (var food in sides)
                        if (food != null) totalPrice += food.Price;

                OrderManager.Instance.MarkCookedWithPrice(
                    capturedOrder.questId, totalPrice);
            };
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
        nextSpawnTime = RandomNormal.Get(baseSpawnInterval, spawnIntervalVariance);

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

        // Setup events
        lifecycle.OnCustomerServed += (validation, reward) => OnCustomerServed(lifecycle, validation, reward);
        lifecycle.OnCustomerLeft += () => OnCustomerLeft(lifecycle);

        // Start customer order
        currentOrderingCustomer = lifecycle.StartOrder();
        activeCustomers.Add(lifecycle);

        // Play door sound
        if (doorSfx != null)
        {
            SoundManager.Instance.Play2DSFX(doorSfx, 0.7f);
        }

        Debug.Log($"[CustomerManager] Spawned customer #{menu.orderNumber}");
    }

    /// <summary>
    /// Called when customer is served with food
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
        Debug.Log("[CustomerManager] Customer left without food");
        OnCustomerCompleted(lifecycle);
    }

    /// <summary>
    /// Called when customer lifecycle completes (served or left)
    /// </summary>
    private void OnCustomerCompleted(CustomerLifecycle lifecycle)
    {
        lifecycle.Cleanup();
        activeCustomers.Remove(lifecycle);

        // Clear ordering customer reference if it was from this lifecycle
        currentOrderingCustomer = null;

        // Check for game end
        CheckGameEnd();

        Debug.Log($"[CustomerManager] Customer completed. Active: {activeCustomers.Count}");
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

        MenuSchema menu = salesMenus[Random.Range(0, salesMenus.Count)];
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

        return customerDataList[Random.Range(0, customerDataList.Count)];
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

        Debug.Log("[CustomerManager] Shop closed");
    }

    /// <summary>
    /// Check if game should end (shop closed + no active tickets)
    /// </summary>
    private void CheckGameEnd()
    {
        if (!isOpen && !ticketController.HasActiveTickets())
        {
            Debug.Log("[CustomerManager] Game End");
            LogSessionSummary();
            OnGameEnd?.Invoke();
        }
    }

    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    private void OnDestroy()
    {
        // Cleanup all active customers
        foreach (var lifecycle in activeCustomers)
        {
            lifecycle.Cleanup();
        }
        activeCustomers.Clear();

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

        Debug.Log("========== Session Summary ==========");
        Debug.Log($"Total Orders: {totalOrders}");
        Debug.Log($"Perfect Orders: {perfectOrders} ({perfectRate:F1}%)");
        Debug.Log($"Average Score: {avgScore:F2} ({MenuValidator.GetGrade(avgScore)})");
        Debug.Log($"Total Earnings: {totalEarnings}원");
        Debug.Log("=====================================");
    }
}
