using UnityEngine;
public interface IOrder
{
    int GenerateOrder(MenuSchema menu, string questId);
    bool IsOrderComplete(string questId);
    int ConsumeBento(string questId);
}
