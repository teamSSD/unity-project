using System.Collections.Generic;
using Game.Schema.State.Mall;

namespace Game.Domain.Mall
{
    /// <summary>
    /// 일별 정산 (수입/지출) 누적 + 스냅샷 (POCO). SettlementManager facade 후속.
    /// PassDay 직전에 Reset()으로 갱신 후 초기화.
    /// </summary>
    public class SettlementService
    {
        public const int ManagementFee = 1000;

        private readonly MallSessionState _state;
        private Dictionary<string, int> IncomeMap => _state.SettlementIncome;
        private Dictionary<string, int> ExpenseMap => _state.SettlementExpense;
        public int DayStartMoney => _state.SettlementDayStartMoney;

        public SettlementService(MallSessionState state) => _state = state ?? throw new System.ArgumentNullException(nameof(state));
        public SettlementService() : this(new MallSessionState()) { }

        public void AddIncome(string label, int amount)
        {
            if (amount <= 0) return;
            IncomeMap[label] = IncomeMap.GetValueOrDefault(label) + amount;
        }

        public void AddExpense(string label, int amount)
        {
            if (amount <= 0) return;
            ExpenseMap[label] = ExpenseMap.GetValueOrDefault(label) + amount;
        }

        public IEnumerable<(string label, int amount)> GetIncomeEntries()
        {
            foreach (var kv in IncomeMap) yield return (kv.Key, kv.Value);
        }

        public IEnumerable<(string label, int amount)> GetExpenseEntries()
        {
            foreach (var kv in ExpenseMap) yield return (kv.Key, kv.Value);
        }

        public int TotalIncome()
        {
            int total = 0;
            foreach (var kv in IncomeMap) total += kv.Value;
            return total;
        }

        public int TotalExpense()
        {
            int total = 0;
            foreach (var kv in ExpenseMap) total += kv.Value;
            return total;
        }

        /// <summary>PassDay 직전 호출 — DayStartMoney 스냅샷 후 누적 초기화.</summary>
        public void Reset(int currentMoney)
        {
            _state.SettlementDayStartMoney = currentMoney;
            IncomeMap.Clear();
            ExpenseMap.Clear();
        }
    }
}
