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

    // sender(this) 인자 포함 — 구독자가 람다 없이 명명된 메서드로 구독 가능 (이벤트 누수 방지)
    public event Action<CustomerLifecycle, MenuValidator.ValidationResult, int> OnCustomerServed;
    public event Action<CustomerLifecycle> OnCustomerLeft;

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

        // Create order ticket — 손님 waitingPositionIndex와 1:1 매핑되는 슬롯에 배치
        orderTicket = ticketController.CreateTicket(
            menuSchema,
            slotIndex: posIndex,
            onTicketTaken: OnOrderFulfilled,
            onCustomerExit: OnCustomerTimeout
        );

        // Setup customer events
        waitingCustomer.onExit += OnCustomerTimeout;

        // Setup ticket events (명명 메서드 — Cleanup에서 정상 해제 가능)
        orderTicket.OnAttached += OnTicketAttached;
        orderTicket.onTake += OnOrderDelivered;
    }

    private void OnTicketAttached()
    {
        waitingCustomer?.StopTimer();
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
        GameSessionRoot.Instance?.Stats.AddMoney(reward);

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
                OnCustomerServed?.Invoke(this, validation, reward);
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
                OnCustomerLeft?.Invoke(this);
            }
        );
    }

    /// <summary>
    /// 주문 전(Ordering→Waiting 전환 미완료) + ticket 없음 = 외부 영향 없이 안전하게 제거 가능.
    /// 영업 종료 시 stuck lifecycle 청소용.
    /// </summary>
    public bool IsStuckPreOrder() => waitingCustomer == null && orderTicket == null;

    public string GetNpcId() => customerData != null ? customerData.npcId : null;

    /// <summary>
    /// Cleanup (called when lifecycle ends) — 모든 외부 이벤트 구독 해제.
    /// </summary>
    public void Cleanup()
    {
        if (waitingCustomer != null)
            waitingCustomer.onExit -= OnCustomerTimeout;

        if (orderTicket != null)
        {
            orderTicket.OnAttached -= OnTicketAttached;
            orderTicket.onTake -= OnOrderDelivered;
        }
    }
}
