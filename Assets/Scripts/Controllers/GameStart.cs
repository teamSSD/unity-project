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
        // Boot 씬을 거치지 않고 직접 Play한 경우 매니저 보장
        if (StatsSystem.Instance == null)
            ManagerBootstrap.EnsureAll();

        UILockManager.Lock(UILockManager.Owner.GameStart);

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
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        // 무거운 초기화는 로딩 화면이 가린 상태에서 실행되도록 람다로 전달
        SceneLoader.LoadSceneWithInit(SceneNames.Mall, () =>
        {
            // Phase 1: 구조 초기화 (디스크 I/O 없음)
            StatsSystem.Instance.Initialize();
            ProgressSystem.Instance.Initialize();
            UnlockedFoodManager.Instance?.Initialize();
            InventoryManager.Instance?.Initialize();
            RecipeDataManager.Instance?.Initialize();
            RecipeLookupService.Instance?.Initialize();
            OrderManager.Instance?.Initialize();

            // Phase 2: 저장 데이터 로드
            SaveManager.LoadAll();

            // Phase 3: 시드 초기화 (세이브에서 복원 후)
            var stats = StatsSystem.Instance.GetSaveData();
            GameRandom.InitSession(stats.immutableSeed, (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            GameRandom.InitDay(StatsSystem.Instance.GetDay());

            HUDManager.Instance?.Initialize();
        });
    }
    private void NewGame()
    {
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneLoader.LoadSceneWithInit(SceneNames.Mall, () =>
        {
            // Phase 1: 구조 초기화
            StatsSystem.Instance.Initialize();
            ProgressSystem.Instance.Initialize();
            UnlockedFoodManager.Instance?.Initialize();
            InventoryManager.Instance?.Initialize();
            RecipeDataManager.Instance?.Initialize();
            RecipeLookupService.Instance?.Initialize();

            // Phase 2: 새 게임 초기값 설정
            StatsSystem.Instance.SetTime(5, 0);
            StatsSystem.Instance.SetMoney(8000);
            StatsSystem.Instance.SetStamina(100);
            ProgressSystem.Instance.phaseData.Day = 0;
            InventoryManager.Instance?.ResetToDefault();
            UnlockedFoodManager.Instance?.UnlockDefaultRecipes();
            DeliveryNpcDialogueInteraction.ResetAll();
            FarmTileStorage.Clear();

            // Phase 3: 시드 초기화
            int now = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StatsSystem.Instance.GetSaveData().immutableSeed = now;
            GameRandom.InitSession(now, now + 1);
            GameRandom.InitDay(0);

            // Phase 4: 초기 상태 저장
            SaveManager.SaveAll();

            HUDManager.Instance?.Initialize();
            Debug.Log("[GameStart] New Game started");
        });
    }
    private void OpenSetting()
    {
        SettingsUIManager.Instance?.Open();
    }
}
