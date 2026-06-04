using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 서브씬(Delivery, Shop, Market)에서 Idle 씬으로 복귀
/// </summary>
public class SubSceneController : MonoBehaviour
{
    [Header("Return Scene")]
    [SerializeField] private string returnSceneName = SceneNames.Idle;

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

        var ps = GameSessionRoot.Instance?.Progress;
        if (ps == null || !ps.PassPhase())
            SceneLoader.LoadScene(returnSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
