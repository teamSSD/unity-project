using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
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

    public int AddOrder(MenuSchema menu)
    {
        var order = new DeliveryOrderData
        {
            orderNumber = menu.orderNumber,
            menuSchema = menu,
            state = DeliveryOrderState.Ordered
        };

        orders.Add(order);
        return orders.Count - 1; // 인덱스 반환
    }
}
