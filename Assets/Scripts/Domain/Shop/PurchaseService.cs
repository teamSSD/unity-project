using System;
using System.Collections.Generic;
using Game.Domain.Common;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 재료 상점 구매 로직 (POCO Service). UnifiedShopManager에서 추출 + ShopDetailPanel.OnBuy 흡수 (Sprint 2-E).
    /// 일일 구매 누적 + 슬롯 캐시 + TryBuy 트랜잭션 (잔액/수용량/차감/지출/추가 일괄).
    /// 영구 상태 없음 (오늘 구매 캐시는 PassDay 시 today 변경으로 자동 리셋).
    /// </summary>
    public class PurchaseService
    {
        private readonly ShopConfigSO _config;
        private readonly InventoryService _inventory;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        private readonly Dictionary<FoodData, int> _dailyPurchased = new();
        private int _cachedDay = -1;
        private List<ItemShopSlotInfo> _cachedItemList;

        public PurchaseService(ShopConfigSO config, InventoryService inventory, IMoneyService money, IExpenseLog expense)
        {
            _config = config;
            _inventory = inventory;
            _money = money;
            _expense = expense;
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

        /// <summary>
        /// 구매 가능 여부 (UI 버튼 활성/비활성 판단용). 잔액 + 수용량 확인.
        /// </summary>
        public bool CanBuy(FoodData food, int totalPrice)
        {
            if (food == null || _money == null) return false;
            if (_money.Current < totalPrice) return false;
            if (_inventory != null && !_inventory.CanAcceptType(food)) return false;
            return true;
        }

        /// <summary>
        /// 구매 트랜잭션: 잔액 차감 → 지출 기록 → 인벤토리 추가 → 일일 캐시 누적.
        /// 모든 검증 통과 시에만 실행. ShopDetailPanel.OnBuy 흡수.
        /// </summary>
        public bool TryBuy(FoodData food, int qty, int unitPrice)
        {
            if (food == null || qty <= 0) return false;
            int total = qty * unitPrice;
            if (!CanBuy(food, total)) return false;
            if (!_money.TrySpend(total)) return false;
            _expense?.Add("재료 구매", total);
            _inventory?.AddFood(food, qty);
            NotifyPurchased(food, qty);
            return true;
        }
    }
}
