using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public PhaseData phase = new();
    public BasicStats stats = new();
    public InventorySaveData inventory = new();
    public OrderSaveData orders = new();
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
/// NPC별 배달 퀘스트 진행도 디스크 직렬화 형식. MallPersistent와 SaveManager에서 변환.
/// </summary>
[System.Serializable]
public class DeliveryQuestSaveData
{
    public List<string> groupIds = new();
    public List<int>    stages   = new();
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
    /// 모든 게임 데이터를 디스크에 저장. PassDay/NewGame에서만 호출.
    /// 도메인별 Adapter가 GameSaveData 슬롯 채움.
    /// </summary>
    public static void SaveAll()
    {
        // piggyback 데이터를 PhaseData에 준비
        RecipeDataManager.Instance?.PrepareForSave();
        UnlockedFoodManager.Instance?.PrepareForSave();

        var save = new GameSaveData();

        // 글로벌 facade 매니저
        if (ProgressSystem.Instance != null) save.phase = ProgressSystem.Instance.phaseData;
        save.stats = StatsSystem.Instance.GetSaveData();
        if (InventoryManager.Instance != null) save.inventory = InventoryManager.Instance.GetSaveData();

        // 도메인 Adapter
        GardenSaveAdapter.Capture(save);
        ShopSaveAdapter.Capture(save);
        MallSaveAdapter.Capture(save);

        DataSaveUtil.SaveData(save, SavePath);
    }

    /// <summary>
    /// 모든 게임 데이터를 디스크에서 로드. ProcessContinue에서만 호출.
    /// 도메인별 Adapter가 GameSaveData 슬롯을 GameState 트리에 적용.
    /// </summary>
    public static void LoadAll()
    {
        MigrateLegacyIfNeeded();
        var save = DataSaveUtil.LoadData(new GameSaveData(), SavePath);

        // 글로벌 facade 매니저
        if (ProgressSystem.Instance != null) ProgressSystem.Instance.ApplySaveData(save.phase);
        StatsSystem.Instance.ApplySaveData(save.stats);
        if (InventoryManager.Instance != null) InventoryManager.Instance.ApplySaveData(save.inventory);

        // 도메인 Adapter
        GardenSaveAdapter.Apply(save);
        ShopSaveAdapter.Apply(save);
        MallSaveAdapter.Apply(save);

        // piggyback 데이터 (PhaseData에 얹혀 있음 — H 미해결)
        UnlockedFoodManager.Instance?.LoadUnlocksFromProgress();
        RecipeDataManager.Instance?.LoadMenusFromProgress();
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
        save.orders = DataSaveUtil.LoadData(new OrderSaveData(), Dir + "/orders");
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
