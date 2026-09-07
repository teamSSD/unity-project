using System;
using System.IO;
using Game.Unity.Persistence;
using NUnit.Framework;
using UnityEngine;

public class SaveRepositoryTest
{
    private string _directory;
    private string _savePath;
    private SaveRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"aftertaste-save-test-{Guid.NewGuid():N}");
        _savePath = Path.Combine(_directory, "gamedata.json");
        _repository = new SaveRepository(_savePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void SaveAndLoad_RoundTripsAndWritesCurrentSchema()
    {
        var source = CreateSave(day: 7, money: 4321);

        var write = _repository.Save(source);
        var read = _repository.Load();

        Assert.IsTrue(write.Succeeded, write.Error);
        Assert.IsTrue(read.Succeeded, read.Error);
        Assert.AreEqual(SaveRepository.CurrentSchemaVersion, read.Data.schemaVersion);
        Assert.AreEqual(7, read.Data.phase.Day);
        Assert.AreEqual(4321, read.Data.stats.money);
    }

    [Test]
    public void SavingTwice_PreservesPreviousValidSaveAsBackup()
    {
        Assert.IsTrue(_repository.Save(CreateSave(day: 2, money: 100)).Succeeded);
        Assert.IsTrue(_repository.Save(CreateSave(day: 3, money: 200)).Succeeded);

        string backupJson = File.ReadAllText(_savePath + ".bak");
        var backup = JsonUtility.FromJson<GameSaveData>(backupJson);

        Assert.AreEqual(2, backup.phase.Day);
        Assert.AreEqual(100, backup.stats.money);
    }

    [Test]
    public void SavingThreeTimes_RotatesBackupToImmediatelyPreviousSave()
    {
        Assert.IsTrue(_repository.Save(CreateSave(day: 2, money: 100)).Succeeded);
        Assert.IsTrue(_repository.Save(CreateSave(day: 3, money: 200)).Succeeded);
        var thirdWrite = _repository.Save(CreateSave(day: 4, money: 300));

        Assert.IsTrue(thirdWrite.Succeeded, thirdWrite.Error);
        var current = _repository.Load();
        var backup = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(_savePath + ".bak"));
        Assert.AreEqual(4, current.Data.phase.Day);
        Assert.AreEqual(3, backup.phase.Day);
    }

    [Test]
    public void CorruptPrimary_LoadsLastValidBackup()
    {
        Assert.IsTrue(_repository.Save(CreateSave(day: 2, money: 100)).Succeeded);
        Assert.IsTrue(_repository.Save(CreateSave(day: 3, money: 200)).Succeeded);
        File.WriteAllText(_savePath, "{not-json");

        var read = _repository.Load();

        Assert.IsTrue(read.Succeeded, read.Error);
        Assert.IsTrue(read.RecoveredFromBackup);
        Assert.AreEqual(2, read.Data.phase.Day);
        Assert.AreEqual(100, read.Data.stats.money);
    }

    [Test]
    public void TemporaryWriteFailure_PreservesExistingSave()
    {
        Assert.IsTrue(_repository.Save(CreateSave(day: 4, money: 100)).Succeeded);
        Directory.CreateDirectory(_savePath + ".tmp");

        var failedWrite = _repository.Save(CreateSave(day: 5, money: 999));

        Assert.IsFalse(failedWrite.Succeeded);
        Directory.Delete(_savePath + ".tmp");
        var read = _repository.Load();
        Assert.IsTrue(read.Succeeded, read.Error);
        Assert.AreEqual(4, read.Data.phase.Day);
        Assert.AreEqual(100, read.Data.stats.money);
    }

    [Test]
    public void LegacyUnversionedSave_LoadsAsCurrentSchema()
    {
        Directory.CreateDirectory(_directory);
        var legacy = CreateSave(day: 8, money: 800);
        string json = JsonUtility.ToJson(legacy)
            .Replace($"\"schemaVersion\":{SaveRepository.CurrentSchemaVersion},", string.Empty);
        File.WriteAllText(_savePath, json);

        var read = _repository.Load();

        Assert.IsTrue(read.Succeeded, read.Error);
        Assert.AreEqual(SaveRepository.CurrentSchemaVersion, read.Data.schemaVersion);
        Assert.AreEqual(8, read.Data.phase.Day);
    }

    [Test]
    public void LegacySaveWithOnlyRequiredFields_InitializesOptionalSlots()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_savePath, "{\"phase\":{\"Day\":6},\"stats\":{\"money\":600}}");

        var read = _repository.Load();

        Assert.IsTrue(read.Succeeded, read.Error);
        Assert.IsNotNull(read.Data.inventory);
        Assert.IsNotNull(read.Data.orders);
        Assert.IsNotNull(read.Data.recipeBook);
        Assert.IsNotNull(read.Data.tutorial);
    }

    [Test]
    public void MissingRequiredState_IsRejectedBeforeApply()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_savePath, "{\"schemaVersion\":1,\"phase\":{\"Day\":6}}");

        var read = _repository.Load();

        Assert.IsFalse(read.Succeeded);
        Assert.IsNull(read.Data);
        StringAssert.Contains("missing required", read.Error);
    }

    [Test]
    public void MismatchedParallelLists_AreRejectedBeforeApply()
    {
        Directory.CreateDirectory(_directory);
        var invalid = CreateSave(day: 6, money: 600);
        invalid.deliveryQuest.groupIds.Add("npc-a");
        File.WriteAllText(_savePath, JsonUtility.ToJson(invalid));

        var read = _repository.Load();

        Assert.IsFalse(read.Succeeded);
        Assert.IsNull(read.Data);
        StringAssert.Contains("counts differ", read.Error);
    }

    [Test]
    public void FutureSchema_IsRejectedWithoutReturningData()
    {
        Directory.CreateDirectory(_directory);
        var future = CreateSave(day: 9, money: 900);
        future.schemaVersion = SaveRepository.CurrentSchemaVersion + 1;
        File.WriteAllText(_savePath, JsonUtility.ToJson(future));

        var read = _repository.Load();

        Assert.IsFalse(read.Succeeded);
        Assert.IsTrue(read.Found);
        Assert.IsNull(read.Data);
        StringAssert.Contains("Unsupported save schema", read.Error);
    }

    [Test]
    public void MissingSave_IsReportedSeparatelyFromFailure()
    {
        var read = _repository.Load();

        Assert.IsFalse(read.Succeeded);
        Assert.IsFalse(read.Found);
        Assert.IsNull(read.Error);
    }

    private static GameSaveData CreateSave(int day, int money)
    {
        var save = new GameSaveData();
        save.phase.Day = day;
        save.stats.money = money;
        return save;
    }
}
