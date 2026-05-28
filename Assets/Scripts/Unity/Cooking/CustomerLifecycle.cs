using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the lifecycle of a single customer from order to fulfillment
/// State flow: Ordering → Waiting → Taking/Exiting
/// </summary>
public class CustomerLifecycle
{
    private MenuSchema menuSchema;
    private CustomerData customerData;
    private CustomerSpawner spawner;
    private OrderTicketController ticketController;

    private WaitingCustomer waitingCustomer;
    private OrderTicketModel orderTicket;
    private int waitingPositionIndex = -1;

    public event Action<MenuValidator.ValidationResult, int> OnCustomerServed; // ValidationResult + reward
    public event Action OnCustomerLeft;

    public CustomerLifecycle(
        MenuSchema menuSchema,
        CustomerData customerData,
        CustomerSpawner spawner,
        OrderTicketController ticketController)
    {
        this.menuSchema = menuSchema;
        this.customerData = customerData;
        this.spawner = spawner;
        this.ticketController = ticketController;
    }

    /// <summary>
    /// Start the customer lifecycle by spawning ordering customer
    /// </summary>
    public GameObject StartOrder()
    {
        return spawner.SpawnOrderingCustomer(menuSchema, customerData, OnOrderPlaced);
    }

    /// <summary>
    /// Called when customer places order
    /// Transitions to waiting state
    /// </summary>
    private void OnOrderPlaced()
    {
        // Spawn waiting customer
        var (customer, posIndex) = spawner.SpawnWaitingCustomer(customerData);
        waitingCustomer = customer;
        waitingPositionIndex = posIndex;

        if (waitingCustomer == null)
        {
            Debug.LogError("[CustomerLifecycle] Failed to spawn waiting customer!");
            return;
        }

        // Create order ticket
        orderTicket = ticketController.CreateTicket(
            menuSchema,
            onTicketTaken: OnOrderFulfilled,
            onCustomerExit: OnCustomerTimeout
        );

        // Setup customer events
        waitingCustomer.onExit += OnCustomerTimeout;

        // Setup ticket events
        orderTicket.OnAttached += () => waitingCustomer.StopTimer();
        orderTicket.onTake += OnOrderDelivered;
    }

    /// <summary>
    /// Called when order is fulfilled (ticket attached to bento)
    /// </summary>
    private void OnOrderFulfilled()
    {
        // This is called when ticket is taken
        // Cleanup is handled in OnOrderDelivered
    }

    /// <summary>
    /// Called when order is delivered to customer
    /// Validates order and calculates reward
    /// </summary>
    private void OnOrderDelivered(FoodSchema mainMenu, List<FoodSchema> sideMenus, Vector3 position)
    {
        // Validate order
        MenuValidator.ValidationResult validation = MenuValidator.Validate(menuSchema, mainMenu, sideMenus);

        // Log validation result
        Debug.Log($"[CustomerLifecycle] Order #{menuSchema.orderNumber} - " +
                  $"Score: {validation.AccuracyScore:F2} ({MenuValidator.GetGrade(validation.AccuracyScore)}) - " +
                  $"{validation.FeedbackMessage}");

        // Calculate and award money (using actual food prices)
        int reward = MenuValidator.CalculateReward(menuSchema, mainMenu, sideMenus);
        StatsSystem.Instance.AddMoney(reward);

        Debug.Log($"[CustomerLifecycle] Reward: {reward}원 (Score: {validation.AccuracyScore:F2})");

        // Cleanup waiting customer
        if (waitingCustomer != null)
        {
            waitingCustomer.DestroyObject();
            waitingCustomer = null;
        }

        // Cleanup ticket
        if (orderTicket != null)
        {
            orderTicket.DestroyObject();
            orderTicket = null;
        }

        // Release waiting position
        if (waitingPositionIndex >= 0)
        {
            spawner.ReleaseWaitingPosition(waitingPositionIndex);
            waitingPositionIndex = -1;
        }

        // Spawn taking customer (with food)
        spawner.SpawnTakingCustomer(
            customerData,
            position,
            menuSchema,
            mainMenu,
            sideMenus,
            isExit: false,
            onCompleted: () => {
                // Notify completion with validation result and reward
                OnCustomerServed?.Invoke(validation, reward);
            }
        );
    }

    /// <summary>
    /// Called when customer times out and leaves without food
    /// </summary>
    private void OnCustomerTimeout()
    {
        // Cleanup waiting customer
        if (waitingCustomer != null)
        {
            waitingCustomer.DestroyObject();
            waitingCustomer = null;
        }

        // Cleanup ticket
        if (orderTicket != null)
        {
            ticketController.RemoveTicket(orderTicket);
            orderTicket.DestroyObject();
            orderTicket = null;
        }

        // Release waiting position
        if (waitingPositionIndex >= 0)
        {
            spawner.ReleaseWaitingPosition(waitingPositionIndex);
            waitingPositionIndex = -1;
        }

        // Spawn taking customer (exit)
        spawner.SpawnTakingCustomer(
            customerData,
            spawner.GetExitPosition(),
            menuSchema,
            null,
            null,
            isExit: true,
            onCompleted: () => {
                // Notify completion
                OnCustomerLeft?.Invoke();
            }
        );
    }

    /// <summary>
    /// Cleanup (called when lifecycle ends)
    /// </summary>
    public void Cleanup()
    {
        if (waitingCustomer != null)
        {
            waitingCustomer.onExit -= OnCustomerTimeout;
        }
    }
}
