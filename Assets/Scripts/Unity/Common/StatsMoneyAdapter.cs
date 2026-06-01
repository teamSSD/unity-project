using Game.Domain.Common;
using UnityEngine;

/// <summary>
/// IMoneyService → StatsService(POCO) 어댑터. GameSessionRoot에서 인스턴스화하여 Service에 주입.
/// </summary>
public class StatsMoneyAdapter : IMoneyService
{
    private static StatsService Svc => GameSessionRoot.Instance?.Stats;

    public int Current => Svc?.GetMoney() ?? 0;

    public bool TrySpend(int amount)
    {
        var s = Svc;
        if (s == null) return false;
        if (s.GetMoney() < amount)
        {
            Debug.Log($"[StatsMoneyAdapter] 골드 부족 ({amount}G 필요)");
            return false;
        }
        s.SubMoney(amount);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Svc?.AddMoney(amount);
    }
}
