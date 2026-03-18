using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 페이즈 버튼 핸들러
/// ActionSelectionManager에서 선택된 액션을 확인하고 실행
/// </summary>
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

        // 진행 상태 저장
        if (ProgressSystem.instance != null)
        {
            ProgressSystem.instance.flush();
        }
        StatsSystem.flush();

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
                }
                Debug.Log("[PhaseButtonHandler] Rested - stamina restored");
                break;

            case ActionType.Shopping:
                // 상가 이동 - Scene_Mall으로
                SceneManager.LoadScene("Scene_Mall");
                break;
        }
    }
}
