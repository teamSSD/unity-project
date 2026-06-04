using Game.Domain.Common;

/// <summary>
/// IExpenseLog → SettlementService(POCO) 어댑터.
/// </summary>
public class SettlementExpenseAdapter : IExpenseLog
{
    public void Add(string category, int amount)
    {
        GameSessionRoot.Instance?.Settlement?.AddExpense(category, amount);
    }
}
