using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeShopSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private Button upgradeButton;

    private string toolId;         // Tool 슬롯이면 사용
    private bool isStorageSlot;

    public void InitTool(string id)
    {
        toolId = id;
        isStorageSlot = false;

        var toolData = SearchDataUtil.GetCookingToolDataById(id);
        if (toolData != null)
        {
            nameLabel.text = toolData.cookerName;
            if (icon != null) icon.sprite = toolData.defaultImage;
        }
        else
        {
            nameLabel.text = id;
        }
        Refresh();
    }

    public void InitStorage()
    {
        isStorageSlot = true;
        nameLabel.text = "창고";
        Refresh();
    }

    public void Refresh()
    {
        if (isStorageSlot)
        {
            var next = StorageUpgradeManager.Instance?.GetNextData();
            bool isMax = StorageUpgradeManager.Instance?.IsMax() ?? true;

            if (isMax)
            {
                levelLabel.text = "MAX";
                costLabel.text = "-";
                upgradeButton.interactable = false;
            }
            else
            {
                var cur = StorageUpgradeManager.Instance.GetCurrentData();
                levelLabel.text = $"Lv.{cur.level} → Lv.{next.level}";
                costLabel.text = $"{next.cost}G";
                upgradeButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
            }
        }
        else
        {
            var next = ToolUpgradeManager.Instance?.GetNextData(toolId);
            bool isMax = ToolUpgradeManager.Instance?.IsMax(toolId) ?? true;

            if (isMax)
            {
                levelLabel.text = "MAX";
                costLabel.text = "-";
                upgradeButton.interactable = false;
            }
            else
            {
                var cur = ToolUpgradeManager.Instance.GetCurrentData(toolId);
                levelLabel.text = $"Lv.{cur.level} → Lv.{next.level}";
                costLabel.text = $"{next.cost}G";
                upgradeButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
            }
        }
    }

    public void BTN_Upgrade()
    {
        bool success = isStorageSlot
            ? StorageUpgradeManager.Instance.TryUpgrade()
            : ToolUpgradeManager.Instance.TryUpgrade(toolId);

        if (success)
            UnifiedShopManager.Instance.RefreshAllUpgradeSlots();
    }
}
