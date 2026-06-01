using System.Collections.Generic;
using Game.Domain.Common;

public class FakeExpenseLog : IExpenseLog
{
    public readonly List<(string category, int amount)> Entries = new();

    public void Add(string category, int amount) => Entries.Add((category, amount));
}
