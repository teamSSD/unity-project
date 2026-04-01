using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [LEGACY] 페이즈 버튼 핸들러.
/// PhaseActionSelector와 함께 사용되었으나 IdleViewerUI로 대체됨.
/// Idle 씬에서만 사용되었으므로 삭제 가능.
/// </summary>
[System.Obsolete("IdleViewerUI로 대체됨. 향후 삭제 예정.")]
public class PhaseButtonHandler : MonoBehaviour
{
    [SerializeField] private int phaseIndex = 0; // 0=아침, 1=점심, 2=저녁

    public void OnButtonClick()
    {
        if (ActionSelectionManager.Instance == null)
        {
            Debug.LogError("[PhaseButtonHandler] ActionSelectionManager not found!");
            return;
        }

        var action = ActionSelectionManager.Instance.GetAction(phaseIndex);
        if (action == null || !action.HasSelection())
        {
            Debug.LogWarning($"[PhaseButtonHandler] No action selected for phase {phaseIndex}");
            return;
        }

        Debug.Log($"[PhaseButtonHandler] Executing action: {action.SelectedAction} for phase {phaseIndex}");

        // 액션에 따라 분기
        switch (action.SelectedAction)
        {
            case ActionType.Work:
                // 영업 - Cooking 씬으로
                SceneManager.LoadScene("Cooking");
                break;

            case ActionType.Rest:
                // 휴식 - 스태미너 충전 후 페이즈 진행
                StatsSystem.SetStamina(100);
                if (ProgressSystem.instance != null)
                {
                    ProgressSystem.instance.PassPhase();
                    // 저장은 PassDay()에서만 수행
                }
                Debug.Log("[PhaseButtonHandler] Rested - stamina restored and phase advanced");
                break;

            case ActionType.Shopping:
                // 상가 이동 - Scene_Mall으로
                SceneManager.LoadScene("Scene_Mall");
                break;
        }
    }
}
