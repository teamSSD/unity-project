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

    private FarmUpgradeType myUpgradeType;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    public void SetupSlot(FarmUpgradeType type)
    {
        myUpgradeType = type;
        RefreshSlot();
    }

    private void OnBuyButtonClicked()
    {
        GardenShopManager.Instance.BuyUpgrade(myUpgradeType);
    }

    public void RefreshSlot()
    {
        int currentLevel = FarmUpgradeManager.Instance.GetCurrentLevel(myUpgradeType);
        string nextValue = FarmUpgradeManager.Instance.GetNextUpgradeValueText(myUpgradeType);
        int nextCost = FarmUpgradeManager.Instance.GetNextUpgradeCost(currentLevel);

        if (nextCost == -1)
        {
            switch (myUpgradeType)
            {
                case FarmUpgradeType.TileCount:
                    titleText.text = $"텃밭 크기 업그레이드 <size=70%>(LV.MAX)</size>";
                    loreText.text = $"밭의 크기를 {nextValue}만큼 확장";
                    break;
                case FarmUpgradeType.TimeReduction:
                    titleText.text = $"재배 시간 업그레이드 <size=70%>(LV.MAX)</size>";
                    loreText.text = $"작물의 수확 시간 {nextValue}% 감소";
                    break;
                case FarmUpgradeType.HarvestCount:
                    titleText.text = $"수확 효율 업그레이드 <size=70%>(LV.MAX)</size>";
                    loreText.text = $"수확시 얻는 작물이 {nextValue}개로 증가";
                    break;
            }
            costText.text = "최대 레벨";
            buyButton.interactable = false;
        }
        else
        {
            switch (myUpgradeType)
            {
                case FarmUpgradeType.TileCount:
                    titleText.text = $"텃밭 크기 업그레이드 <size=70%>(LV.{currentLevel})</size>";
                    loreText.text = $"밭의 크기를 {nextValue}만큼 확장";
                    break;
                case FarmUpgradeType.TimeReduction:
                    titleText.text = $"재배 시간 업그레이드 <size=70%>(LV.{currentLevel})</size>";
                    loreText.text = $"작물의 수확 시간 {nextValue}% 감소";
                    break;
                case FarmUpgradeType.HarvestCount:
                    titleText.text = $"수확 효율 업그레이드 <size=70%>(LV.{currentLevel})</size>";
                    loreText.text = $"수확시 얻는 작물이 {nextValue}개로 증가";
                    break;
            }
            costText.text = $"{nextCost:N0} G";
            buyButton.interactable = true;
        }
    }
}
