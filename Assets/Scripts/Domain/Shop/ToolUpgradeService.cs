using System.Collections.Generic;
using System.Linq;
using Game.Domain.Common;
using Game.Schema.State.Shop;

namespace Game.Domain.Shop
{
    /// <summary>
    /// 조리 도구 업그레이드 로직 (POCO Service).
    /// 키는 toolId (T001~T005), ShopPersistent.toolIds/toolLevels에 보관.
    /// </summary>
    public class ToolUpgradeService
    {
        private readonly ShopPersistent _state;
        private readonly Dictionary<string, List<ToolUpgradeData>> _table;
        private readonly IMoneyService _money;
        private readonly IExpenseLog _expense;

        public ToolUpgradeService(
            ShopPersistent state,
            IEnumerable<ToolUpgradeData> rows,
            IMoneyService money,
            IExpenseLog expense)
        {
            _state = state;
            _money = money;
            _expense = expense;
            _table = rows.GroupBy(r => r.toolId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var id in _table.Keys)
            {
                if (!_state.toolIds.Contains(id))
                {
                    _state.toolIds.Add(id);
                    _state.toolLevels.Add(0);
                }
            }
        }

        public ToolUpgradeData GetCurrentData(string toolId)
        {
            if (!_table.TryGetValue(toolId, out var list)) return null;
            return list.Find(d => d.level == GetLevel(toolId));
        }

        public ToolUpgradeData GetNextData(string toolId)
        {
            if (!_table.TryGetValue(toolId, out var list)) return null;
            return list.Find(d => d.level == GetLevel(toolId) + 1);
        }

        public bool IsMax(string toolId) => GetNextData(toolId) == null;

        public bool TryUpgrade(string toolId)
        {
            var next = GetNextData(toolId);
            if (next == null) return false;
            if (!_money.TrySpend(next.cost)) return false;
            _expense.Add("업그레이드", next.cost);
            SetLevel(toolId, next.level);
            return true;
        }

        public IEnumerable<string> GetAllToolIds() => _table.Keys;

        private int GetLevel(string toolId)
        {
            int idx = _state.toolIds.IndexOf(toolId);
            return idx >= 0 ? _state.toolLevels[idx] : 0;
        }

        private void SetLevel(string toolId, int level)
        {
            int idx = _state.toolIds.IndexOf(toolId);
            if (idx >= 0) _state.toolLevels[idx] = level;
            else
            {
                _state.toolIds.Add(toolId);
                _state.toolLevels.Add(level);
            }
        }
    }
}
