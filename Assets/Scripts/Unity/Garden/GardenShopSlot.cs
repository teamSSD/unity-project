using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 농장 업그레이드 탭은 UpgradeShopSlot으로 대체됨. 이 클래스는 미사용.
public class GardenShopSlot : MonoBehaviour
{
    [Header("Connect UI")]
    public Button buyButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI loreText;
    public TextMeshProUGUI costText;

    public void RefreshSlot() { }
}
