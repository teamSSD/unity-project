using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemShopButton : MonoBehaviour
{
    private SearchProductUsecase searchProductUsecase;

    [Header("상점 목록")]
    [SerializeField] public List<ItemShopSlotInfo> sellItemList;
    private void Awake()
    {
        searchProductUsecase = new TempSearchProductUsecase();
    }
    public void OpenShop()
    {
        foreach (var p in RandomGeneral.Pick(searchProductUsecase.GetSpecial(), 4))
        {
            sellItemList.Add(new ItemShopSlotInfo(p.id, 10, ProductType.Special));
        }
        foreach (var p in searchProductUsecase.GetGeneral())
        {
            sellItemList.Add(new ItemShopSlotInfo(p.id, 999, ProductType.General));
        }
        ItemShopManager.Instance.OpenItemShop(sellItemList);
        sellItemList.Clear();
    }
}
