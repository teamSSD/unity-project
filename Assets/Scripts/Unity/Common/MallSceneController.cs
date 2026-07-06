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

        // Mall 체류 중 페이즈가 넘어가는 순간마다(Rest / GoHome confirm / etc.) 새 페이즈의 UI를
        // 즉시 자동 표시. 씬 진입 전에 이미 넘어간 페이즈(Cooking → Mall 흐름)는 아래 flag가 대신 잡음.
        progressService = session?.Progress;
        if (progressService != null)
            progressService.OnPhaseChanged += OnPhaseChangedInMall;

        // Cooking 종료로 Mall 복귀한 직후엔 이번 페이즈 액션을 바로 선택하게 UI 자동 표시.
        // (Mall.Start 이전에 PassPhase가 발화되어 OnPhaseChanged 구독 이전이라 이 flag가 필요.)
        if (SceneLoader.ConsumePendingPhaseSelector())
        {
            OpenActionSelection();
        }
        // Settlement 종료 → 새 하루 Mall 진입 시엔 메뉴 선택 자동. 게임 첫 진입(Continue/NewGame)엔
        // flag가 세팅 안 되어 유저 자유롭게 Mall 탐색 가능.
        else if (SceneLoader.ConsumePendingMenuSelection())
        {
            OpenMenuSelection();
        }
    }

    private void OnDestroy()
    {
        if (phaseSelector != null)
            phaseSelector.OnActionExecuted -= OnPhaseActionExecuted;
        if (progressService != null)
            progressService.OnPhaseChanged -= OnPhaseChangedInMall;
    }

    /// <summary>Mall 체류 중 페이즈 전환 시 다음 페이즈 UI 즉시 표시.</summary>
    private void OnPhaseChangedInMall(PhaseType newPhase)
    {
        // Morning은 오직 Preparation→Morning 경로로만 도달 (메뉴 선택 콜백에서 PassPhase 후
        // LoadScene(Cooking)이 뒤따름). Mall UI 열면 순간 flicker → skip.
        // Night 종료 = PassPhase가 Settlement 씬 로드 후 OnPhaseChanged 미발화 → 여기 도달 안 함.
        if (newPhase == PhaseType.Morning) return;

        if (newPhase == PhaseType.Preparation)
            OpenMenuSelection();
        else
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
