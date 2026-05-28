using System.Collections.Generic;
using UnityEngine;

public interface IOrderCommand
{
    void GenerateOrder(MenuSchema menu, string questId, string npcId);
    bool MarkCookedWithPrice(string questId, int price);
    int ConsumeBento(string questId);
}
