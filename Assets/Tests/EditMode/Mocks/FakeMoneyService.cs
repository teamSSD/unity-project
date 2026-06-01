using Game.Domain.Common;

/// <summary>
/// IMoneyService 테스트용 in-memory 구현.
/// </summary>
public class FakeMoneyService : IMoneyService
{
    public int Current { get; set; }

    public bool TrySpend(int amount)
    {
        if (Current < amount) return false;
        Current -= amount;
        return true;
    }

    public void Add(int amount)
    {
        if (amount > 0) Current += amount;
    }
}
