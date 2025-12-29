using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemShop : MonoBehaviour
{
    [Header("판매할 아이템")]
    [SerializeField] public ItemShopSlotInfo[] SellItemInfos;
    private void Awake()
    {
        List<ItemShopSlotInfo> itemShopSlotInfos = new List<ItemShopSlotInfo>();
        foreach (ItemShopSlotInfo shopShopSlotInfo in SellItemInfos)
            itemShopSlotInfos.Add(shopShopSlotInfo);
        SellItemInfos = itemShopSlotInfos.ToArray();
    }
}
