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

#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Register("mall.go-home", goHomeButton);
#endif

        // Mall 체류 중 페이즈 전환(Rest / GoHome confirm) 시 새 페이즈 UI 즉시 표시.
        progressService = session?.Progress;
        if (progressService != null)
            progressService.OnPhaseChanged += OnPhaseChangedInMall;

        // Mall 진입 시 UI 결정.
        // - Preparation: 자동 UI 없음 (첫 진입, 새 하루 모두 이 케이스).
        // - Morning: 어차피 실제로 도달 불가(Prep→Cooking 경로), 방어적으로 skip.
        // - Shop/Garden 복귀: 유저가 Shopping/Farming 후 돌아온 것 → 다시 강요하지 않음.
        // - 그 외 (Cooking→Mall 복귀 등): ActionSelector 자동 표시.
        // 주: SceneLoader.CurrentScene은 LoadSceneAdditive의 onComplete 이전엔
        // 이전 씬명을 유지하므로 Mall.Start 실행 중엔 "직전 씬"을 가리킴.
        var currentPhase = session?.Progress?.PhaseData?.Phase ?? PhaseType.Preparation;
        bool cameFromSubScene = SceneLoader.CurrentScene == SceneNames.Shop
                             || SceneLoader.CurrentScene == SceneNames.Garden;
        if (currentPhase != PhaseType.Preparation && currentPhase != PhaseType.Morning && !cameFromSubScene)
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
#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Unregister("mall.go-home", goHomeButton);
#endif
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

        // 튜토리얼 마무리 훅 — Afternoon 페이즈 튜토리얼 진행 중 GoHome 클릭 시 Closing 표시 후
        // Complete + Settlement 씬 로드 (0일차 정산 → 유저가 확인 시 Day 1로 넘어감).
        var tc = TutorialController.Instance;
        if (phase == PhaseType.Afternoon && tc != null && tc.CanShow(TutorialStepId.Closing))
        {
            tc.Show(TutorialStepId.Closing, onDone: () =>
            {
                tc.Complete();
                SceneLoader.LoadScene(SceneNames.Settlement);
            });
            return;
        }

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

        UIFlowController.TryOpenBentoSelection(bentoSelectionController, () =>
        {
            // Preparation → Morning 후 자동으로 Cooking 씬 진입.
            // 튜토리얼 활성 시엔 격리된 CookingTutorial 씬 (Mock).
            var ps = GameSessionRoot.Instance?.Progress;
            ps?.PassPhase();
            bool tutorialActive = GameSessionRoot.Instance?.Tutorial?.IsActive ?? false;
            SceneLoader.LoadScene(tutorialActive ? SceneNames.CookingTutorial : SceneNames.Cooking);
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
        TryStartPhaseSelectTutorial();
    }

    /// <summary>Afternoon 페이즈 진입 시 PhaseSelectAfternoon 스텝 시도.
    /// - Work/Rest 비활성 (Shopping만 가능)
    /// - Part 0 동안엔 Shopping도 비활성 (설명 중 실수 클릭 방지). Part 1 진입 시 Shopping 활성.
    /// - Shopping 클릭 시 튜토리얼 마감 → 연달아 MallCorridor 안내 시작</summary>
    private void TryStartPhaseSelectTutorial()
    {
        var tc = TutorialController.Instance;
        if (tc == null) return;
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData?.Phase ?? PhaseType.Preparation;
        if (phase != PhaseType.Afternoon) return;
        if (!tc.CanShow(TutorialStepId.PhaseSelectAfternoon)) return;

        phaseSelector.SetActionEnabled(ActionType.Work, false);
        phaseSelector.SetActionEnabled(ActionType.Rest, false);
        phaseSelector.SetActionEnabled(ActionType.Shopping, false);

        // Part 1 진입 감지 → Shopping 활성.
        tc.OnPartShown += HandlePhaseSelectPartShown;

        tc.Show(TutorialStepId.PhaseSelectAfternoon, onDone: () =>
        {
            tc.OnPartShown -= HandlePhaseSelectPartShown;
            TryStartMallCorridorTutorial();
        });
    }

    private void HandlePhaseSelectPartShown(int stepId, int partIdx, TutorialStepPart part)
    {
        if (stepId != TutorialStepId.PhaseSelectAfternoon) return;
        // Part 1(Shopping 유도) 진입 시 Shopping 버튼 활성.
        if ((part.message ?? "").StartsWith("이번 점심에는"))
            phaseSelector?.SetActionEnabled(ActionType.Shopping, true);
    }

    /// <summary>PhaseSelectAfternoon 완료 후 자동 시작. Mall 전체 안내 (복도/텃밭/상점/NPC/복귀).</summary>
    private void TryStartMallCorridorTutorial()
    {
        var tc = TutorialController.Instance;
        if (tc == null || !tc.CanShow(TutorialStepId.MallCorridor)) return;
        tc.Show(TutorialStepId.MallCorridor);
    }

    private void OnPhaseActionExecuted(ActionType actionType)
    {
        // 액션 실행 후 UI 닫음 (씬 전환되는 액션은 어차피 UI 자동 destroy).
        phaseSelector?.Hide();

        // 튜토리얼 진행 중 Shopping 선택 → 활성 파트 dismiss (마지막 파트 dismiss 시 MarkShown + onDone).
        if (actionType == ActionType.Shopping)
            TutorialController.Instance?.DismissActivePart();
    }

    /// <summary>씬에 root Canvas가 있으면 그것을, 없으면 새로 만들어 반환.
    /// isRootCanvas 필터 — sub-canvas(예: 튜토리얼 bubble overrideSorting)를 잡으면
    /// 자식으로 붙는 UI가 그 sub-canvas order를 상속받아 위치·순서가 어긋남.</summary>
    private static Canvas FindOrCreateOverlayCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
            if (c != null && c.isRootCanvas) return c;

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
