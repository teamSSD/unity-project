using System.Collections.Generic;

namespace Game.Schema.State.Shop
{
    public sealed class ShopSessionState
    {
        public Dictionary<FoodData, int> PurchasedByFood { get; } = new();
        public long CachedKey { get; set; } = long.MinValue;
        public List<ItemShopSlotInfo> CachedItemList { get; set; }
        public int RefreshCount { get; set; }
    }
}
