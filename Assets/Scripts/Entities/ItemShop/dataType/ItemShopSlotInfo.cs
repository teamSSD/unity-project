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

    [Header("등급")]
    [SerializeField] public ProductType ItemType;

    public ItemShopSlotInfo(ItemShopSlotInfo item)
    {
        this.Id = item.Id;
        this.ItemAmount = item.ItemAmount;
        this.ItemType = item.ItemType;
    }
    public ItemShopSlotInfo(string id, int amount, ProductType type)
    {
        this.Id = id;
        this.ItemAmount = amount;
        this.ItemType = type;
    }
}
