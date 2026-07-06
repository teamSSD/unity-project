using UnityEngine;

/// <summary>
/// ShopUIAdapter 의 Tool/Storage/Farm 업그레이드 탭 처리 (partial).
/// PopulateXxxList + ShowXxxDetail + 라벨 매핑.
/// </summary>
public partial class ShopUIAdapter
{
    private void PopulateToolList()
    {
        var mgr = toolUpgrade;
        if (mgr == null) return;
        foreach (var id in mgr.GetAllToolIds())
        {
            var tool = SearchDataUtil.GetCookingToolDataById(id);
            var cur  = mgr.GetCurrentData(id);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(id) ? "MAX" : $"{mgr.GetNextData(id).cost}G";
            AddRow(
                tool?.defaultImage,
                tool != null ? tool.cookerName : id,
                leftSub, rightSub,
                new ToolRowData { Id = id }
            );
        }
    }

    private void ShowToolDetail(string id)
    {
        var mgr = toolUpgrade;
        if (mgr == null) return;
        var tool = SearchDataUtil.GetCookingToolDataById(id);
        var cur  = mgr.GetCurrentData(id);
        string title = tool != null ? $"{tool.cookerName} 업그레이드" : id;
        bool isMax = mgr.IsMax(id);

        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc = cur != null
                ? $"미니게임 시간 {cur.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}"
                : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(id);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc = $"미니게임 시간 {cur.durationMultiplier * 100:0}%→{next.durationMultiplier * 100:0}%  /  스태미나 {cur.staminaCost}→{next.staminaCost}";
            canUpgrade = HasMoney(next.cost);
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Tool, id, tool?.defaultImage, title, desc, levelText, costText, canUpgrade);
    }

    private void PopulateStorageList()
    {
        var mgr = storageUpgrade;
        if (mgr == null) return;
        foreach (var type in mgr.GetAllTypes())
        {
            var cur  = mgr.GetCurrentData(type);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(type) ? "MAX" : $"{mgr.GetNextData(type).cost}G";
            AddRow(null, StorageTypeName(type), leftSub, rightSub, new StorageRowData { Type = type });
        }
    }

    private void ShowStorageDetail(string type)
    {
        var mgr = storageUpgrade;
        if (mgr == null) return;
        var cur = mgr.GetCurrentData(type);
        bool isMax = mgr.IsMax(type);
        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc      = cur != null ? $"{StorageTypeName(type)} {cur.value}칸" : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(type);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc      = $"{StorageTypeName(type)} {cur.value}칸→{next.value}칸";
            canUpgrade = HasMoney(next.cost);
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Storage, type, null, StorageTypeName(type), desc, levelText, costText, canUpgrade);
    }

    private void PopulateFarmList()
    {
        var mgr = farmUpgrade;
        if (mgr == null) return;
        foreach (var type in mgr.GetAllTypes())
        {
            var cur = mgr.GetCurrentData(type);
            string leftSub  = cur != null ? $"Lv.{cur.level}" : "-";
            string rightSub = mgr.IsMax(type) ? "MAX" : $"{mgr.GetNextData(type).cost}G";
            AddRow(null, FarmTypeName(type), leftSub, rightSub, new FarmRowData { Type = type });
        }
    }

    private void ShowFarmDetail(string type)
    {
        var mgr = farmUpgrade;
        if (mgr == null) return;
        var cur = mgr.GetCurrentData(type);
        bool isMax = mgr.IsMax(type);
        string levelText, costText, desc;
        bool canUpgrade;
        if (isMax)
        {
            levelText = "MAX";
            costText  = "-";
            desc      = cur != null ? FarmValueLabel(type, cur.value, cur.value) : "";
            canUpgrade = false;
        }
        else
        {
            var next = mgr.GetNextData(type);
            levelText = $"Lv.{cur.level} → Lv.{next.level}";
            costText  = $"{next.cost}G";
            desc      = FarmValueLabel(type, cur.value, next.value);
            canUpgrade = HasMoney(next.cost);
        }
        detailPanel?.ShowUpgrade(ShopDetailPanel.UpgradeKind.Farm, type, null, FarmTypeName(type), desc, levelText, costText, canUpgrade);
    }

    private bool HasMoney(int cost) => (stats?.GetMoney() ?? 0) >= cost;

    private static string StorageTypeName(string type) => type switch
    {
        "refrigerator" => "냉장고 확장",
        "upperShelf"   => "윗 찬장 확장",
        "lowerShelf"   => "아랫 찬장 확장",
        _              => type,
    };

    private static string FarmTypeName(string type) => type switch
    {
        "tile"          => "농장 확장",
        "timeReduction" => "수확 시간 감소",
        "harvestCount"  => "수확량 증가",
        _               => type,
    };

    private static string FarmValueLabel(string type, float cur, float next)
    {
        bool isMax = Mathf.Approximately(cur, next);
        return type switch
        {
            "tile"          => isMax ? $"타일 {(int)cur}개" : $"타일 {(int)cur}개→{(int)next}개",
            "timeReduction" => isMax ? $"수확 시간 -{cur * 100:0}%" : $"수확 시간 -{cur * 100:0}%→-{next * 100:0}%",
            "harvestCount"  => isMax ? $"수확량 {(int)cur}개" : $"수확량 {(int)cur}개→{(int)next}개",
            _               => $"{cur}→{next}",
        };
    }
}
