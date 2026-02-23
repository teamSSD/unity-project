using UnityEngine;

/// <summary>
/// Rest 액션 Command
/// 조건: Morning/Afternoon/Evening/Night 페이즈
/// 결과: 스테미너 충전, Selected 상태로 전환
/// </summary>
public class RestAction : IActionCommand
{
    public bool CanExecute(PhaseType phase, DiaryModel model)
    {
        // Preparation 제외한 모든 페이즈에서 실행 가능
        return phase != PhaseType.Preparation;
    }

    public ActionExecutionResult Execute(PhaseType phase, DiaryModel model)
    {
        if (!CanExecute(phase, model))
        {
            return ActionExecutionResult.FailureResult("Preparation 페이즈에서는 휴식할 수 없습니다.");
        }

        // 스테미너 충전 (StatsSystem 사용)
        StatsSystem.SetStamina(100);

        Debug.Log($"[RestAction] {phase} 휴식 - 스테미너 100% 충전");
    
        // SINGLE 모드: 즉시 완료 (씬 전환 없음)
        return ActionExecutionResult.SuccessResult(ActionState.Selected);
    }
}
