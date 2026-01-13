using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour,
    IOrderReader,
    IOrderCommand
{
    public static OrderManager Instance;

    [SerializeField]
    private List<DeliveryOrderData> orders = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =====================
    // 조회
    // =====================
    public IReadOnlyList<DeliveryOrderData> GetOrders()
    {
        return orders;
    }

    public DeliveryOrderData GetOrder(string questId)
    {
        return orders.Find(o => o.questId == questId);
    }

    // =====================
    // 명령, 수정
    // =====================
    public void GenerateOrder(MenuSchema menu, string questId)
    {
        orders.Add(new DeliveryOrderData
        {
            questId = questId,
            orderNumber = menu.orderNumber,
            menuSchema = menu,
            state = DeliveryOrderState.Ordered
        });
    }

    public bool TryMarkCooked(string questId)
    {
        var order = GetOrder(questId);
        if (order == null) return false;
        if (order.state != DeliveryOrderState.Ordered) return false;

        order.state = DeliveryOrderState.Cooked;
        return true;
    }

    public int ConsumeBento(string questId)
    {
        var order = GetOrder(questId);
        if (order == null) return 0;

        orders.Remove(order);
        return 10000;
    }
}
