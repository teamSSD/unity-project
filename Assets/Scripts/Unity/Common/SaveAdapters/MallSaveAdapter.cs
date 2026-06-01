using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mall 도메인 ↔ GameSaveData 슬롯 변환.
/// OrderService(런타임 List&lt;DeliveryOrderData&gt;) ↔ OrderSaveData (entries, FoodData → id).
/// MallPersistent(questGroupIds/Stages) ↔ DeliveryQuestSaveData.
/// </summary>
public static class MallSaveAdapter
{
    public static void Capture(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;

        // 진행 중 주문 → OrderSaveData (FoodData → string id)
        var orderSvc = GameSessionRoot.Instance.Order;
        var saved = new OrderSaveData();
        foreach (var order in orderSvc.GetOrders())
        {
            saved.entries.Add(new OrderEntry
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
        save.orders = saved;

        // NPC 퀘스트 진행도
        var mp = GameSessionRoot.Instance.State.mall.persistent;
        save.deliveryQuest = new DeliveryQuestSaveData
        {
            groupIds = new List<string>(mp.questGroupIds),
            stages   = new List<int>(mp.questStages)
        };
    }

    public static void Apply(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;

        // 주문 복원 (FoodData ID → 객체 lookup)
        if (save.orders != null)
        {
            var orderSvc = GameSessionRoot.Instance.Order;
            var rebuilt = new List<DeliveryOrderData>();
            foreach (var entry in save.orders.entries)
            {
                var sideMenus = entry.sideMenuIds
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(id => SearchDataUtil.GetFoodDataById(id))
                    .Where(f => f != null)
                    .ToList();

                rebuilt.Add(new DeliveryOrderData
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
            orderSvc.SetOrders(rebuilt);
        }

        // NPC 단계 복원
        if (save.deliveryQuest != null)
        {
            var mp = GameSessionRoot.Instance.State.mall.persistent;
            mp.questGroupIds = new List<string>(save.deliveryQuest.groupIds);
            mp.questStages   = new List<int>(save.deliveryQuest.stages);
        }
    }
}
