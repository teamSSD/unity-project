using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Game.Domain.Common;
using Game.Unity.Persistence;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public int schemaVersion = SaveRepository.CurrentSchemaVersion;
    public PhaseData phase = new();
    public BasicStats stats = new();
    public InventorySaveData inventory = new();
    public OrderSaveData orders = new();
    public DeliveryQuestSaveData deliveryQuest = new();
    public NpcNormalCycleSaveData npcNormalCycle = new();
    public ToolUpgradeSaveData toolUpgrades = new();
    public StorageUpgradeSaveData storageUpgrades = new();
    public FarmUpgradeSaveData farmUpgrades = new();
    public FarmTilesSaveData farmTiles = new();
    // Phase 2 Sprint 2: piggyback 매니저 분리 (H) — RecipeData/UnlockedFood를 자체 슬롯으로
    public RecipeBookSaveData recipeBook = new();
    public UnlockedRecipesSaveData unlockedRecipes = new();
    public TutorialSaveData tutorial = new();
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
    private static string SavePath => Dir + "/gamedata.json";
    private static SaveRepository Repository => new(SavePath);

#if UNITY_WEBGL && !UNITY_EDITOR
    // Assets/Plugins/WebGL/SaveSync.jslib — IDBFS in-memory 캐시를 IndexedDB에 flush.
    // 이 호출 없으면 브라우저 강제 종료 시 저장 유실 (탭 정상 종료는 자동 flush됨).
    [DllImport("__Internal")]
    private static extern void SyncFiles();
#endif

    /// <summary>
    /// 저장 데이터 존재 여부 (Continue 버튼 활성화용)
    /// </summary>
    public static bool HasSaveData()
    {
        MigrateLegacyIfNeeded();
        var result = Repository.Load();
        return result.Succeeded && result.Found;
    }

    /// <summary>
    /// 모든 게임 데이터를 디스크에 저장. PassDay/NewGame에서만 호출.
    /// 도메인별 Adapter가 GameSaveData 슬롯 채움.
    /// </summary>
    public static bool SaveAll()
    {
        var session = GameSessionRoot.Instance;
        if (session == null)
        {
            Debug.LogError("[SaveManager] Save failed: GameSessionRoot is missing.");
            return false;
        }

        var save = new GameSaveData();

        // 글로벌 facade 매니저
        if (session.Progress != null) save.phase = session.Progress.PhaseData;
        if (session.Stats != null) save.stats = session.Stats.GetSaveData();
        if (session.Inventory != null) save.inventory = session.Inventory.GetSaveData();

        // 도메인 Adapter
        GardenSaveAdapter.Capture(save);
        ShopSaveAdapter.Capture(save);
        MallSaveAdapter.Capture(save);

        // Self-contained 매니저 (piggyback 분리 후, H 해결)
        if (session.MenuSelection != null) save.recipeBook = session.MenuSelection.GetSaveData();
        if (session.UnlockedFood != null) save.unlockedRecipes = session.UnlockedFood.GetSaveData();
        if (session.Tutorial != null) save.tutorial = session.Tutorial.GetSaveData();

        var result = Repository.Save(save);
        if (!result.Succeeded)
        {
            Debug.LogError($"[SaveManager] Save failed: {result.Error}");
            return false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
        return true;
    }

    /// <summary>
    /// 모든 게임 데이터를 디스크에서 로드. ProcessContinue에서만 호출.
    /// 도메인별 Adapter가 GameSaveData 슬롯을 GameState 트리에 적용.
    /// </summary>
    public static bool LoadAll()
    {
        MigrateLegacyIfNeeded();
        var session = GameSessionRoot.Instance;
        if (session == null)
        {
            Debug.LogError("[SaveManager] Load failed: GameSessionRoot is missing.");
            return false;
        }

        var result = Repository.Load();
        if (!result.Succeeded || !result.Found)
        {
            Debug.LogError($"[SaveManager] Load failed: {result.Error ?? "save not found"}");
            return false;
        }

        if (result.RecoveredFromBackup)
            Debug.LogWarning("[SaveManager] Primary save was invalid; loaded backup.");

        var save = result.Data;

        // 글로벌 서비스 (GameSessionRoot 경유)
        session?.Progress?.ApplySaveData(save.phase);
        session?.Stats?.ApplySaveData(save.stats);
        session.Inventory?.ApplySaveData(save.inventory);

        // 도메인 Adapter
        GardenSaveAdapter.Apply(save);
        ShopSaveAdapter.Apply(save);
        MallSaveAdapter.Apply(save);

        // Self-contained 서비스: 신규 슬롯 우선, 비어있으면 legacy PhaseData fallback
        if (session?.UnlockedFood != null)
        {
            if (save.unlockedRecipes != null && save.unlockedRecipes.recipeIds.Count > 0)
                session.UnlockedFood.ApplySaveData(save.unlockedRecipes);
            else
                session.UnlockedFood.LoadUnlocksFromProgressLegacy(save.phase);
        }
        if (session?.MenuSelection != null)
        {
            if (save.recipeBook != null && save.recipeBook.selectedMenus.Count > 0)
                session.MenuSelection.ApplySaveData(save.recipeBook);
            else
                session.MenuSelection.LoadMenusFromProgressLegacy(save.phase);
        }
        session?.Tutorial?.ApplySaveData(save.tutorial);
        return true;
    }

    /// <summary>
    /// 구 버전(5개 파일) → 신 버전(단일 파일) 자동 마이그레이션
    /// </summary>
    private static void MigrateLegacyIfNeeded()
    {
        var current = Repository.Load();
        if (current.Succeeded && current.Found) return;

        string oldProgress = Dir + "/progress";
        if (!DataSaveUtil.HasFile<PhaseData>(oldProgress)) return;

        var save = new GameSaveData();
        save.phase = DataSaveUtil.LoadData(new PhaseData(), oldProgress);
        save.stats = DataSaveUtil.LoadData(new BasicStats(), Dir + "/stats");
        save.inventory = DataSaveUtil.LoadData(new InventorySaveData(), Dir + "/inventory");
        save.orders = DataSaveUtil.LoadData(new OrderSaveData(), Dir + "/orders");
        save.deliveryQuest = DataSaveUtil.LoadData(new DeliveryQuestSaveData(), Dir + "/deliveryQuest");

        var result = Repository.Save(save);
        if (!result.Succeeded)
        {
            Debug.LogError($"[SaveManager] Legacy migration failed: {result.Error}");
            return;
        }

        DeleteLegacyFile(Dir + "/progress");
        DeleteLegacyFile(Dir + "/stats");
        DeleteLegacyFile(Dir + "/inventory");
        DeleteLegacyFile(Dir + "/orders");
        DeleteLegacyFile(Dir + "/deliveryQuest");

        Debug.Log("[SaveManager] Legacy save migrated to single file");

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
    }

    private static void DeleteLegacyFile(string path)
    {
        string filePath = path + ".json";
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
