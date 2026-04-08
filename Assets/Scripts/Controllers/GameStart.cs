using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField] private Button ContinueButton;
    [SerializeField] private Button NewGameButton;
    [SerializeField] private Button Settings;
    
    void Start()
    {
        UILockManager.Lock(UILockManager.Owner.GameStart);

        // Manager 초기화 (없으면 생성)
        ManagerBootstrap.EnsureAll();

        // Continue 버튼 활성화/비활성화
        bool hasSaveData = SaveManager.HasSaveData();

        if (ContinueButton != null)
        {
            ContinueButton.interactable = hasSaveData;

            // 비활성화 시 색상 변경 (어두운 회색)
            if (!hasSaveData)
            {
                var colors = ContinueButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                ContinueButton.colors = colors;
            }
        }

        // 버튼 이벤트 연결
        if (NewGameButton != null)
            NewGameButton.onClick.AddListener(NewGame);

        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(ProcessContinue);

        if (Settings != null)
            Settings.onClick.AddListener(OpenSetting);
    }

    private void ProcessContinue()
    {
        // Phase 1: 구조 초기화 (디스크 I/O 없음)
        StatsSystem.Instance.Initialize();
        ProgressSystem.Instance.Initialize();
        UnlockedFoodManager.Instance?.Initialize();
        InventoryManager.Instance?.Initialize();
        RecipeDataManager.Instance?.Initialize();
        OrderManager.Instance?.Initialize();

        // Phase 2: 저장 데이터 로드
        SaveManager.LoadAll();

        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneManager.LoadScene("Scene_Mall");
    }
    private void NewGame()
    {
        // Phase 1: 구조 초기화
        StatsSystem.Instance.Initialize();
        ProgressSystem.Instance.Initialize();
        UnlockedFoodManager.Instance?.Initialize();
        InventoryManager.Instance?.Initialize();
        RecipeDataManager.Instance?.Initialize();

        // Phase 2: 새 게임 초기값 설정
        StatsSystem.Instance.SetTime(5, 0);
        StatsSystem.Instance.SetMoney(8000);
        StatsSystem.Instance.SetStamina(100);
        ProgressSystem.Instance.phaseData.Day = 0;
        InventoryManager.Instance?.ResetToDefault();
        UnlockedFoodManager.Instance?.UnlockDefaultRecipes();
        DeliveryNpcDialogueInteraction.ResetAll();

        // Phase 3: 초기 상태 저장
        SaveManager.SaveAll();

        Debug.Log("[GameStart] New Game started");
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneManager.LoadScene("Scene_Mall");
    }
    private void OpenSetting()
    {
        // 세팅창 열기~
    }
}
