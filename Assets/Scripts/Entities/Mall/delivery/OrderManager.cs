using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour, IOrder
{
    public static OrderManager Instance;

    public List<DeliveryOrderData> orders = new();

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

    public int GenerateOrder(MenuSchema menu, string questId)
    {
        var order = new DeliveryOrderData
        {
            questId = questId,
            orderNumber = menu.orderNumber,
            menuSchema = menu,
            state = DeliveryOrderState.Ordered
        };

        orders.Add(order);
        return orders.Count - 1;
    }

    public bool IsOrderComplete(string questId)
    {
        return orders.Exists(o =>
            o.questId == questId &&
            o.state == DeliveryOrderState.Cooked);
    }

    public int ConsumeBento(string questId)
    {
        var order = orders.Find(o => o.questId == questId);
        if (order == null) return 0;

        orders.Remove(order);
        return 10000;//order.menuSchema.price;//@@@@@@@@@@@@@@@
    }
}
