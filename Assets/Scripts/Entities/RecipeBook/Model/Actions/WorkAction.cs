using UnityEngine;

/// <summary>
/// Work 액션 Command
/// 조건: Morning/Afternoon/Evening/Night 페이즈
/// 결과: Scene_Shop으로 전환 요청, Selected 상태로 전환
/// </summary>
public class WorkAction : IActionCommand
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
            return ActionExecutionResult.FailureResult("Preparation 페이즈에서는 영업할 수 없습니다.");
        }

        // 씬 전환 요청 이벤트 발행
        model.RequestSceneTransition("Scene_Shop");

        Debug.Log($"[WorkAction] {phase} 영업 시작 - Scene_Shop으로 전환 요청");

        // SINGLE 모드: 씬 전환 후 복귀 시 Done으로 변경 예정
        // 현재는 Selected 상태로 설정
        return ActionExecutionResult.SuccessResult(ActionState.Selected);
    }
}
