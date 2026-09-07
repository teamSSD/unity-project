using System;
using System.IO;
using System.Text.RegularExpressions;
using Game.Domain.Common;
using UnityEngine;

namespace Game.Unity.Persistence
{
    public readonly struct SaveWriteResult
    {
        public bool Succeeded { get; }
        public string Error { get; }

        private SaveWriteResult(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public static SaveWriteResult Success() => new(true, null);
        public static SaveWriteResult Failure(string error) => new(false, error);
    }

    public readonly struct SaveReadResult
    {
        public bool Succeeded { get; }
        public bool Found { get; }
        public bool RecoveredFromBackup { get; }
        public GameSaveData Data { get; }
        public string Error { get; }

        private SaveReadResult(bool succeeded, bool found, bool recoveredFromBackup, GameSaveData data, string error)
        {
            Succeeded = succeeded;
            Found = found;
            RecoveredFromBackup = recoveredFromBackup;
            Data = data;
            Error = error;
        }

        public static SaveReadResult Success(GameSaveData data, bool recoveredFromBackup = false) =>
            new(true, true, recoveredFromBackup, data, null);

        public static SaveReadResult NotFound() => new(false, false, false, null, null);
        public static SaveReadResult Failure(string error) => new(false, true, false, null, error);
    }

    /// <summary>
    /// GameSaveData 파일 I/O의 단일 경계. 임시 파일 검증 후 교체하고,
    /// 정상인 기존 파일은 backup으로 보존한다.
    /// </summary>
    public sealed class SaveRepository
    {
        public const int CurrentSchemaVersion = 1;

        private readonly string _path;
        private string TempPath => _path + ".tmp";
        private string BackupPath => _path + ".bak";

        public SaveRepository(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Save path is required.", nameof(path));

            _path = path;
        }

        public SaveWriteResult Save(GameSaveData data)
        {
            if (data == null) return SaveWriteResult.Failure("Save data is null.");

            try
            {
                data.schemaVersion = CurrentSchemaVersion;
                string json = JsonUtility.ToJson(data);
                if (!TryDeserialize(json, out _, out string serializationError))
                    return SaveWriteResult.Failure($"Serialized save validation failed: {serializationError}");

                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(TempPath, json);
                if (!TryRead(TempPath, out _, out string tempError))
                    throw new InvalidDataException($"Temporary save validation failed: {tempError}");

                ReplaceWithValidatedTemp();
                return SaveWriteResult.Success();
            }
            catch (Exception exception)
            {
                return SaveWriteResult.Failure(exception.Message);
            }
            finally
            {
                TryDelete(TempPath);
            }
        }

        public SaveReadResult Load()
        {
            bool primaryExists = File.Exists(_path);
            bool backupExists = File.Exists(BackupPath);
            if (!primaryExists && !backupExists) return SaveReadResult.NotFound();

            string primaryError = null;
            string backupError = null;
            if (primaryExists && TryRead(_path, out GameSaveData primary, out primaryError))
                return SaveReadResult.Success(primary);

            if (backupExists && TryRead(BackupPath, out GameSaveData backup, out backupError))
                return SaveReadResult.Success(backup, recoveredFromBackup: true);

            string error = primaryExists ? primaryError : "Primary save is missing.";
            if (backupExists) error += $" Backup failed: {backupError}";
            return SaveReadResult.Failure(error);
        }

        private void ReplaceWithValidatedTemp()
        {
            if (!File.Exists(_path))
            {
                File.Move(TempPath, _path);
                return;
            }

            bool primaryIsValid = TryRead(_path, out _, out _);
            if (!primaryIsValid)
            {
                File.Delete(_path);
                File.Move(TempPath, _path);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            ReplaceWithRecoverableFallback();
#else
            try
            {
                File.Replace(TempPath, _path, BackupPath, ignoreMetadataErrors: true);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceWithRecoverableFallback();
            }
            catch (NotSupportedException)
            {
                ReplaceWithRecoverableFallback();
            }
#endif
        }

        private void ReplaceWithRecoverableFallback()
        {
            File.Copy(_path, BackupPath, overwrite: true);
            try
            {
                File.Delete(_path);
                File.Move(TempPath, _path);
            }
            catch
            {
                if (!File.Exists(_path) && File.Exists(BackupPath))
                    File.Copy(BackupPath, _path);
                throw;
            }
        }

        private static bool TryRead(string path, out GameSaveData data, out string error)
        {
            try
            {
                return TryDeserialize(File.ReadAllText(path), out data, out error);
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
        }

        private static bool TryDeserialize(string json, out GameSaveData data, out string error)
        {
            try
            {
                if (!HasJsonField(json, "phase") || !HasJsonField(json, "stats"))
                {
                    data = null;
                    error = "Save is missing required phase or stats data.";
                    return false;
                }

                // 필수 필드를 null sentinel로 둬서 문법만 유효한 불완전 JSON을 구분한다.
                data = new GameSaveData
                {
                    schemaVersion = 0,
                    phase = null,
                    stats = null
                };
                JsonUtility.FromJsonOverwrite(json, data);
                if (data.phase == null || data.stats == null)
                {
                    error = "Save is missing required phase or stats data.";
                    data = null;
                    return false;
                }

                if (data.schemaVersion > CurrentSchemaVersion)
                {
                    error = $"Unsupported save schema {data.schemaVersion}.";
                    data = null;
                    return false;
                }

                // 기존 통합 세이브에는 version 필드가 없으므로 version 0으로 취급한다.
                if (data.schemaVersion <= 0) data.schemaVersion = CurrentSchemaVersion;

                NormalizeOptionalData(data);
                if (!ValidateParallelLists(data, out error))
                {
                    data = null;
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
        }

        private static bool HasJsonField(string json, string fieldName) =>
            !string.IsNullOrEmpty(json) &&
            Regex.IsMatch(json, $"\\\"{Regex.Escape(fieldName)}\\\"\\s*:");

        private static void NormalizeOptionalData(GameSaveData data)
        {
            data.phase.UnlockedRecipes ??= new();
            data.phase.SelectedMenus ??= new();

            data.inventory ??= new InventorySaveData();
            data.inventory.foodIds ??= new();
            data.inventory.amounts ??= new();
            data.inventory.items ??= new();
            foreach (var item in data.inventory.items)
                if (item != null) item.batches ??= new();

            data.orders ??= new OrderSaveData();
            data.orders.entries ??= new();
            foreach (var entry in data.orders.entries)
                if (entry != null) entry.sideMenuIds ??= new();

            data.deliveryQuest ??= new DeliveryQuestSaveData();
            data.deliveryQuest.groupIds ??= new();
            data.deliveryQuest.stages ??= new();

            data.npcNormalCycle ??= new NpcNormalCycleSaveData();
            data.npcNormalCycle.npcIds ??= new();
            data.npcNormalCycle.indices ??= new();

            data.toolUpgrades ??= new ToolUpgradeSaveData();
            data.toolUpgrades.ids ??= new();
            data.toolUpgrades.levels ??= new();

            data.storageUpgrades ??= new StorageUpgradeSaveData();
            data.storageUpgrades.types ??= new();
            data.storageUpgrades.levels ??= new();

            data.farmUpgrades ??= new FarmUpgradeSaveData();
            data.farmUpgrades.types ??= new();
            data.farmUpgrades.levels ??= new();
            data.farmTiles ??= new FarmTilesSaveData();

            data.recipeBook ??= new RecipeBookSaveData();
            data.recipeBook.selectedMenus ??= new();
            data.unlockedRecipes ??= new UnlockedRecipesSaveData();
            data.unlockedRecipes.recipeIds ??= new();
            data.tutorial ??= new TutorialSaveData();
            data.tutorial.shownSteps ??= new();
        }

        private static bool ValidateParallelLists(GameSaveData data, out string error)
        {
            if (data.inventory.foodIds.Count != data.inventory.amounts.Count)
                return Fail("Inventory legacy id/amount counts differ.", out error);
            if (data.deliveryQuest.groupIds.Count != data.deliveryQuest.stages.Count)
                return Fail("Delivery quest id/stage counts differ.", out error);
            if (data.npcNormalCycle.npcIds.Count != data.npcNormalCycle.indices.Count)
                return Fail("NPC cycle id/index counts differ.", out error);
            if (data.toolUpgrades.ids.Count != data.toolUpgrades.levels.Count)
                return Fail("Tool upgrade id/level counts differ.", out error);
            if (data.storageUpgrades.types.Count != data.storageUpgrades.levels.Count)
                return Fail("Storage upgrade type/level counts differ.", out error);
            if (data.farmUpgrades.types.Count != data.farmUpgrades.levels.Count)
                return Fail("Farm upgrade type/level counts differ.", out error);

            error = null;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // 임시 파일 정리 실패는 원래 저장 결과를 덮어쓰지 않는다.
            }
        }
    }
}
