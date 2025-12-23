using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemShopSlotInfo
{
    [Header("가격")]
    [SerializeField] public int Cost;

    [Header("재고")]
    [SerializeField] public int ItemAmount;

   public ItemShopSlotInfo(ItemShopSlotInfo item)
    {
        this.Cost = item.Cost;
        this.ItemAmount = item.ItemAmount;
    }
}
