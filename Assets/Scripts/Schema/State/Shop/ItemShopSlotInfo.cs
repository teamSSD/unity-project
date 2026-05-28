using System;
using UnityEngine;

public enum ProductType
{
    General, Special
}

[Serializable]
public class ItemShopSlotInfo
{
    public FoodData item;
    public int stock;
    public ProductType type;

    public ItemShopSlotInfo(FoodData item, int stock, ProductType type)
    {
        this.item = item;
        this.stock = stock;
        this.type = type;
    }
}
