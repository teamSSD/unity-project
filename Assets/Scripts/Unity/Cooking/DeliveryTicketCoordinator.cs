using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배달 주문 ticket 생성 및 onTake 람다 해제 책임. CustomerManager에서 추출.
/// CreateDeliveryTickets + Dictionary 추적 + OnDestroy 일괄 해제.
/// </summary>
[RequireComponent(typeof(OrderTicketController))]
public class DeliveryTicketCoordinator : MonoBehaviour
{
    private OrderTicketController ticketController;

    // 배달 ticket 추적 — onTake 람다 구독 해제용
    private readonly Dictionary<OrderTicketModel, Action<FoodSchema, List<FoodSchema>, Vector3>> handlers = new();

    private void Awake()
    {
        ticketController = GetComponent<OrderTicketController>();
    }

    /// <summary>등록된 주문(state=Ordered)을 모두 ticket으로 변환.</summary>
    public void CreateAll()
    {
        var orderSvc = GameSessionRoot.Instance?.Order;
        if (orderSvc == null) return;

        foreach (var order in orderSvc.GetOrders())
        {
            if (order.state != DeliveryOrderState.Ordered) continue;
            CreateOne(order);
        }
    }

    private void CreateOne(DeliveryOrderData order)
    {
        var ticket = ticketController.CreateDeliveryTicket(order.menuSchema);
        ticket.IsDelivery = true;
        ticket.QuestId = order.questId;
        ticket.GetComponent<Receipt>().Set(order.menuSchema, isDelivery: true);

        // 람다를 변수에 저장 → OnDestroy에서 -=로 정상 해제 (누수 차단)
        var capturedOrder = order;
        Action<FoodSchema, List<FoodSchema>, Vector3> handler = (main, sides, pos) =>
        {
            int totalPrice = 0;
            if (sides != null)
                foreach (var food in sides)
                    if (food != null) totalPrice += food.Price;

            GameSessionRoot.Instance?.Order.MarkCookedWithPrice(capturedOrder.questId, totalPrice);
        };
        ticket.onTake += handler;
        handlers[ticket] = handler;
    }

    private void OnDestroy()
    {
        foreach (var kv in handlers)
            if (kv.Key != null) kv.Key.onTake -= kv.Value;
        handlers.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
