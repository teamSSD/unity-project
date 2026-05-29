using System;
using System.Collections.Generic;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 재료 상점 구매 로직 (POCO Service). UnifiedShopManager에서 추출.
    /// 일일 구매 누적 + 슬롯 캐시 보관. UI(ShopUIAdapter)는 이 서비스만 호출.
    /// 영구 상태 없음 (오늘 구매 캐시는 PassDay 시 today 변경으로 자동 리셋).
    /// </summary>
    public class PurchaseService
    {
        private readonly ShopConfigSO _config;
        private readonly Dictionary<FoodData, int> _dailyPurchased = new();
        private int _cachedDay = -1;
        private List<ItemShopSlotInfo> _cachedItemList;

        public PurchaseService(ShopConfigSO config)
        {
            _config = config;
        }

        /// <summary>
        /// 오늘의 슬롯 리스트. 날짜 바뀌면 재구축 + 일일 구매 캐시 리셋.
        /// </summary>
        public IReadOnlyList<ItemShopSlotInfo> GetItemListForDay(int today)
        {
            if (_config == null) return Array.Empty<ItemShopSlotInfo>();
            if (_cachedDay != today || _cachedItemList == null)
            {
                _cachedItemList = _config.BuildSlotList();
                _cachedDay = today;
                _dailyPurchased.Clear();
            }
            return _cachedItemList;
        }

        public int GetPurchasedToday(FoodData item)
            => _dailyPurchased.TryGetValue(item, out var v) ? v : 0;

        public int GetRemaining(ItemShopSlotInfo info)
            => Math.Max(0, info.stock - GetPurchasedToday(info.item));

        public void NotifyPurchased(FoodData item, int qty)
        {
            _dailyPurchased[item] = GetPurchasedToday(item) + qty;
        }
    }
}
