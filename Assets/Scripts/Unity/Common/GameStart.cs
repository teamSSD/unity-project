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
        if (GameSessionRoot.Instance?.Stats == null)
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

#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Register("start.new-game", NewGameButton);
        E2EUiTargetRegistry.Register("start.continue", ContinueButton);
        E2EUiTargetRegistry.Register("start.settings", Settings);
#endif
    }

    private void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Unregister("start.new-game", NewGameButton);
        E2EUiTargetRegistry.Unregister("start.continue", ContinueButton);
        E2EUiTargetRegistry.Unregister("start.settings", Settings);
#endif
    }

    private void ProcessContinue()
    {
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneLoader.LoadSceneWithInit(SceneNames.Mall, () =>
        {
            InitializeManagers();
            GameSessionRoot.Instance?.Order.Clear();

            SaveManager.LoadAll();

            // Continue: 저장된 시드 복원. Day SSOT = PhaseData.Day.
            var session = GameSessionRoot.Instance;
            var saveData = session.Stats.GetSaveData();
            int day = session.Progress?.PhaseData?.Day ?? 0;
            int contSessionSeed = (int)((uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() ^ (uint)System.Environment.TickCount);
            GameRandom.InitSession(saveData.immutableSeed, contSessionSeed);
            GameRandom.InitDay(day);
            session?.Weather?.UpdateWeather(day);

            HUDManager.Instance?.Initialize();
        });
    }

    private void NewGame()
    {
        // 세이브 있으면 덮어쓰기 confirm — 실수 방지.
        if (SaveManager.HasSaveData())
        {
            ConfirmModal.Show(
                title: "새로 시작하시겠습니까?",
                message: "기존 세이브 데이터가 삭제됩니다.",
                onConfirm: StartNewGame,
                yesText: "새로 시작",
                noText: "취소"
            );
            return;
        }
        StartNewGame();
    }

    private void StartNewGame()
    {
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneLoader.LoadSceneWithInit(SceneNames.Mall, () =>
        {
            InitializeManagers();
            ApplyNewGameDefaults();

            // NewGame: 새 시드 생성. E2E 전용 빌드는 장기 운영을 바로 검증하도록
            // 튜토리얼 완료 + 고정 시드 프로필로 시작한다.
#if AFTERTASTE_E2E_LONGRUN
            AftertasteE2EProfileSetup.ApplyAtNewGame(GameStateReporter.CurrentSession);
#else
            int now = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            GameSessionRoot.Instance.Stats.GetSaveData().immutableSeed = now;
            int newSessionSeed = (int)((uint)now ^ (uint)System.Environment.TickCount);
            GameRandom.InitSession(now, newSessionSeed);
            GameRandom.InitDay(0);
            GameSessionRoot.Instance?.Weather?.UpdateWeather(0);
#endif

            SaveManager.SaveAll();
            HUDManager.Instance?.Initialize();
        });
    }

    /// <summary>구조 초기화 — Continue/NewGame 공통 (G 중복 제거).</summary>
    private static void InitializeManagers()
    {
        GameSessionRoot.Instance?.Stats.Reset();
        GameSessionRoot.Instance?.Progress.Initialize();
    }

    /// <summary>New Game 전용 초기값 설정.</summary>
    private static void ApplyNewGameDefaults()
    {
        var session = GameSessionRoot.Instance;
        if (session != null)
        {
            session.Stats.SetTime(5, 0);
            session.Stats.SetMoney(12000);
            session.Stats.SetStamina(100);
            session.Progress.PhaseData.Day = 0;
        }
        GameSessionRoot.Instance?.Inventory?.ResetToDefault();
        GameSessionRoot.Instance?.UnlockedFood?.UnlockDefaultRecipes();
        DeliveryNpcDialogueInteraction.ResetAll();
        if (GameSessionRoot.Instance != null)
            System.Array.Clear(GameSessionRoot.Instance.State.garden.persistent.tiles, 0,
                GameSessionRoot.Instance.State.garden.persistent.tiles.Length);
    }
    private void OpenSetting()
    {
        UIFlowController.TryOpenSettings();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
