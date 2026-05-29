using System.Collections.Generic;
using Game.Domain.Common;

namespace Game.Domain.Mall
{
    /// <summary>
    /// 배달 주문 로직 (POCO Service). OrderManager에서 추출.
    /// IOrderReader + IOrderCommand 구현. 직렬화는 SaveManager가 GetOrders()로 읽고
    /// SetOrders()로 일괄 주입 (디스크 호환).
    /// UnlockRecipe 같은 부수효과는 호출자(NPC interaction)에 위임 — 서비스는 주문 상태만.
    /// </summary>
    public class OrderService : IOrderReader, IOrderCommand
    {
        private readonly List<DeliveryOrderData> _orders = new();
        private readonly IMoneyService _money;

        public OrderService(IMoneyService money)
        {
            _money = money;
        }

        public IReadOnlyList<DeliveryOrderData> GetOrders() => _orders;

        public DeliveryOrderData GetOrder(string questId)
            => _orders.Find(o => o.questId == questId);

        public void GenerateOrder(MenuSchema menu, string questId, string npcId)
        {
            _orders.Add(new DeliveryOrderData
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
            return true;
        }

        public int ConsumeBento(string questId)
        {
            var order = GetOrder(questId);
            if (order == null) return 0;

            int reward = order.cookedPrice;
            _money?.Add(reward);
            order.state = DeliveryOrderState.Delivered;
            return reward;
        }

        public void Clear() => _orders.Clear();

        /// <summary>
        /// SaveManager 전용: 직렬화된 주문 데이터를 통째로 주입 (load 경로).
        /// </summary>
        public void SetOrders(IEnumerable<DeliveryOrderData> orders)
        {
            _orders.Clear();
            _orders.AddRange(orders);
        }
    }
}
