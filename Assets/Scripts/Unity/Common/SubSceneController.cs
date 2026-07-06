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

        // PassPhase가 Night case에서 자동으로 Settlement 씬 로드 후 true 반환 → return-scene 로드 skip.
        // 그 외에는 PassPhase 후 지정된 return 씬(Mall)으로. Mall이 자체적으로 phase 보고 UI 결정.
        var ps = GameSessionRoot.Instance?.Progress;
        if (ps == null || !ps.PassPhase())
            SceneLoader.LoadScene(returnSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
