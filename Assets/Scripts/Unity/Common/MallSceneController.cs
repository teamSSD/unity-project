using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Scene_Mall 씬 컨트롤러. Idle 씬 흡수 후:
/// - Preparation 페이즈: GoHome 시 메뉴 선택 modal → 확인 시 PassPhase + Cooking 자동 진입
/// - 그 외 페이즈: GoHome 시 PhaseActionSelector UI (Work/Rest/Shopping 카드)
/// </summary>
public class MallSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button goHomeButton;
    [SerializeField] private GameObject bentoSelectionPrefab;
    [SerializeField] private GameObject phaseSelectionPrefab;

    private BentoSelectionController bentoSelectionController;
    private PhaseActionSelector phaseSelector;
    private ProgressService progressService;

    private void Start()
    {
        Debug.Log("[MallSceneController] Mall scene started");

        // 키보드 nav(Submit/Move)가 player 이동키와 충돌해 무심코 버튼이 selected 되면
        // Space로 goHomeButton 같은 게 트리거되어 의도치 않은 UI(메뉴 선택)가 뜸.
        // 마우스 클릭은 그대로 작동하므로 nav만 차단.
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        // Return position 있으면 player 위치 override. 카메라는 return 유무와 무관하게
        // player+offset으로 즉시 정렬 (첫 진입에도 lerp 슬라이드 방지).
        var player = GameObject.FindGameObjectWithTag(Tags.Player);
        if (player != null)
        {
            if (SceneLoader.MallReturnPosition.HasValue)
            {
                player.transform.position = SceneLoader.MallReturnPosition.Value;
                SceneLoader.ClearMallReturnPosition();
            }

            Object.FindFirstObjectByType<CameraFollow>()?.SnapToPlayer();
        }

        var session = GameSessionRoot.Instance;

        // Preparation 페이즈 진입 시 메뉴 초기화
        if (session?.Progress?.PhaseData.Phase == PhaseType.Preparation)
            session?.MenuSelection?.ClearAllMenus();

        if (bentoSelectionPrefab != null)
        {
            var bentoInst = Instantiate(bentoSelectionPrefab);
            bentoSelectionController = bentoInst.GetComponent<BentoSelectionController>();
            if (bentoSelectionController != null)
            {
                bentoSelectionController.Inject(session?.UnlockedFood, session?.MenuSelection);
                bentoSelectionController.Close();
            }
        }
        else
        {
            Debug.LogError("[MallSceneController] BentoSelection prefab is not assigned!");
        }

        if (phaseSelectionPrefab != null)
        {
            var parentCanvas = FindOrCreateOverlayCanvas();
            var phaseInst = Instantiate(phaseSelectionPrefab, parentCanvas.transform, false);
            phaseSelector = phaseInst.GetComponentInChildren<PhaseActionSelector>(includeInactive: true);
            if (phaseSelector != null)
            {
                phaseSelector.OnActionExecuted += OnPhaseActionExecuted;
                phaseSelector.Hide();
            }
        }
        else
        {
            Debug.LogError("[MallSceneController] PhaseSelection prefab is not assigned!");
        }

        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(GoHome);

        // Mall 체류 중 페이즈 전환(Rest / GoHome confirm) 시 새 페이즈 UI 즉시 표시.
        progressService = session?.Progress;
        if (progressService != null)
            progressService.OnPhaseChanged += OnPhaseChangedInMall;

        // Mall 진입 시 UI 결정.
        // - Preparation: 자동 UI 없음 (첫 진입, 새 하루 모두 이 케이스).
        // - Morning: 어차피 실제로 도달 불가(Prep→Cooking 경로), 방어적으로 skip.
        // - Shop 복귀: 유저가 Shopping 후 돌아온 것 → 다시 강요하지 않음.
        // - 그 외 (Cooking→Mall 복귀 등): ActionSelector 자동 표시.
        // 주: SceneLoader.CurrentScene은 LoadSceneAdditive의 onComplete 이전엔
        // 이전 씬명을 유지하므로 Mall.Start 실행 중엔 "직전 씬"을 가리킴.
        var currentPhase = session?.Progress?.PhaseData?.Phase ?? PhaseType.Preparation;
        bool cameFromShop = SceneLoader.CurrentScene == SceneNames.Shop;
        if (currentPhase != PhaseType.Preparation && currentPhase != PhaseType.Morning && !cameFromShop)
            OpenActionSelection();

        TryStartWelcomeTutorial(currentPhase);
    }

    /// <summary>Preparation 첫 진입 시 튜토리얼 활성 상태면 Welcome 스텝 순차 표시.</summary>
    private void TryStartWelcomeTutorial(PhaseType currentPhase)
    {
        if (currentPhase != PhaseType.Preparation) return;
        var tc = TutorialController.Instance;
        if (tc == null) return;
        if (!tc.CanShow(TutorialStepId.WelcomeAtSpawn)) return;
        tc.Show(TutorialStepId.WelcomeAtSpawn);
    }

    private void OnDestroy()
    {
        if (phaseSelector != null)
            phaseSelector.OnActionExecuted -= OnPhaseActionExecuted;
        if (progressService != null)
            progressService.OnPhaseChanged -= OnPhaseChangedInMall;
    }

    /// <summary>Mall 체류 중 페이즈 전환 시 ActionSelector 자동 표시. Mall.Start와 같은 규칙.</summary>
    private void OnPhaseChangedInMall(PhaseType newPhase)
    {
        if (newPhase == PhaseType.Preparation || newPhase == PhaseType.Morning) return;
        OpenActionSelection();
    }

    /// <summary>Preparation은 메뉴 선택, 그 외 모든 phase는 페이즈 마치기 confirm. 액션 선택은
    /// Cooking 복귀 시 Start에서 자동 표시(GoHome 트리거 아님).</summary>
    public void GoHome()
    {
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData.Phase ?? PhaseType.Preparation;

        if (phase == PhaseType.Preparation)
            OpenMenuSelection();
        else
            ConfirmPassPhase(phase);
    }

    private static void ConfirmPassPhase(PhaseType phase)
    {
        bool isNight = phase == PhaseType.Night;
        string title = isNight ? "하루를 마치시겠습니까?" : "이 페이즈를 마치시겠습니까?";
        string message = isNight ? "잠들면 다음 날이 시작됩니다." : "다음 페이즈로 넘어갑니다.";
        ConfirmModal.Show(
            title: title,
            message: message,
            onConfirm: () => GameSessionRoot.Instance?.Progress?.PassPhase(),
            yesText: "마치기",
            noText: "취소"
        );
    }

    private void OpenMenuSelection()
    {
        if (bentoSelectionController == null)
        {
            Debug.LogError("[MallSceneController] BentoSelectionController is null!");
            return;
        }

        bentoSelectionController.Show(() =>
        {
            // Preparation → Morning 후 자동으로 Cooking 씬 진입
            var ps = GameSessionRoot.Instance?.Progress;
            ps?.PassPhase();
            SceneLoader.LoadScene(SceneNames.Cooking);
        });
    }

    private void OpenActionSelection()
    {
        if (phaseSelector == null)
        {
            Debug.LogError("[MallSceneController] PhaseActionSelector is null!");
            return;
        }
        phaseSelector.Show();
    }

    private void OnPhaseActionExecuted(ActionType actionType)
    {
        // 액션 실행 후 UI 닫음 (씬 전환되는 액션은 어차피 UI 자동 destroy).
        phaseSelector?.Hide();
    }

    /// <summary>씬에 Canvas가 있으면 그것을, 없으면 새로 만들어 반환.</summary>
    private static Canvas FindOrCreateOverlayCanvas()
    {
        var existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        var go = new GameObject("MallCanvas",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
