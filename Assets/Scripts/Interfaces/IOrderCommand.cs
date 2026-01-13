using System.Collections.Generic;
using UnityEngine;

public interface IOrderCommand
{
    void GenerateOrder(MenuSchema menu, string questId);
    bool TryMarkCooked(string questId);
    int ConsumeBento(string questId);
}
