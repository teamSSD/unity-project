using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/FoodShopConfig")]
public class FoodShopConfigSO : ShopConfigSO
{
    /// <summary>Special 재고 슬롯 정의. General은 무한 매입이므로 FoodData 직접 나열.</summary>
    [Serializable]
    public class SpecialEntry
    {
        public FoodData item;
        public int stock;
    }

    public List<FoodData> generalFoods;
    public List<SpecialEntry> specialSlots;
    public int specialPickCount = 4;

    public override List<ItemShopSlotInfo> BuildSlotList(System.Random rng)
    {
        var slots = new List<ItemShopSlotInfo>();
        if (specialSlots != null && specialSlots.Count > 0)
        {
            foreach (var e in GameRandom.Pick(rng, specialSlots, specialPickCount))
                if (e?.item != null) slots.Add(new ItemShopSlotInfo(e.item, e.stock, ProductType.Special));
        }
        if (generalFoods != null)
        {
            foreach (var item in generalFoods)
                if (item != null) slots.Add(new ItemShopSlotInfo(item, -1, ProductType.General));
        }
        return slots;
    }
}
