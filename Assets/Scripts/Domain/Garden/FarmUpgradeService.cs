using System.Collections.Generic;
using System.Linq;
using Game.Domain.Common;
using Game.Schema.State.Garden;

namespace Game.Domain.Garden
{
    /// <summary>
    /// 농장 업그레이드 로직 (POCO Service).
    /// 상태(level)는 GardenPersistent에 보관, 골드 차감은 IMoneyService에 위임.
    /// </summary>
    public class FarmUpgradeService
    {
        private readonly GardenPersistent _state;
        private readonly Dictionary<string, List<FarmUpgradeData>> _table;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        public FarmUpgradeService(
            GardenPersistent state,
            IEnumerable<FarmUpgradeData> rows,
            IMoneyService money,
            IExpenseLog expense)
        {
            _state = state;
            _money = money;
            _expense = expense;
            _table = rows.GroupBy(r => r.type).ToDictionary(g => g.Key, g => g.ToList());

            // 누락된 type은 level=0으로 초기화
            foreach (var t in _table.Keys)
            {
                if (!_state.upgradeTypes.Contains(t))
                {
                    _state.upgradeTypes.Add(t);
                    _state.upgradeLevels.Add(0);
                }
            }
        }

        public FarmUpgradeData GetCurrentData(string type)
        {
            if (!_table.TryGetValue(type, out var list)) return null;
            int lv = GetLevel(type);
            return list.Find(d => d.level == lv);
        }

        public FarmUpgradeData GetNextData(string type)
        {
            if (!_table.TryGetValue(type, out var list)) return null;
            int lv = GetLevel(type);
            return list.Find(d => d.level == lv + 1);
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
            int idx = _state.upgradeTypes.IndexOf(type);
            return idx >= 0 ? _state.upgradeLevels[idx] : 0;
        }

        private void SetLevel(string type, int level)
        {
            int idx = _state.upgradeTypes.IndexOf(type);
            if (idx >= 0) _state.upgradeLevels[idx] = level;
            else
            {
                _state.upgradeTypes.Add(type);
                _state.upgradeLevels.Add(level);
            }
        }
    }
}
