using System;
using System.Collections.Generic;
using Game.Domain.Common;
using Game.Schema.State.Shop;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 재료 상점 구매 로직 (POCO Service). Phase 단위 라인업 갱신 지원.
    /// - Special: 페이즈마다 새 라인업 뽑음, 페이즈별 재고/구매수 트래킹, 페이즈 넘어가면 리셋.
    /// - General: 무한 매입, 재고 개념 없음.
    /// 캐시 키 = (day, phase). 재현성은 GameRandom.PhaseRandom(day, phase, variant)로 보장.
    /// 새로고침 시 variant(=_refreshCount) 증가해 시드 축을 넘긴다 (페이즈 넘어가면 count 리셋).
    /// </summary>
    public class PurchaseService
    {
        /// <summary>새로고침 기본 비용 (골드). 실제 비용은 refreshCount 지수 증가.</summary>
        public const int RefreshBaseCost = 500;
        /// <summary>비용 증가 배율 (1.5배).</summary>
        public const float RefreshCostMultiplier = 1.5f;

        private readonly ShopConfigSO _config;
        private readonly InventoryService _inventory;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        private readonly ShopSessionState _state;
        private Dictionary<FoodData, int> PhasePurchased => _state.PurchasedByFood;

        public PurchaseService(ShopSessionState state, ShopConfigSO config, InventoryService inventory, IMoneyService money, IExpenseLog expense)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _config = config;
            _inventory = inventory;
            _money = money;
            _expense = expense;
        }
        public PurchaseService(ShopConfigSO config, InventoryService inventory, IMoneyService money, IExpenseLog expense)
            : this(new ShopSessionState(), config, inventory, money, expense) { }

        private static long MakeKey(int day, int phaseIndex) => ((long)day << 8) | (uint)phaseIndex;

        /// <summary>
        /// (day, phase) 조합의 슬롯 리스트. 조합 바뀌면 새 라인업 뽑고 페이즈별 구매수·새로고침 카운트 리셋.
        /// 같은 조합에서 새로고침으로 캐시 무효화됐으면 variant 시드로 재빌드.
        /// </summary>
        public IReadOnlyList<ItemShopSlotInfo> GetItemList(int day, int phaseIndex)
        {
            if (_config == null) return Array.Empty<ItemShopSlotInfo>();
            long key = MakeKey(day, phaseIndex);
            if (_state.CachedKey != key)
            {
                _state.CachedKey = key; PhasePurchased.Clear(); _state.RefreshCount = 0; _state.CachedItemList = null;
            }
            if (_state.CachedItemList == null)
            {
                var rng = GameRandom.PhaseRandom(day, phaseIndex, _state.RefreshCount);
                _state.CachedItemList = _config.BuildSlotList(rng);
            }
            return _state.CachedItemList;
        }

        public int GetPurchasedThisPhase(FoodData item)
            => PhasePurchased.TryGetValue(item, out var v) ? v : 0;

        public int GetRemaining(ItemShopSlotInfo info)
        {
            if (info == null) return 0;
            if (info.IsUnlimited) return int.MaxValue;
            return Math.Max(0, info.stock - GetPurchasedThisPhase(info.item));
        }

        public void NotifyPurchased(FoodData item, int qty)
        {
            PhasePurchased[item] = GetPurchasedThisPhase(item) + qty;
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
        /// 구매 거절 사유. null = 구매 가능. UI가 사유별 안내 팝업에 사용.
        /// </summary>
        public PurchaseRefusal? GetRefusalReason(FoodData food, int totalPrice)
        {
            if (food == null) return null;
            if (_money != null && _money.Current < totalPrice)
                return new PurchaseRefusal(PurchaseRefusalKind.Money, IngredientDisplayCategory.NONE, 0, 0);
            if (_inventory == null || _inventory.CanAcceptType(food)) return null;

            var usage = _inventory.GetCategoryUsage(food);
            var cat = food.ingredient?.display ?? IngredientDisplayCategory.NONE;
            int used = usage?.used ?? 0;
            int max = usage?.max ?? 0;
            return new PurchaseRefusal(PurchaseRefusalKind.Storage, cat, used, max);
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

        /// <summary>
        /// 현재 새로고침에 필요한 골드. refreshCount에 대해 지수 (1.5배) 증가.
        /// 예: 500, 750, 1125, 1687, 2531, 3796, ...
        /// </summary>
        public int GetRefreshCost()
        {
            return UnityEngine.Mathf.RoundToInt(RefreshBaseCost * UnityEngine.Mathf.Pow(RefreshCostMultiplier, _state.RefreshCount));
        }

        public bool CanRefresh() => _money != null && _money.Current >= GetRefreshCost();

        /// <summary>
        /// 상점 라인업 새로고침: 잔액 차감 → 지출 기록 → 새로고침 카운트 증가 → 캐시 무효화.
        /// 다음 GetItemList 호출 시 새 시드(_refreshCount)로 라인업 재빌드.
        /// 페이즈별 구매수는 유지 (새 슬롯이라 어차피 무의미하지만, 기존 슬롯 재출현 시 이력 참고).
        /// </summary>
        public bool TryRefresh(int day, int phaseIndex)
        {
            int cost = GetRefreshCost();
            if (!CanRefresh()) return false;
            if (!_money.TrySpend(cost)) return false;
            _expense?.Add("상점 새로고침", cost);
            _state.RefreshCount++; _state.CachedItemList = null; _state.CachedKey = MakeKey(day, phaseIndex);
            return true;
        }
    }
}
