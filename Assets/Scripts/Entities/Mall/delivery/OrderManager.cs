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

    public bool TryMarkCooked(string questId)
    {
        var order = GetOrder(questId);
        if (order == null) return false;
        if (order.state != DeliveryOrderState.Ordered) return false;

        order.state = DeliveryOrderState.Cooked;
        return true;
    }

    public int ConsumeBento(string questId)
    {/*
        var order = GetOrder(questId);
        if (order == null) return 0;

        MenuSchema menuSchema = order.menuSchema;
        FoodData mainMenu = menuSchema.mainMenu;
        List<FoodData> sideMenus = menuSchema.sideMenus;

        int totalPrice = 0;
       totalPrice += mainMenu.Price;
       sideMenus.ForEach(menu => totalPrice += menu.Price);

       int matchCount = 0;
       if (menuSchema.mainMenu.id == mainMenu.foodData.id) matchCount++;
       List<string> ids = sideMenus.Select(menu => menu.foodData.id).ToList();
       menuSchema.sideMenus.ForEach(menu =>
       {
           if (ids.Contains(menu.id)) matchCount++;
       });

       if (matchCount == menuSchema.sideMenus.Count + 1)
        {
            StatsSystem.AddMoney(totalPrice);
            return;
        }
        StatsSystem.AddMoney((int) (totalPrice * 0.7f));
        */
        return 1000;
    }
}
