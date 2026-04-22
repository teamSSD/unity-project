using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GardenShopSlot : MonoBehaviour
{
    [Header("Connect UI")]
    public Button buyButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI loreText;
    public TextMeshProUGUI costText;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void OnBuyButtonClicked()
    {
        UnifiedShopManager.Instance.BuyFarmUpgrade();
    }

    public void RefreshSlot()
    {
        bool isMax = FarmUpgradeManager.Instance.IsMax();
        var cur  = FarmUpgradeManager.Instance.GetCurrentData();
        var next = FarmUpgradeManager.Instance.GetNextData();

        if (isMax)
        {
            titleText.text = "농장 업그레이드 <size=70%>(MAX)</size>";
            loreText.text  = "모든 항목 최대 레벨";
            costText.text  = "최대 레벨";
            buyButton.interactable = false;
        }
        else
        {
            titleText.text = $"농장 업그레이드 <size=70%>(Lv.{cur.level} → Lv.{next.level})</size>";
            loreText.text  = $"타일 {cur.tileCount}→{next.tileCount}  /  "
                           + $"수확 시간 -{next.timeReduction * 100:0}%  /  "
                           + $"수확량 {cur.harvestCount}→{next.harvestCount}";
            costText.text  = $"{next.cost:N0} G";
            buyButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
        }
    }
}
