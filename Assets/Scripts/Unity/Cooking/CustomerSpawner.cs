using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Handles spawning and positioning of different customer types
/// Manages waiting positions as a resource pool
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    private GameObject orderingCustomerPrefab;
    private GameObject waitingCustomerPrefab;
    private GameObject takingCustomerPrefab;
    private Canvas worldCanvas;

    /// <summary>
    /// Inject prefab dependencies from CustomerManager
    /// </summary>
    public void Inject(GameObject orderingPrefab, GameObject waitingPrefab, GameObject takingPrefab, Canvas canvas)
    {
        orderingCustomerPrefab = orderingPrefab;
        waitingCustomerPrefab = waitingPrefab;
        takingCustomerPrefab = takingPrefab;
        worldCanvas = canvas;
    }

    // Waiting position management
    private List<int> availableWaitingPositions = new List<int> { 0, 1, 2, 3, 4 };
    private Vector3 waitingBasePosition = new Vector3(-11.37f, 0.5f, 0);
    private Vector3 waitingPositionOffset = new Vector3(2, 0, 0);
    private Vector3 waitingPositionVariance = new Vector3(0.8f, 0.5f, 0);

    // Taking customer positions
    private Vector3 exitPosition = new Vector3(-11.63f, -0.85f, 0);

    public int AvailableWaitingSlots => availableWaitingPositions.Count;
    public int MaxWaitingCustomers => 3;

    // Position Settings
    private Vector3 orderingPosition = new Vector3(3.02f, 0.21f, 0f); 

    /// <summary>
    /// Spawn an ordering customer (at counter)
    /// </summary>
    public GameObject SpawnOrderingCustomer(MenuSchema menuSchema, CustomerData customerData, Action onOrderPlaced)
    {
        GameObject customer = Instantiate(orderingCustomerPrefab);
        customer.transform.position = orderingPosition; // 위치 명시적 설정

        OrderingCustomer script = customer.GetComponent<OrderingCustomer>();
        script.Inject(menuSchema, customerData);
        script.onExit += () =>
        {
            onOrderPlaced?.Invoke();
            Destroy(customer);
        };

        return customer;
    }

    /// <summary>
    /// Spawn a waiting customer and reserve a waiting position
    /// Returns the waiting customer and the reserved position index
    /// </summary>
    public (WaitingCustomer customer, int positionIndex) SpawnWaitingCustomer(CustomerData customerData)
    {
        if (availableWaitingPositions.Count == 0)
        {
            Debug.LogWarning("[CustomerSpawner] No available waiting positions!");
            return (null, -1);
        }

        // Reserve a position
        int positionIndex = PickRandomWaitingPosition();

        // Spawn customer
        GameObject customerObj = Instantiate(waitingCustomerPrefab);
        WaitingCustomer waitingCustomer = customerObj.GetComponent<WaitingCustomer>();

        Vector3 position = CalculateWaitingPosition(positionIndex);
        customerObj.transform.position = position;
        waitingCustomer.inject(worldCanvas, customerData);

        return (waitingCustomer, positionIndex);
    }

    /// <summary>
    /// Spawn a taking customer (receiving order)
    /// </summary>
    public GameObject SpawnTakingCustomer(CustomerData customerData, Vector3 position,
        MenuSchema menuSchema, FoodSchema mainMenu, List<FoodSchema> sideMenus, bool isExit, Action onCompleted = null)
    {
        GameObject customer = Instantiate(takingCustomerPrefab);
        TakingCustomer script = customer.GetComponent<TakingCustomer>();

        customer.transform.position = position;
        script.customerData = customerData;

        if (isExit)
        {
            script.exit();
        }
        else
        {
            script.take(menuSchema, mainMenu, sideMenus);
        }

        Destroy(customer, 3f); // Auto-destroy after animation

        if (onCompleted != null)
        {
            InvokeAfterDelayAsync(3f, onCompleted).Forget();
        }

        return customer;
    }

    private async UniTaskVoid InvokeAfterDelayAsync(float delay, Action action)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());
        action?.Invoke();
    }

    /// <summary>
    /// Pick a random waiting position and reserve it
    /// </summary>
    private int PickRandomWaitingPosition()
    {
        if (availableWaitingPositions.Count == 0) return -1;

        int randomIndex = GameRandom.Pick(GameRandom.Variable, availableWaitingPositions);
        availableWaitingPositions.Remove(randomIndex);
        return randomIndex;
    }

    /// <summary>
    /// Release a waiting position back to the pool
    /// </summary>
    public void ReleaseWaitingPosition(int positionIndex)
    {
        if (!availableWaitingPositions.Contains(positionIndex))
        {
            availableWaitingPositions.Add(positionIndex);
        }
    }

    /// <summary>
    /// Calculate world position for waiting customer at given index
    /// </summary>
    private Vector3 CalculateWaitingPosition(int index)
    {
        Vector3 randomVariance = new Vector3(
            waitingPositionVariance.x * GameRandom.NormalRange(GameRandom.Variable, -1, 1),
            waitingPositionVariance.y * GameRandom.NormalRange(GameRandom.Variable, -1, 1),
            0
        );

        return waitingBasePosition + waitingPositionOffset * index + randomVariance;
    }

    /// <summary>
    /// Get exit position for customers leaving without order
    /// </summary>
    public Vector3 GetExitPosition()
    {
        return exitPosition;
    }

    /// <summary>
    /// Check if waiting queue is full
    /// </summary>
    public bool IsWaitingQueueFull()
    {
        return availableWaitingPositions.Count == 0;
    }

    /// <summary>
    /// Reset all waiting positions (for scene reset)
    /// </summary>
    public void ResetWaitingPositions()
    {
        availableWaitingPositions.Clear();
        for (int i = 0; i < MaxWaitingCustomers; i++)
        {
            availableWaitingPositions.Add(i);
        }
    }
}
