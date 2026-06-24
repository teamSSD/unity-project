using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cooking 씬 영수증(OrderTicket) 위치/생명주기 관리.
/// - 일반 ticket: 손님 waitingPositionIndex와 1:1 매핑 (0~4번 슬롯, 가로)
/// - delivery ticket: 6번 슬롯에 세로 스택 (멀티 quest 대비)
/// </summary>
public class OrderTicketController : MonoBehaviour
{
    private GameObject receiptPrefab;

    public void Inject(GameObject receiptPrefab)
    {
        this.receiptPrefab = receiptPrefab;
    }

    // 슬롯 배치 — 손님 대기 위치(CustomerSpawner)와 동일 X, 화면 상단 Y
    private Vector3 basePosition  = new Vector3(-8f, 4.474f, 0);
    private Vector3 slotOffset    = new Vector3(1.75f, 0, 0);
    private const int QuestSlotIndex = 5; // 6번째 자리
    private Vector3 questStackOffset = new Vector3(0, -1f, 0);

    private readonly Dictionary<int, OrderTicketModel> regularTickets = new(); // slotIndex → ticket
    private readonly List<OrderTicketModel> deliveryTickets = new();

    public int ActiveTicketCount => regularTickets.Count + deliveryTickets.Count;

    /// <summary>일반 손님 ticket. slotIndex는 손님 waitingPositionIndex와 동일하게 전달.</summary>
    public OrderTicketModel CreateTicket(MenuSchema menuSchema, int slotIndex, Action onTicketTaken, Action onCustomerExit)
    {
        var (ticketObj, ticketModel) = InstantiateTicket(menuSchema);

        Vector3 position = CalculateRegularPosition(slotIndex);
        ticketModel.SetDefaultPosition(position);
        ticketObj.transform.position = position + new Vector3(0, -2f, 0); // Spawn 아래에서 → 애니메이션으로 올라옴

        regularTickets[slotIndex] = ticketModel;

        ticketModel.onTake += (main, sides, pos) =>
        {
            RemoveTicket(ticketModel);
            onTicketTaken?.Invoke();
        };

        return ticketModel;
    }

    /// <summary>배달 ticket. 6번 자리에 세로로 쌓인다.</summary>
    public OrderTicketModel CreateDeliveryTicket(MenuSchema menuSchema)
    {
        var (ticketObj, ticketModel) = InstantiateTicket(menuSchema);

        deliveryTickets.Add(ticketModel);
        int stackIndex = deliveryTickets.Count - 1;

        Vector3 position = CalculateDeliveryPosition(stackIndex);
        ticketModel.SetDefaultPosition(position);
        ticketObj.transform.position = position + new Vector3(0, -2f, 0);

        ticketModel.onTake += (main, sides, pos) => RemoveTicket(ticketModel);

        return ticketModel;
    }

    private (GameObject, OrderTicketModel) InstantiateTicket(MenuSchema menuSchema)
    {
        GameObject ticketObj = Instantiate(receiptPrefab);
        OrderTicketModel ticketModel = ticketObj.GetComponent<OrderTicketModel>();
        Receipt receiptScript = ticketObj.GetComponent<Receipt>();
        ticketModel.SetMenu(menuSchema);
        receiptScript.Set(menuSchema);
        return (ticketObj, ticketModel);
    }

    public void RemoveTicket(OrderTicketModel ticket)
    {
        int slotKey = -1;
        foreach (var kv in regularTickets)
        {
            if (kv.Value == ticket) { slotKey = kv.Key; break; }
        }
        if (slotKey >= 0)
        {
            regularTickets.Remove(slotKey);
            return;
        }
        if (deliveryTickets.Remove(ticket))
        {
            RearrangeDelivery();
        }
    }

    private void RearrangeDelivery()
    {
        for (int i = 0; i < deliveryTickets.Count; i++)
            deliveryTickets[i].SetDefaultPosition(CalculateDeliveryPosition(i));
    }

    private Vector3 CalculateRegularPosition(int slotIndex)
        => basePosition + slotOffset * slotIndex;

    private Vector3 CalculateDeliveryPosition(int stackIndex)
        => basePosition + slotOffset * QuestSlotIndex + questStackOffset * stackIndex;

    public void ClearAllTickets()
    {
        foreach (var t in regularTickets.Values) if (t != null) Destroy(t.gameObject);
        regularTickets.Clear();
        foreach (var t in deliveryTickets) if (t != null) Destroy(t.gameObject);
        deliveryTickets.Clear();
    }

    public bool HasActiveTickets() => regularTickets.Count + deliveryTickets.Count > 0;

    /// <summary>일반(non-delivery) ticket 존재 여부 — 영업 종료 게이트 판정에 사용.</summary>
    public bool HasActiveCustomerTickets() => regularTickets.Count > 0;

    private void OnDestroy() => ClearAllTickets();
}
