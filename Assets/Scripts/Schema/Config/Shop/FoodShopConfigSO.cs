using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/FoodShopConfig")]
public class FoodShopConfigSO : ShopConfigSO
{
    [Serializable]
    public class Entry
    {
        public FoodData item;
        public int stock;
        public ProductType type;
    }

    public List<Entry> generalItems;
    public List<Entry> specialPool;
    public int specialPickCount = 4;

    public override List<ItemShopSlotInfo> BuildSlotList()
    {
        var slots = new List<ItemShopSlotInfo>();
        foreach (var e in GameRandom.Pick(GameRandom.Immutable, specialPool, specialPickCount))
            slots.Add(new ItemShopSlotInfo(e.item, e.stock, ProductType.Special));
        foreach (var e in generalItems)
            slots.Add(new ItemShopSlotInfo(e.item, e.stock, ProductType.General));
        return slots;
    }
}
