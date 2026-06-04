using System.Collections.Generic;

namespace Game.Domain.Mall
{
    /// <summary>
    /// 일별 정산 (수입/지출) 누적 + 스냅샷 (POCO). SettlementManager facade 후속.
    /// PassDay 직전에 Reset()으로 갱신 후 초기화.
    /// </summary>
    public class SettlementService
    {
        public const int ManagementFee = 1000;

        private readonly Dictionary<string, int> incomeMap  = new();
        private readonly Dictionary<string, int> expenseMap = new();
        public int DayStartMoney { get; private set; }

        public void AddIncome(string label, int amount)
        {
            if (amount <= 0) return;
            incomeMap[label] = incomeMap.GetValueOrDefault(label) + amount;
        }

        public void AddExpense(string label, int amount)
        {
            if (amount <= 0) return;
            expenseMap[label] = expenseMap.GetValueOrDefault(label) + amount;
        }

        public IEnumerable<(string label, int amount)> GetIncomeEntries()
        {
            foreach (var kv in incomeMap) yield return (kv.Key, kv.Value);
        }

        public IEnumerable<(string label, int amount)> GetExpenseEntries()
        {
            foreach (var kv in expenseMap) yield return (kv.Key, kv.Value);
        }

        public int TotalIncome()
        {
            int total = 0;
            foreach (var kv in incomeMap) total += kv.Value;
            return total;
        }

        public int TotalExpense()
        {
            int total = 0;
            foreach (var kv in expenseMap) total += kv.Value;
            return total;
        }

        /// <summary>PassDay 직전 호출 — DayStartMoney 스냅샷 후 누적 초기화.</summary>
        public void Reset(int currentMoney)
        {
            DayStartMoney = currentMoney;
            incomeMap.Clear();
            expenseMap.Clear();
        }
    }
}
