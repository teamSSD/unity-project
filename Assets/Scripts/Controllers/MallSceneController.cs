using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene_Mall 씬 컨트롤러.
/// 상가에서 메뉴 선택 후 집으로 돌아가기를 제어합니다.
/// </summary>
public class MallSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button goHomeButton;
    [SerializeField] private GameObject bentoSelectionPrefab;

    private GameObject bentoSelectionInstance;
    private BentoSelectionController bentoSelectionController;

    private void Start()
    {
        Debug.Log("[MallSceneController] Mall scene started");

        // UnlockedFoodManager 초기화 (없으면 생성)
        EnsureUnlockedFoodManager();

        if (bentoSelectionPrefab != null)
        {
            bentoSelectionInstance = Instantiate(bentoSelectionPrefab);
            bentoSelectionController = bentoSelectionInstance.GetComponent<BentoSelectionController>();

            if (bentoSelectionController != null)
            {
                bentoSelectionController.Close();
            }
        }
        else
        {
            Debug.LogError("[MallSceneController] BentoSelection prefab is not assigned in the inspector!");
        }

        if (goHomeButton != null)
        {
            goHomeButton.onClick.AddListener(OnGoHome);
        }
    }

    private void OnGoHome()
    {
        Debug.Log("[MallSceneController] Opening BentoSelection...");

        if (bentoSelectionController != null)
        {
            bentoSelectionController.Show(() =>
            {
                Debug.Log("[MallSceneController] Selection confirmed, transitioning to Idle...");
                SceneManager.LoadScene("Idle");
            });
        }
        else
        {
            Debug.LogError("[MallSceneController] BentoSelectionController is null!");
        }
    }

    private void EnsureUnlockedFoodManager()
    {
        if (UnlockedFoodManager.Instance == null)
        {
            GameObject managerObj = new GameObject("UnlockedFoodManager");
            managerObj.AddComponent<UnlockedFoodManager>();
            Debug.Log("[MallSceneController] Created UnlockedFoodManager");
        }
    }
}
