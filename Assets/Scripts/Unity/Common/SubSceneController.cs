using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 서브씬(Cooking, Garden 등)에서 Mall 씬으로 복귀
/// </summary>
public class SubSceneController : MonoBehaviour
{
    [Header("Return Scene")]
    [SerializeField] private string returnSceneName = SceneNames.Mall;

    /// <summary>
    /// 작업 완료 후 호출 (버튼 등에 연결)
    /// </summary>
    public void ReturnToIdle()
    {
        // PassPhase가 Night case에서 자동으로 Settlement 씬 로드 후 true 반환 → return-scene 로드 skip.
        // 그 외에는 PassPhase 후 지정된 return 씬(Mall)으로. Mall이 자체적으로 phase 보고 UI 결정.
        var ps = GameSessionRoot.Instance?.Progress;
        if (ps == null) { SceneLoader.LoadScene(returnSceneName); return; }

        // Night: PassPhase가 자체적으로 Settlement 씬 로드 (fade 처리 포함).
        if (ps.PhaseData?.Phase == PhaseType.Night) { ps.PassPhase(); return; }

        // 그 외: fade-in 완료 후 PassPhase 실행 → OnPhaseChanged 로 인한 배경 sprite 교체가
        // 검정 화면 뒤에서 일어나서 유저에게 안 보임 (Cooking/Garden BackgroundController 대응).
        SceneLoader.LoadSceneWithInit(returnSceneName, () => ps.PassPhase());
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
