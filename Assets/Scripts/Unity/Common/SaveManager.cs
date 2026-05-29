using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public PhaseData phase = new();
    public BasicStats stats = new();
    public InventorySaveData inventory = new();
    public OrderManager.OrderSaveData orders = new();
    public DeliveryQuestSaveData deliveryQuest = new();
    public ToolUpgradeSaveData toolUpgrades = new();
    public StorageUpgradeSaveData storageUpgrades = new();
    public FarmUpgradeSaveData farmUpgrades = new();
    public FarmTilesSaveData farmTiles = new();
}

/// <summary>
/// 농장 업그레이드 디스크 직렬화 형식. GameSaveData.farmUpgrades 슬롯에 그대로 보존
/// (디스크 호환성 유지를 위해 기존 shape 유지). GardenPersistent와 SaveManager에서 변환.
/// </summary>
[System.Serializable]
public class FarmUpgradeSaveData
{
    public List<string> types  = new();
    public List<int>    levels = new();
}

/// <summary>
/// 저장소 업그레이드 디스크 직렬화 형식. ShopPersistent와 SaveManager에서 변환.
/// </summary>
[System.Serializable]
public class StorageUpgradeSaveData
{
    public List<string> types  = new();
    public List<int>    levels = new();
}

/// <summary>
/// 조리 도구 업그레이드 디스크 직렬화 형식. ShopPersistent와 SaveManager에서 변환.
/// </summary>
[System.Serializable]
public class ToolUpgradeSaveData
{
    public List<string> ids    = new();
    public List<int>    levels = new();
}

/// <summary>
/// 게임 데이터 저장/로드 유틸리티.
/// 모든 JSON 디스크 I/O를 일원화 — 개별 매니저는 DataSaveUtil을 직접 호출하지 않음.
/// 단일 파일(gamedata.json)에 모든 데이터 통합 저장.
/// </summary>
public static class SaveManager
{
    private static string Dir => Application.persistentDataPath + "/saves";
    private static string SavePath => Dir + "/gamedata";

    /// <summary>
    /// 저장 데이터 존재 여부 (Continue 버튼 활성화용)
    /// </summary>
    public static bool HasSaveData()
    {
        MigrateLegacyIfNeeded();
        return DataSaveUtil.HasFile<GameSaveData>(SavePath);
    }

    /// <summary>
    /// 모든 게임 데이터를 디스크에 저장.
    /// PassDay()와 NewGame()에서만 호출.
    /// </summary>
    public static void SaveAll()
    {
        // Phase 1: piggyback 데이터를 PhaseData에 준비
        RecipeDataManager.Instance?.PrepareForSave();
        UnlockedFoodManager.Instance?.PrepareForSave();

        // Phase 2: GameSaveData 조립
        var save = new GameSaveData();

        if (ProgressSystem.Instance != null)
            save.phase = ProgressSystem.Instance.phaseData;

        save.stats = StatsSystem.Instance.GetSaveData();

        if (InventoryManager.Instance != null)
            save.inventory = InventoryManager.Instance.GetSaveData();

        if (OrderManager.Instance != null)
            save.orders = OrderManager.Instance.GetSaveData();

        save.deliveryQuest = DeliveryNpcDialogueInteraction.GetSaveData();

        if (GameSessionRoot.Instance != null)
        {
            var gp = GameSessionRoot.Instance.State.garden.persistent;
            save.farmUpgrades = new FarmUpgradeSaveData
            {
                types  = new List<string>(gp.upgradeTypes),
                levels = new List<int>(gp.upgradeLevels)
            };

            var sp = GameSessionRoot.Instance.State.shop.persistent;
            save.storageUpgrades = new StorageUpgradeSaveData
            {
                types  = new List<string>(sp.storageTypes),
                levels = new List<int>(sp.storageLevels)
            };
            save.toolUpgrades = new ToolUpgradeSaveData
            {
                ids    = new List<string>(sp.toolIds),
                levels = new List<int>(sp.toolLevels)
            };
        }

        if (GameSessionRoot.Instance != null)
        {
            var gpTiles = GameSessionRoot.Instance.State.garden.persistent.tiles;
            save.farmTiles = new FarmTilesSaveData { tiles = (FarmTileSaveData[])gpTiles.Clone() };
        }

        // Phase 3: 단일 파일로 저장
        DataSaveUtil.SaveData(save, SavePath);

        Debug.Log("[SaveManager] All data saved");
    }

    /// <summary>
    /// 모든 게임 데이터를 디스크에서 로드.
    /// ProcessContinue()에서만 호출.
    /// </summary>
    public static void LoadAll()
    {
        MigrateLegacyIfNeeded();

        var save = DataSaveUtil.LoadData(new GameSaveData(), SavePath);

        // Phase 1: 각 매니저에 분배
        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.ApplySaveData(save.phase);

        StatsSystem.Instance.ApplySaveData(save.stats);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ApplySaveData(save.inventory);

        if (OrderManager.Instance != null)
            OrderManager.Instance.ApplySaveData(save.orders);

        DeliveryNpcDialogueInteraction.ApplySaveData(save.deliveryQuest);

        if (GameSessionRoot.Instance != null && save.farmUpgrades != null)
        {
            // 서비스 초기화 시 채워진 타입 목록을 유지하며 saved level만 덮어쓰기.
            var gp = GameSessionRoot.Instance.State.garden.persistent;
            for (int i = 0; i < save.farmUpgrades.types.Count; i++)
            {
                string type = save.farmUpgrades.types[i];
                int level = save.farmUpgrades.levels[i];
                int idx = gp.upgradeTypes.IndexOf(type);
                if (idx >= 0) gp.upgradeLevels[idx] = level;
                else
                {
                    gp.upgradeTypes.Add(type);
                    gp.upgradeLevels.Add(level);
                }
            }
        }

        if (GameSessionRoot.Instance != null && save.storageUpgrades != null)
        {
            var sp = GameSessionRoot.Instance.State.shop.persistent;
            for (int i = 0; i < save.storageUpgrades.types.Count; i++)
            {
                string type = save.storageUpgrades.types[i];
                int level = save.storageUpgrades.levels[i];
                int idx = sp.storageTypes.IndexOf(type);
                if (idx >= 0) sp.storageLevels[idx] = level;
                else
                {
                    sp.storageTypes.Add(type);
                    sp.storageLevels.Add(level);
                }
            }
        }

        if (GameSessionRoot.Instance != null && save.toolUpgrades != null)
        {
            var sp = GameSessionRoot.Instance.State.shop.persistent;
            for (int i = 0; i < save.toolUpgrades.ids.Count; i++)
            {
                string id = save.toolUpgrades.ids[i];
                int level = save.toolUpgrades.levels[i];
                int idx = sp.toolIds.IndexOf(id);
                if (idx >= 0) sp.toolLevels[idx] = level;
                else
                {
                    sp.toolIds.Add(id);
                    sp.toolLevels.Add(level);
                }
            }
        }

        if (GameSessionRoot.Instance != null)
        {
            var gpTiles = GameSessionRoot.Instance.State.garden.persistent.tiles;
            for (int i = 0; i < gpTiles.Length; i++)
                gpTiles[i] = (save.farmTiles?.tiles != null && i < save.farmTiles.tiles.Length)
                    ? save.farmTiles.tiles[i] : null;
        }

        // Phase 2: PhaseData에서 piggyback 데이터 로드
        UnlockedFoodManager.Instance?.LoadUnlocksFromProgress();
        RecipeDataManager.Instance?.LoadMenusFromProgress();

        Debug.Log("[SaveManager] All data loaded");
    }

    /// <summary>
    /// 구 버전(5개 파일) → 신 버전(단일 파일) 자동 마이그레이션
    /// </summary>
    private static void MigrateLegacyIfNeeded()
    {
        if (DataSaveUtil.HasFile<GameSaveData>(SavePath)) return;

        string oldProgress = Dir + "/progress";
        if (!DataSaveUtil.HasFile<PhaseData>(oldProgress)) return;

        var save = new GameSaveData();
        save.phase = DataSaveUtil.LoadData(new PhaseData(), oldProgress);
        save.stats = DataSaveUtil.LoadData(new BasicStats(), Dir + "/stats");
        save.inventory = DataSaveUtil.LoadData(new InventorySaveData(), Dir + "/inventory");
        save.orders = DataSaveUtil.LoadData(new OrderManager.OrderSaveData(), Dir + "/orders");
        save.deliveryQuest = DataSaveUtil.LoadData(new DeliveryQuestSaveData(), Dir + "/deliveryQuest");

        DataSaveUtil.SaveData(save, SavePath);

        DeleteLegacyFile(Dir + "/progress");
        DeleteLegacyFile(Dir + "/stats");
        DeleteLegacyFile(Dir + "/inventory");
        DeleteLegacyFile(Dir + "/orders");
        DeleteLegacyFile(Dir + "/deliveryQuest");

        Debug.Log("[SaveManager] Legacy save migrated to single file");
    }

    private static void DeleteLegacyFile(string path)
    {
        string filePath = path + ".json";
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
