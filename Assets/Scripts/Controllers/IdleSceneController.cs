using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idle 씬의 메인 컨트롤러
/// RecipeBook 통합 및 씬 전환 관리
/// </summary>
public class IdleSceneController : MonoBehaviour
{
    [Header("RecipeBook")]
    [SerializeField] private RecipeBookManager recipeBookManager;

    private void Start()
    {
        // UnlockedFoodManager 초기화 (없으면 생성)
        EnsureUnlockedFoodManager();

        if (recipeBookManager == null)
        {
            recipeBookManager = FindObjectOfType<RecipeBookManager>();
        }

        Debug.Log("[IdleSceneController] Idle scene started");

        // BentoSelectionController는 Scene_Mall에서 열리고, 확인 후 Idle로 전환됨
    }

    /// <summary>
    /// 씬 전환 (간단한 헬퍼 메서드로 사용 가능)
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        Debug.Log($"[IdleSceneController] Scene transition: {sceneName}");

        // RecipeBook은 항상 View Mode이므로 리셋 불필요
        // flush()는 PassPhase() 또는 PassDay()에서만 호출 (씬 전환마다 저장 불필요)

        // 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    private void EnsureUnlockedFoodManager()
    {
        if (UnlockedFoodManager.Instance == null)
        {
            GameObject managerObj = new GameObject("UnlockedFoodManager");
            managerObj.AddComponent<UnlockedFoodManager>();
            Debug.Log("[IdleSceneController] Created UnlockedFoodManager");
        }
    }
}
