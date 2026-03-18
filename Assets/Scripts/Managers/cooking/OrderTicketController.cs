using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages order tickets (receipts) in the cooking scene
/// Handles creation, positioning, and lifecycle of order tickets
/// </summary>
public class OrderTicketController : MonoBehaviour
{
    private GameObject receiptPrefab;

    /// <summary>
    /// Inject receipt prefab from CustomerManager
    /// </summary>
    public void Inject(GameObject receiptPrefab)
    {
        this.receiptPrefab = receiptPrefab;
    }

    private List<OrderTicketModel> activeTickets = new List<OrderTicketModel>();
    private Vector3 basePosition = new Vector3(7.65f, 0.94f, 0);
    private Vector3 verticalOffset = new Vector3(0, -2.38f, 0);

    public int ActiveTicketCount => activeTickets.Count;

    /// <summary>
    /// Create a new order ticket for the given menu
    /// </summary>
    public OrderTicketModel CreateTicket(MenuSchema menuSchema, Action onTicketTaken, Action onCustomerExit)
    {
        GameObject ticketObj = Instantiate(receiptPrefab);
        OrderTicketModel ticketModel = ticketObj.GetComponent<OrderTicketModel>();
        Receipt receiptScript = ticketObj.GetComponent<Receipt>();

        // Setup ticket
        ticketModel.SetMenu(menuSchema);
        receiptScript.Set(menuSchema);

        // Calculate position
        Vector3 position = CalculateTicketPosition(activeTickets.Count);
        ticketModel.SetDefaultPosition(position);
        ticketObj.transform.position = position + new Vector3(0, -2f, 0); // Spawn below, will animate up

        // Register ticket
        activeTickets.Add(ticketModel);

        // Setup events
        ticketModel.onTake += (main, sides, pos) =>
        {
            RemoveTicket(ticketModel);
            onTicketTaken?.Invoke();
        };

        return ticketModel;
    }

    /// <summary>
    /// Remove a ticket and rearrange remaining tickets
    /// </summary>
    public void RemoveTicket(OrderTicketModel ticket)
    {
        if (activeTickets.Remove(ticket))
        {
            RearrangeTickets();
        }
    }

    /// <summary>
    /// Rearrange all active tickets to fill gaps
    /// </summary>
    private void RearrangeTickets()
    {
        for (int i = 0; i < activeTickets.Count; i++)
        {
            Vector3 newPosition = CalculateTicketPosition(i);
            activeTickets[i].SetDefaultPosition(newPosition);
        }
    }

    /// <summary>
    /// Calculate position for ticket at given index
    /// </summary>
    private Vector3 CalculateTicketPosition(int index)
    {
        return basePosition + verticalOffset * index;
    }

    /// <summary>
    /// Clear all tickets (for scene cleanup)
    /// </summary>
    public void ClearAllTickets()
    {
        foreach (var ticket in activeTickets)
        {
            if (ticket != null)
            {
                Destroy(ticket.gameObject);
            }
        }
        activeTickets.Clear();
    }

    /// <summary>
    /// Check if there are any active tickets
    /// </summary>
    public bool HasActiveTickets()
    {
        return activeTickets.Count > 0;
    }

    private void OnDestroy()
    {
        ClearAllTickets();
    }
}
