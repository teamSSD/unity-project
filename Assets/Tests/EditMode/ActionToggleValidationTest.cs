using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>
/// ActionToggle 컴포넌트 검증 테스트
/// </summary>
public class ActionToggleValidationTest
{
    [Test]
    public void ActionToggle_WithNoneActionType_HasNoneType()
    {
        // Arrange
        var go = new GameObject("TestActionToggle");
        var toggle = go.AddComponent<Toggle>();

        // DiaryActionManager가 없어서 발생하는 에러는 예상된 동작
        LogAssert.Expect(LogType.Error, "DiaryActionManager instance not found in scene.");

        var actionToggle = go.AddComponent<ActionToggle>();
        actionToggle.actionType = ActionType.None;

        // Assert
        Assert.AreEqual(ActionType.None, actionToggle.actionType, "ActionType should be None");

        // Cleanup - OnDisable에서도 에러 발생 예상
        LogAssert.Expect(LogType.Error, "DiaryActionManager instance not found in scene.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ActionToggle_WithValidActionType_InitializesCorrectly()
    {
        // Arrange
        var go = new GameObject("TestActionToggle");
        var toggle = go.AddComponent<Toggle>();

        // DiaryActionManager가 없어서 발생하는 에러는 예상된 동작
        LogAssert.Expect(LogType.Error, "DiaryActionManager instance not found in scene.");
        var actionToggle = go.AddComponent<ActionToggle>();

        // Act - Inspector에서 수동으로 설정하는 것처럼
        var serializedObject = new UnityEditor.SerializedObject(actionToggle);
        serializedObject.FindProperty("actionType").enumValueIndex = (int)ActionType.Work;
        serializedObject.FindProperty("toggle").objectReferenceValue = toggle;
        serializedObject.FindProperty("phase").enumValueIndex = (int)PhaseType.Preparation;
        serializedObject.ApplyModifiedProperties();

        // Assert
        Assert.AreEqual(ActionType.Work, actionToggle.actionType, "ActionType should match");

        // Cleanup - OnDisable에서도 에러 발생 예상
        LogAssert.Expect(LogType.Error, "DiaryActionManager instance not found in scene.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ActionDatabase_AllEnumValues_Exist()
    {
        // Arrange
        var allActionTypes = System.Enum.GetValues(typeof(ActionType));

        // Act & Assert
        foreach (ActionType actionType in allActionTypes)
        {
            if (actionType == ActionType.None)
                continue;

            bool exists = ActionDatabase.Exists(actionType);
            Assert.IsTrue(exists, $"ActionType.{actionType} not found in ActionDatabase!");
        }
    }

    [Test]
    public void ActionDatabase_ValidateContent()
    {
        // Arrange
        var allActionTypes = ActionDatabase.GetAllActionTypes();
        int count = 0;

        // Act & Assert
        foreach (var actionType in allActionTypes)
        {
            var info = ActionDatabase.GetInfo(actionType);
            Assert.AreNotEqual(ActionType.None, actionType, $"ActionDatabase contains invalid ActionType.None");
            Assert.IsFalse(string.IsNullOrEmpty(info.DisplayName), $"ActionType.{actionType} has empty DisplayName");
            Assert.IsFalse(string.IsNullOrEmpty(info.Description), $"ActionType.{actionType} has empty Description");
            count++;
        }

        Debug.Log($"[Test] Validated {count} actions in ActionDatabase");
    }
}
