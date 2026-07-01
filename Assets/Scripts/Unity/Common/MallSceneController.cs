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

    private void Start()
    {
        Debug.Log("[MallSceneController] Mall scene started");

        // 키보드 nav(Submit/Move)가 player 이동키와 충돌해 무심코 버튼이 selected 되면
        // Space로 goHomeButton 같은 게 트리거되어 의도치 않은 UI(메뉴 선택)가 뜸.
        // 마우스 클릭은 그대로 작동하므로 nav만 차단.
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        if (SceneLoader.MallReturnPosition.HasValue)
        {
            var player = GameObject.FindGameObjectWithTag(Tags.Player);
            if (player != null)
            {
                player.transform.position = SceneLoader.MallReturnPosition.Value;

                var cam = Object.FindFirstObjectByType<CameraFollow>();
                if (cam != null)
                {
                    cam.transform.position = new Vector3(
                        player.transform.position.x + cam.offset.x,
                        player.transform.position.y + cam.offset.y,
                        cam.transform.position.z);
                }
            }
            SceneLoader.ClearMallReturnPosition();
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
    }

    private void OnDestroy()
    {
        if (phaseSelector != null)
            phaseSelector.OnActionExecuted -= OnPhaseActionExecuted;
    }

    /// <summary>Phase 따라 메뉴 선택 modal / 액션 선택 UI / 밤 종료 확인.</summary>
    public void GoHome()
    {
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData.Phase ?? PhaseType.Preparation;

        if (phase == PhaseType.Preparation)
            OpenMenuSelection();
        else if (phase == PhaseType.Night)
            ConfirmEndDay();
        else
            OpenActionSelection();
    }

    private void ConfirmEndDay()
    {
        ConfirmModal.Show(
            title: "하루를 마치시겠습니까?",
            message: "잠들면 다음 날이 시작됩니다.",
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
