using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemShopSlotInfo
{
    [Header("재료 ID")]
    [SerializeField] public string Id;

    [Header("재고")]
    [SerializeField] public int ItemAmount;

   public ItemShopSlotInfo(ItemShopSlotInfo item)
    {
        this.Id = item.Id;
        this.ItemAmount = item.ItemAmount;
    }
    public ItemShopSlotInfo(string id, int amount)
    {
        this.Id = id;
        this.ItemAmount = amount;
    }
}
