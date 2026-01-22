using System.Collections.Generic;
using UnityEngine;

public interface IOrderCommand
{
    void GenerateOrder(MenuSchema menu, string questId, Vector2 location, string characterSpriteName);
    bool TryMarkCooked(string questId);
    int ConsumeBento(string questId);
}
