using Game.Domain.Common;
using UnityEngine;

/// <summary>
/// IMoneyService → StatsSystem 어댑터. GameSessionRoot에서 인스턴스화하여 Service에 주입.
/// </summary>
public class StatsMoneyAdapter : IMoneyService
{
    public int Current => StatsSystem.Instance != null ? StatsSystem.Instance.GetMoney() : 0;

    public bool TrySpend(int amount)
    {
        if (StatsSystem.Instance == null) return false;
        if (Current < amount)
        {
            Debug.Log($"[StatsMoneyAdapter] 골드 부족 ({amount}G 필요)");
            return false;
        }
        StatsSystem.Instance.SubMoney(amount);
        return true;
    }
}
