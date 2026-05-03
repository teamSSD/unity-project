using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeShopSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private Button upgradeButton;

    private enum SlotType { Tool, Storage, Farm }
    private SlotType slotType;
    private string itemId;

    private void Awake()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(BTN_Upgrade);
            foreach (var btn in upgradeButton.GetComponentsInChildren<Button>(true))
                if (btn != upgradeButton)
                    btn.onClick.AddListener(BTN_Upgrade);
        }
    }

    public void InitTool(string id)
    {
        slotType = SlotType.Tool;
        itemId   = id;

        var toolData = SearchDataUtil.GetCookingToolDataById(id);
        if (toolData != null)
        {
            nameLabel.text = $"{toolData.cookerName} 업그레이드";
            if (icon != null) icon.sprite = toolData.defaultImage;
        }
        else
        {
            nameLabel.text = id;
        }
        Refresh();
    }

    public void InitStorage(string type)
    {
        slotType       = SlotType.Storage;
        itemId         = type;
        nameLabel.text = StorageTypeName(type);
        Refresh();
    }

    public void InitFarm(string type)
    {
        slotType       = SlotType.Farm;
        itemId         = type;
        nameLabel.text = FarmTypeName(type);
        Refresh();
    }

    public void Refresh()
    {
        switch (slotType)
        {
            case SlotType.Tool:    RefreshTool();    break;
            case SlotType.Storage: RefreshStorage(); break;
            case SlotType.Farm:    RefreshFarm();    break;
        }
    }

    private void RefreshTool()
    {
        bool isMax = ToolUpgradeManager.Instance?.IsMax(itemId) ?? true;

        if (isMax)
        {
            var cur = ToolUpgradeManager.Instance?.GetCurrentData(itemId);
            levelLabel.text = "MAX";
            costLabel.text  = "-";
            upgradeButton.interactable = false;
            if (descriptionLabel != null && cur != null)
                descriptionLabel.text = $"미니게임 시간 {cur.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}";
        }
        else
        {
            var cur  = ToolUpgradeManager.Instance.GetCurrentData(itemId);
            var next = ToolUpgradeManager.Instance.GetNextData(itemId);
            levelLabel.text = $"Lv.{cur.level} → Lv.{next.level}";
            costLabel.text  = $"{next.cost}G";
            upgradeButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
            if (descriptionLabel != null)
                descriptionLabel.text = $"미니게임 시간 {cur.durationMultiplier * 100:0}%→{next.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}→{next.staminaCost}";
        }
    }

    private void RefreshStorage()
    {
        bool isMax = StorageUpgradeManager.Instance?.IsMax(itemId) ?? true;

        if (isMax)
        {
            var cur = StorageUpgradeManager.Instance?.GetCurrentData(itemId);
            levelLabel.text = "MAX";
            costLabel.text  = "-";
            upgradeButton.interactable = false;
            if (descriptionLabel != null && cur != null)
                descriptionLabel.text = $"{StorageTypeName(itemId)} {cur.value}칸";
        }
        else
        {
            var cur  = StorageUpgradeManager.Instance.GetCurrentData(itemId);
            var next = StorageUpgradeManager.Instance.GetNextData(itemId);
            levelLabel.text = $"Lv.{cur.level} → Lv.{next.level}";
            costLabel.text  = $"{next.cost}G";
            upgradeButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
            if (descriptionLabel != null)
                descriptionLabel.text = $"{StorageTypeName(itemId)} {cur.value}칸→{next.value}칸";
        }
    }

    private void RefreshFarm()
    {
        bool isMax = FarmUpgradeManager.Instance?.IsMax(itemId) ?? true;

        if (isMax)
        {
            var cur = FarmUpgradeManager.Instance?.GetCurrentData(itemId);
            levelLabel.text = "MAX";
            costLabel.text  = "-";
            upgradeButton.interactable = false;
            if (descriptionLabel != null && cur != null)
                descriptionLabel.text = FarmValueLabel(itemId, cur.value, cur.value);
        }
        else
        {
            var cur  = FarmUpgradeManager.Instance.GetCurrentData(itemId);
            var next = FarmUpgradeManager.Instance.GetNextData(itemId);
            levelLabel.text = $"Lv.{cur.level} → Lv.{next.level}";
            costLabel.text  = $"{next.cost}G";
            upgradeButton.interactable = StatsSystem.Instance.GetMoney() >= next.cost;
            if (descriptionLabel != null)
                descriptionLabel.text = FarmValueLabel(itemId, cur.value, next.value);
        }
    }

    public void BTN_Upgrade()
    {
        bool success = slotType switch
        {
            SlotType.Tool    => ToolUpgradeManager.Instance.TryUpgrade(itemId),
            SlotType.Storage => StorageUpgradeManager.Instance.TryUpgrade(itemId),
            SlotType.Farm    => FarmUpgradeManager.Instance.TryUpgrade(itemId),
            _                => false
        };

        if (success)
            UnifiedShopManager.Instance.RefreshAllUpgradeSlots();
    }

    private static string StorageTypeName(string type) => type switch
    {
        "refrigerator" => "냉장고 확장",
        "upperShelf"   => "윗 찬장 확장",
        "lowerShelf"   => "아랫 찬장 확장",
        _              => type
    };

    private static string FarmTypeName(string type) => type switch
    {
        "tile"          => "농장 확장",
        "timeReduction" => "수확 시간 감소",
        "harvestCount"  => "수확량 증가",
        _               => type
    };

    private static string FarmValueLabel(string type, float cur, float next)
    {
        bool isMax = Mathf.Approximately(cur, next);
        return type switch
        {
            "tile"          => isMax ? $"타일 {(int)cur}개" : $"타일 {(int)cur}개→{(int)next}개",
            "timeReduction" => isMax ? $"수확 시간 -{cur * 100:0}%" : $"수확 시간 -{cur * 100:0}%→-{next * 100:0}%",
            "harvestCount"  => isMax ? $"수확량 {(int)cur}개" : $"수확량 {(int)cur}개→{(int)next}개",
            _               => $"{cur}→{next}"
        };
    }
}
