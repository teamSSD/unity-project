using System.Collections.Generic;
using System.Linq;
using Game.Domain.Common;
using Game.Schema.State.Shop;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 저장소(냉장고/선반) 업그레이드 로직 (POCO Service).
    /// 패턴: FarmUpgradeService와 동일 (Storage 데이터 타입과 ShopPersistent.storageX 사용).
    /// </summary>
    public class StorageUpgradeService
    {
        private readonly ShopPersistent _state;
        private readonly Dictionary<string, List<StorageUpgradeData>> _table;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        public StorageUpgradeService(
            ShopPersistent state,
            IEnumerable<StorageUpgradeData> rows,
            IMoneyService money,
            IExpenseLog expense)
        {
            _state = state;
            _money = money;
            _expense = expense;
            _table = rows.GroupBy(r => r.type).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var t in _table.Keys)
            {
                if (!_state.storageTypes.Contains(t))
                {
                    _state.storageTypes.Add(t);
                    _state.storageLevels.Add(0);
                }
            }
        }

        public StorageUpgradeData GetCurrentData(string type)
        {
            if (!_table.TryGetValue(type, out var list)) return null;
            return list.Find(d => d.level == GetLevel(type));
        }

        public StorageUpgradeData GetNextData(string type)
        {
            if (!_table.TryGetValue(type, out var list)) return null;
            return list.Find(d => d.level == GetLevel(type) + 1);
        }

        public bool IsMax(string type) => GetNextData(type) == null;

        public bool TryUpgrade(string type)
        {
            var next = GetNextData(type);
            if (next == null) return false;
            if (!_money.TrySpend(next.cost)) return false;
            _expense.Add("업그레이드", next.cost);
            SetLevel(type, next.level);
            return true;
        }

        public IEnumerable<string> GetAllTypes() => _table.Keys;

        private int GetLevel(string type)
        {
            int idx = _state.storageTypes.IndexOf(type);
            return idx >= 0 ? _state.storageLevels[idx] : 0;
        }

        private void SetLevel(string type, int level)
        {
            int idx = _state.storageTypes.IndexOf(type);
            if (idx >= 0) _state.storageLevels[idx] = level;
            else
            {
                _state.storageTypes.Add(type);
                _state.storageLevels.Add(level);
            }
        }
    }
}
