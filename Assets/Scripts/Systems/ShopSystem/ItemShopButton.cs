using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemShopButton : MonoBehaviour
{
    [Header("상점 목록")]
    [SerializeField] public ItemShopSlotInfo[] sellItemList;

    public void OpenShop()
    {
        ItemShopManager.Instance.OpenItemShop(sellItemList);
    }
}
