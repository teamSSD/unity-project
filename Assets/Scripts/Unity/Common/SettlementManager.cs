using System.Collections.Generic;
using UnityEngine;

public class SettlementManager : SingletonMonoBehaviour<SettlementManager>
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

    // PassDay 직전에 호출 — 스냅샷 갱신 후 초기화
    public void Reset()
    {
        DayStartMoney = StatsSystem.Instance != null ? StatsSystem.Instance.GetMoney() : 0;
        incomeMap.Clear();
        expenseMap.Clear();
    }
}
