using System;
using System.Collections;
using System.IO;
using Game.Unity.Persistence;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SaveReentryPlayModeTest
{
    private string _directory;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"aftertaste-play-save-{Guid.NewGuid():N}");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SaveThenReenter_RestoresCoreState()
    {
        var repository = new SaveRepository(Path.Combine(_directory, "gamedata.json"));
        var data = new GameSaveData { phase = new PhaseData { Day = 4 }, stats = new BasicStats { money = 2500 } };

        Assert.IsTrue(repository.Save(data).Succeeded);
        yield return null;

        var restored = repository.Load();
        Assert.IsTrue(restored.Succeeded, restored.Error);
        Assert.AreEqual(4, restored.Data.phase.Day);
        Assert.AreEqual(2500, restored.Data.stats.money);
    }
}
