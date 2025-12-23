using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemShopSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI NameLabel, LoreLabel, CostLabel;
    [SerializeField] private Button BuyButton;

    private ItemShopSlotInfo sellInfo;

    public void RefreshSlot()
    {
        CostLabel.text = $"{sellInfo.Cost}";
    }

    public void InitSlot(ItemShopSlotInfo sellItem)
    {
        // 정보 가져오기
        sellInfo = sellItem;
    }
    public void BTN_BuyItem()
    {
        Debug.Log("버튼을 눌렀습니다.");
        ItemShopManager.Instance.AddPrice(sellInfo.Cost);
        ItemShopManager.Instance.UpdateTotalPrice();
    }
}
