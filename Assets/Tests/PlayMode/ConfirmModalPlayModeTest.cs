using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ConfirmModalPlayModeTest
{
    private GameObject _host;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _host = new GameObject("ConfirmModalPlayModeTest");
        _host.AddComponent<ConfirmModal>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_host != null) Object.Destroy(_host);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Show_MakesTheModalVisible()
    {
        Assert.IsFalse(ConfirmModal.IsOpen);

        ConfirmModal.Show("제목", "내용", null);
        yield return null;

        Assert.IsTrue(ConfirmModal.IsOpen);
    }
}
