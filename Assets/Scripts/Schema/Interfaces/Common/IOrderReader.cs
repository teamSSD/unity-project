using System.Collections.Generic;
using UnityEngine;

public interface IOrderReader
{
    IReadOnlyList<DeliveryOrderData> GetOrders();
    DeliveryOrderData GetOrder(string questId);
}
