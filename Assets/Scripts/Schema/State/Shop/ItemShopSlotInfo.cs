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
    /// <summary>-1 = 무제한(General). 0 이상 = Special 페이즈별 잔여 수량.</summary>
    public int stock;
    public ProductType type;

    public bool IsUnlimited => stock < 0;

    public ItemShopSlotInfo(FoodData item, int stock, ProductType type)
    {
        this.item = item;
        this.stock = stock;
        this.type = type;
    }
}
