using UnityEngine;

/// <summary>
/// PrepareIngredients 액션 Command
/// 조건: Preparation 페이즈
/// 결과: Scene_Delivery로 전환 요청, Done 상태로 전환
/// </summary>
public class PrepareIngredientsAction : IActionCommand
{
    public bool CanExecute(PhaseType phase, DiaryModel model)
    {
        // Preparation 페이즈에서만 실행 가능
        return phase == PhaseType.Preparation;
    }

    public ActionExecutionResult Execute(PhaseType phase, DiaryModel model)
    {
        if (!CanExecute(phase, model))
        {
            return ActionExecutionResult.FailureResult("Preparation 페이즈에서만 실행 가능합니다.");
        }

        // 씬 전환 요청 이벤트 발행
        model.RequestSceneTransition("Scene_Delivery");

        Debug.Log("[PrepareIngredientsAction] 재료 수급 시작 - Scene_Delivery로 전환 요청");

        return ActionExecutionResult.SuccessResult(ActionState.Done);
    }
}
