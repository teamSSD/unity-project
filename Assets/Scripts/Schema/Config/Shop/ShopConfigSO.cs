using System.Collections.Generic;
using UnityEngine;

public abstract class ShopConfigSO : ScriptableObject
{
    public string shopName;
    /// <summary>페이즈 단위 재현성 위해 호출자가 RNG 주입. GameRandom.Immutable static 시퀀스 미참조.</summary>
    public abstract List<ItemShopSlotInfo> BuildSlotList(System.Random rng);
}
