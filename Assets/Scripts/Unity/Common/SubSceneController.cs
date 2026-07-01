using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 서브씬(Cooking, Garden 등)에서 Mall 씬으로 복귀
/// </summary>
public class SubSceneController : MonoBehaviour
{
    [Header("Return Scene")]
    [SerializeField] private string returnSceneName = SceneNames.Mall;

    private void Start()
    {
        // RecipeBookManager는 항상 View Mode이므로 체크 불필요
        Debug.Log("[SubSceneController] Sub scene started");
    }

    /// <summary>
    /// 작업 완료 후 호출 (버튼 등에 연결)
    /// </summary>
    public void ReturnToIdle()
    {
        Debug.Log($"[SubSceneController] Returning to {returnSceneName}");

        // Cooking 종료로 Mall 돌아갈 때는 페이즈 액션 선택 UI 자동 표시 요청. Garden 등 다른
        // 서브씬은 대상 아님.
        if (gameObject.scene.name == SceneNames.Cooking && returnSceneName == SceneNames.Mall)
            SceneLoader.RequestPhaseSelectorOnNextMall();

        var ps = GameSessionRoot.Instance?.Progress;
        if (ps == null || !ps.PassPhase())
            SceneLoader.LoadScene(returnSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
