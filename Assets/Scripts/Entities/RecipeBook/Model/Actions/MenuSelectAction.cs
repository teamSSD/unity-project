using UnityEngine;

/// <summary>
/// MenuSelect 액션 Command
/// 조건: 최소 1개 도시락 선택 필요
/// 결과: 도시락 선택 잠금, Done 상태로 전환
/// </summary>
public class MenuSelectAction : IActionCommand
{
    public bool CanExecute(PhaseType phase, DiaryModel model)
    {
        // Preparation 페이즈에서만 실행 가능
        if (phase != PhaseType.Preparation)
            return false;

        // 최소 1개 도시락 선택 필요 (기획 요구사항)
        return model.HasAnyBentoSelection();
    }

    public ActionExecutionResult Execute(PhaseType phase, DiaryModel model)
    {
        if (!CanExecute(phase, model))
        {
            return ActionExecutionResult.FailureResult("도시락이 선택되지 않았습니다.");
        }

        // 도시락 검증
        string validationError = model.ValidateBentoSelections();
        if (!string.IsNullOrEmpty(validationError))
        {
            return ActionExecutionResult.FailureResult(validationError);
        }

        // 도시락 선택 잠금 (UI 수정 불가)
        model.LockBentoSelections();

        Debug.Log($"[MenuSelectAction] 도시락 선택 완료:\n{model.GetBentoSummary()}");

        return ActionExecutionResult.SuccessResult(ActionState.Done);
    }
}
