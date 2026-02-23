/// <summary>
/// Command 패턴 인터페이스
/// 각 ActionType별 실행 로직을 캡슐화하여 switch-case 제거
/// </summary>
public interface IActionCommand
{
    /// <summary>
    /// 액션 실행 가능 여부 검증
    /// </summary>
    /// <param name="phase">현재 페이즈</param>
    /// <param name="model">DiaryModel 참조</param>
    /// <returns>실행 가능하면 true</returns>
    bool CanExecute(PhaseType phase, DiaryModel model);

    /// <summary>
    /// 액션 실행
    /// </summary>
    /// <param name="phase">현재 페이즈</param>
    /// <param name="model">DiaryModel 참조</param>
    /// <returns>실행 결과</returns>
    ActionExecutionResult Execute(PhaseType phase, DiaryModel model);
}

/// <summary>
/// 액션 실행 결과
/// </summary>
public struct ActionExecutionResult
{
    public bool Success;
    public ActionState NewState; // Selected, Done 등
    public string ErrorMessage;

    public static ActionExecutionResult SuccessResult(ActionState newState)
    {
        return new ActionExecutionResult
        {
            Success = true,
            NewState = newState,
            ErrorMessage = null
        };
    }

    public static ActionExecutionResult FailureResult(string errorMessage)
    {
        return new ActionExecutionResult
        {
            Success = false,
            NewState = ActionState.Disavailable,
            ErrorMessage = errorMessage
        };
    }
}
