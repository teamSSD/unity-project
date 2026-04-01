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
        // Manager 초기화 (없으면 생성)
        ManagerBootstrap.EnsureAll();

        // Continue 버튼 활성화/비활성화
        bool hasSaveData = ProgressSystem.Instance?.IsLoadable() ?? false;

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
        // 저장된 데이터 로드
        StatsSystem.Initialize();
        ProgressSystem.Instance.Initialize();
        UnlockedFoodManager.Instance?.Initialize();
        InventoryManager.Instance?.Initialize();
        RecipeDataManager.Instance?.Initialize();
        OrderManager.Instance?.Initialize();

        // Scene_Mall 씬 로드 (메뉴 선택 → Idle)
        SceneManager.LoadScene("Scene_Mall");
    }
    /// <summary>
    /// 새 게임을 시작합니다.
    ///
    /// ⚠️ 경고: 게임 시작 시 초기 데이터를 저장합니다!
    /// 저장은 여기와 PassDay()에서만 수행됩니다!
    /// </summary>
    private void NewGame()
    {
        // StatsSystem 초기화
        StatsSystem.Initialize();

        // 초기 상태 설정 (D+0, 영업준비 05:00, 돈 3000, 스태미나 100)
        StatsSystem.SetTime(5, 0);       // 05:00 (영업준비 시작)
        StatsSystem.SetMoney(3000);      // 3000원
        StatsSystem.SetStamina(100);     // 100

        // ProgressSystem 초기화
        ProgressSystem.Instance.Initialize();

        // PhaseData.Day를 0으로 설정 (기본값 1 → 0)
        ProgressSystem.Instance.phaseData.Day = 0;

        // Manager 초기화 (New Game이므로 초기값으로 리셋)
        UnlockedFoodManager.Instance?.Initialize();
        InventoryManager.Instance?.ResetToDefault();
        RecipeDataManager.Instance?.Initialize();

        // 배달 퀘스트 초기화
        DeliveryNpcDialogueInteraction.ResetAll();

        // ⚠️ 모든 게임 데이터 초기값 저장 (New Game과 PassDay()에서만 저장!)
        StatsSystem.flush();
        ProgressSystem.Instance.flush();
        InventoryManager.Instance?.flush();
        RecipeDataManager.Instance?.flush();
        OrderManager.Instance?.flush();
        DeliveryNpcDialogueInteraction.flush();

        Debug.Log("[GameStart] New Game - 초기 데이터 저장 완료");

        // Scene_Mall 씬 로드 (메뉴 선택 → Idle)
        SceneManager.LoadScene("Scene_Mall");
    }
    private void OpenSetting()
    {
        // 세팅창 열기~
    }
}
