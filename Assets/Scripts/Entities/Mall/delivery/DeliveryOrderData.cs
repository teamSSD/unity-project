using System;
using UnityEngine;

public enum DeliveryOrderState
{
    Ordered,        // 주문만 된 상태
    Cooking,        // 요리 중
    Cooked,         // 요리 완료
    Failed,         // 요리 실패
    Delivered       // 배달 완료
}

[Serializable]
public class DeliveryOrderData
{
    public int orderNumber;
    public MenuSchema menuSchema;
    public DeliveryOrderState state;
}
