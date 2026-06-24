using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Scene_Mall 씬 컨트롤러.
/// - Preparation 페이즈: 메뉴 선택 UI 열기 (매일 초기화)
/// - 그 외 페이즈: "{페이즈}를 끝내시겠습니까?" 확인 다이얼로그 후 PassPhase
/// </summary>
public class MallSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button goHomeButton;
    [SerializeField] private GameObject bentoSelectionPrefab;
    [SerializeField] private DialogueManager dialogueManager;

    private GameObject bentoSelectionInstance;
    private BentoSelectionController bentoSelectionController;

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
            bentoSelectionInstance = Instantiate(bentoSelectionPrefab);
            bentoSelectionController = bentoSelectionInstance.GetComponent<BentoSelectionController>();
            if (bentoSelectionController != null)
            {
                // Composition Root: 자식 컨트롤러에 의존 명시 주입
                bentoSelectionController.Inject(session?.UnlockedFood, session?.MenuSelection);
                bentoSelectionController.Close();
            }
        }
        else
        {
            Debug.LogError("[MallSceneController] BentoSelection prefab is not assigned in the inspector!");
        }

        if (dialogueManager == null)
            dialogueManager = Object.FindFirstObjectByType<DialogueManager>();

        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(GoHome);
    }

    /// <summary>Phase 따라 메뉴 선택 modal 또는 phase 종료 확인 다이얼로그 — Canvas Button + GoHomeInteraction 공용.</summary>
    public void GoHome()
    {
        var phase = GameSessionRoot.Instance?.Progress?.PhaseData.Phase ?? PhaseType.Preparation;

        if (phase == PhaseType.Preparation)
        {
            OpenMenuSelection();
        }
        else
        {
            ShowPhaseEndConfirm(phase);
        }
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
            var ps = GameSessionRoot.Instance?.Progress;
            if (ps == null || !ps.PassPhase())
                SceneLoader.LoadScene(SceneNames.Idle);
        });
    }

    private void ShowPhaseEndConfirm(PhaseType phase)
    {
        if (dialogueManager == null)
        {
            Debug.LogError("[MallSceneController] DialogueManager not found!");
            return;
        }

        var so = ScriptableObject.CreateInstance<DialogueSO>();
        so.entries = new List<DialogueEntry>
        {
            new DialogueEntry
            {
                speaker = "",
                text = $"{PhaseToString(phase)}를 끝내시겠습니까?",
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { label = "예",   resultTag = "confirm" },
                    new DialogueChoice { label = "아니요", resultTag = "cancel"  }
                }
            }
        };

        dialogueManager.OnDialogueEnded += OnPhaseEndResult;
        dialogueManager.StartDialogue(so);
    }

    private void OnPhaseEndResult(string tag)
    {
        dialogueManager.OnDialogueEnded -= OnPhaseEndResult;

        if (tag == "confirm")
        {
            var ps = GameSessionRoot.Instance?.Progress;
            if (ps == null || !ps.PassPhase())
                SceneLoader.LoadScene(SceneNames.Idle);
        }
    }

    private static string PhaseToString(PhaseType phase) => phase switch
    {
        PhaseType.Preparation => "영업 준비",
        PhaseType.Morning     => "아침",
        PhaseType.Afternoon   => "점심",
        PhaseType.Evening     => "저녁",
        PhaseType.Night       => "밤",
        _                     => phase.ToString()
    };

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
