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
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        SceneLoader.LoadSceneWithInit(SceneNames.Mall, () =>
        {
            InitializeManagers();
            ApplyNewGameDefaults();

            // NewGame: 새 시드 생성
            int now = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            GameSessionRoot.Instance.Stats.GetSaveData().immutableSeed = now;
            int newSessionSeed = (int)((uint)now ^ (uint)System.Environment.TickCount);
            GameRandom.InitSession(now, newSessionSeed);
            GameRandom.InitDay(0);
            GameSessionRoot.Instance?.Weather?.UpdateWeather(0);

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
        SettingsUIManager.Instance?.Open();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
