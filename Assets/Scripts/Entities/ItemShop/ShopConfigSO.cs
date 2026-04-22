using System.Collections.Generic;
using UnityEngine;

public abstract class ShopConfigSO : ScriptableObject
{
    public string shopName;
    public abstract List<ItemShopSlotInfo> BuildSlotList();
}
