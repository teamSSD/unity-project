using System;
using System.Collections.Generic;
using Game.Domain.Common;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 재료 상점 구매 로직 (POCO Service). Phase 단위 라인업 갱신 지원.
    /// - Special: 페이즈마다 새 라인업 뽑음, 페이즈별 재고/구매수 트래킹, 페이즈 넘어가면 리셋.
    /// - General: 무한 매입, 재고 개념 없음.
    /// 캐시 키 = (day, phase). 재현성은 GameRandom.PhaseRandom(day, phase)로 보장.
    /// </summary>
    public class PurchaseService
    {
        private readonly ShopConfigSO _config;
        private readonly InventoryService _inventory;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        private readonly Dictionary<FoodData, int> _phasePurchased = new();
        private long _cachedKey = long.MinValue;
        private List<ItemShopSlotInfo> _cachedItemList;

        public PurchaseService(ShopConfigSO config, InventoryService inventory, IMoneyService money, IExpenseLog expense)
        {
            _config = config;
            _inventory = inventory;
            _money = money;
            _expense = expense;
        }

        private static long MakeKey(int day, int phaseIndex) => ((long)day << 8) | (uint)phaseIndex;

        /// <summary>
        /// (day, phase) 조합의 슬롯 리스트. 조합 바뀌면 새 라인업 뽑고 페이즈별 구매수 리셋.
        /// </summary>
        public IReadOnlyList<ItemShopSlotInfo> GetItemList(int day, int phaseIndex)
        {
            if (_config == null) return Array.Empty<ItemShopSlotInfo>();
            long key = MakeKey(day, phaseIndex);
            if (_cachedKey != key || _cachedItemList == null)
            {
                var rng = GameRandom.PhaseRandom(day, phaseIndex);
                _cachedItemList = _config.BuildSlotList(rng);
                _cachedKey = key;
                _phasePurchased.Clear();
            }
            return _cachedItemList;
        }

        public int GetPurchasedThisPhase(FoodData item)
            => _phasePurchased.TryGetValue(item, out var v) ? v : 0;

        public int GetRemaining(ItemShopSlotInfo info)
        {
            if (info == null) return 0;
            if (info.IsUnlimited) return int.MaxValue;
            return Math.Max(0, info.stock - GetPurchasedThisPhase(info.item));
        }

        public void NotifyPurchased(FoodData item, int qty)
        {
            _phasePurchased[item] = GetPurchasedThisPhase(item) + qty;
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
        /// 구매 트랜잭션: 잔액 차감 → 지출 기록 → 인벤토리 추가 → 페이즈별 구매수 누적.
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
