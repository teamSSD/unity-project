using Game.Domain.Common;
using Game.Domain.Mall;

namespace Game.Editor.Simulation
{
    /// <summary>StatsService에 직접 바인딩하는 IMoneyService — 프로덕션 StatsMoneyAdapter의 headless판.
    /// GameSessionRoot.Instance 싱글턴 대신 생성자 주입.</summary>
    public class DirectMoneyAdapter : IMoneyService
    {
        private readonly StatsService _stats;
        public DirectMoneyAdapter(StatsService stats) { _stats = stats; }

        public int Current => _stats.GetMoney();

        public bool TrySpend(int amount)
        {
            if (_stats.GetMoney() < amount) return false;
            _stats.SubMoney(amount);
            return true;
        }

        public void Add(int amount) { if (amount > 0) _stats.AddMoney(amount); }
    }

    /// <summary>SettlementService에 직접 바인딩하는 IExpenseLog.</summary>
    public class DirectExpenseAdapter : IExpenseLog
    {
        private readonly SettlementService _settlement;
        public DirectExpenseAdapter(SettlementService settlement) { _settlement = settlement; }

        public void Add(string category, int amount) => _settlement?.AddExpense(category, amount);
    }
}
