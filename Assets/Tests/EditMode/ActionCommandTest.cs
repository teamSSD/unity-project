using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Command 패턴 테스트
/// 각 Action Command의 CanExecute/Execute 로직 검증
/// 참고: DiaryModel이 아직 없으므로 모의 객체 사용
/// </summary>
public class ActionCommandTest
{
    // DiaryModel이 Step 3에서 구현되므로 현재는 기본 구조만 테스트

    [Test]
    public void IActionCommand_Interface_Exists()
    {
        // Command 인터페이스가 존재하는지 확인
        var commandType = typeof(IActionCommand);
        Assert.IsNotNull(commandType);
        Assert.IsTrue(commandType.IsInterface);
    }

    [Test]
    public void ActionExecutionResult_SuccessResult_CreatesCorrectly()
    {
        var result = ActionExecutionResult.SuccessResult(ActionState.Done);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ActionState.Done, result.NewState);
        Assert.IsNull(result.ErrorMessage);
    }

    [Test]
    public void ActionExecutionResult_FailureResult_CreatesCorrectly()
    {
        var result = ActionExecutionResult.FailureResult("Test error");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ActionState.Disavailable, result.NewState);
        Assert.AreEqual("Test error", result.ErrorMessage);
    }

    [Test]
    public void MenuSelectAction_CreatesSuccessfully()
    {
        var action = new MenuSelectAction();
        Assert.IsNotNull(action);
        Assert.IsInstanceOf<IActionCommand>(action);
    }

    [Test]
    public void PrepareIngredientsAction_CreatesSuccessfully()
    {
        var action = new PrepareIngredientsAction();
        Assert.IsNotNull(action);
        Assert.IsInstanceOf<IActionCommand>(action);
    }

    [Test]
    public void WorkAction_CreatesSuccessfully()
    {
        var action = new WorkAction();
        Assert.IsNotNull(action);
        Assert.IsInstanceOf<IActionCommand>(action);
    }

    [Test]
    public void RestAction_CreatesSuccessfully()
    {
        var action = new RestAction();
        Assert.IsNotNull(action);
        Assert.IsInstanceOf<IActionCommand>(action);
    }

    [Test]
    public void ShoppingAction_CreatesSuccessfully()
    {
        var action = new ShoppingAction();
        Assert.IsNotNull(action);
        Assert.IsInstanceOf<IActionCommand>(action);
    }

    // Note: 실제 CanExecute/Execute 테스트는 Step 3에서 DiaryModel 완성 후 추가 예정
}
