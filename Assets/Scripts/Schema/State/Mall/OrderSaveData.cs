using System;
using System.Collections.Generic;

/// <summary>
/// 배달 주문 디스크 직렬화 형식. GameSaveData.orders 슬롯.
/// MallSession(런타임 List&lt;DeliveryOrderData&gt;)와 SaveManager가 변환.
/// 디스크 호환 위해 기존 OrderManager.OrderSaveData / OrderEntry shape 그대로 보존.
/// </summary>
[Serializable]
public class OrderSaveData
{
    public List<OrderEntry> entries = new();
}

[Serializable]
public class OrderEntry
{
    public string questId;
    public int orderNumber;
    public string menuName;
    public string mainMenuId;
    public List<string> sideMenuIds = new();
    public int state;
    public string npcId;
    public int cookedPrice;
}
