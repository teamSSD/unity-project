using Game.Domain.Common;

/// <summary>
/// IExpenseLog → SettlementManager 어댑터.
/// </summary>
public class SettlementExpenseAdapter : IExpenseLog
{
    public void Add(string category, int amount)
    {
        SettlementManager.Instance?.AddExpense(category, amount);
    }
}
