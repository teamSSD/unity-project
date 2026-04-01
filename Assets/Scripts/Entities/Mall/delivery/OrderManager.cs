using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrderManager : SingletonMonoBehaviour<OrderManager>,
    IOrderReader,
    IOrderCommand
{
    [SerializeField]
    private List<DeliveryOrderData> orders = new();

    private static string SavePath => Application.persistentDataPath + "/saves/orders";

    [System.Serializable]
    private class OrderSaveData
    {
        public List<OrderEntry> entries = new();
    }

    [System.Serializable]
    private class OrderEntry
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

    public void Initialize()
    {
        var data = DataSaveUtil.LoadData(new OrderSaveData(), SavePath);
        orders.Clear();
        foreach (var entry in data.entries)
        {
            var sideMenus = entry.sideMenuIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => SearchDataUtil.GetFoodDataById(id))
                .Where(f => f != null)
                .ToList();

            orders.Add(new DeliveryOrderData
            {
                questId = entry.questId,
                orderNumber = entry.orderNumber,
                menuSchema = new MenuSchema(
                    entry.menuName,
                    entry.orderNumber,
                    !string.IsNullOrEmpty(entry.mainMenuId)
                        ? SearchDataUtil.GetFoodDataById(entry.mainMenuId) : null,
                    sideMenus
                ),
                state = (DeliveryOrderState)entry.state,
                npcId = entry.npcId,
                cookedPrice = entry.cookedPrice
            });
        }

        Debug.Log($"[OrderManager] Initialized - {orders.Count} orders loaded");
    }

    public void flush()
    {
        var data = new OrderSaveData();
        foreach (var order in orders)
        {
            data.entries.Add(new OrderEntry
            {
                questId = order.questId,
                orderNumber = order.orderNumber,
                menuName = order.menuSchema?.name ?? "",
                mainMenuId = order.menuSchema?.mainMenu?.id ?? "",
                sideMenuIds = order.menuSchema?.sideMenus?
                    .Select(f => f?.id ?? "").ToList() ?? new List<string>(),
                state = (int)order.state,
                npcId = order.npcId,
                cookedPrice = order.cookedPrice
            });
        }
        DataSaveUtil.SaveData(data, SavePath);
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
    public void GenerateOrder(MenuSchema menu, string questId, string npcId)//퀘스트ID는 npc가 생성후 여기로 넘기는게 아니라 여기서 생성하는게 나을듯
    {
        orders.Add(new DeliveryOrderData
        {
            questId = questId,
            orderNumber = menu.orderNumber,
            menuSchema = menu,
            state = DeliveryOrderState.Ordered,
            npcId = npcId
        });
    }

    public bool MarkCookedWithPrice(string questId, int price)
    {
        var order = GetOrder(questId);
        if (order == null) return false;
        if (order.state != DeliveryOrderState.Ordered) return false;

        order.state = DeliveryOrderState.Cooked;
        order.cookedPrice = price;
        Debug.Log($"[OrderManager] Marked cooked: {questId}, price={price}원");
        return true;
    }

    public int ConsumeBento(string questId)
    {
        var order = GetOrder(questId);
        if (order == null) return 0;

        int price = order.cookedPrice;
        StatsSystem.AddMoney(price);
        order.state = DeliveryOrderState.Delivered;

        Debug.Log($"[OrderManager] Delivered: {questId}, reward={price}원");
        return price;
    }
}
