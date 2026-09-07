using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mall 씬의 페이즈별 Work/Rest/Shopping 액션 선택 UI (Idle 씬 흡수 후).
/// MallSceneController가 prefab 인스턴스화 → GoHome 상호작용 시 Show().
/// </summary>
public class PhaseActionSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI phaseTypeText;

    [Header("Action Buttons")]
    [SerializeField] private Button workButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button shoppingButton;

    [Header("Button Texts")]
    [SerializeField] private TextMeshProUGUI workTitle;
    [SerializeField] private TextMeshProUGUI workDescription;
    [SerializeField] private TextMeshProUGUI restTitle;
    [SerializeField] private TextMeshProUGUI restDescription;
    [SerializeField] private TextMeshProUGUI shoppingTitle;
    [SerializeField] private TextMeshProUGUI shoppingDescription;

    private Dictionary<ActionType, Action> actionExecutors;

    /// <summary>선택된 액션 후 호출 — 호출자가 UI 정리(Hide)나 씬 전환 처리.</summary>
    public event Action<ActionType> OnActionExecuted;

    private void Awake()
    {
        InitializeActionExecutors();
        SetupButtons();
    }

    private void InitializeActionExecutors()
    {
        actionExecutors = new Dictionary<ActionType, Action>
        {
            [ActionType.Work] = () => SceneLoader.LoadScene(SceneNames.Cooking),
            [ActionType.Rest] = () =>
            {
                var progress = GameSessionRoot.Instance?.Progress;
                var stats    = GameSessionRoot.Instance?.Stats;

                // Night → Settlement 씬 이동. Blackout으로 감싸면 LoadingManager.isLoading이 걸려
                // 내부 LoadScene 호출이 무시됨(재귀 락). LoadScene 자체가 fade 처리하므로 우회.
                if (progress?.PhaseData?.Phase == PhaseType.Night)
                {
                    stats?.SetStamina(100);
                    progress.PassPhase();
                    return;
                }

                LoadingManager.Instance?.Blackout(
                    midAction: () =>
                    {
                        stats?.SetStamina(100);
                        progress?.PassPhase();
                    },
                    onComplete: UpdateUI);
            },
            [ActionType.Shopping] = () =>
            {
                // Mall에 이미 있으므로 UI만 닫음 (MallSceneController가 Hide 처리).
            }
        };
    }

    private void SetupButtons()
    {
        workButton?.onClick.AddListener(() => OnActionClicked(ActionType.Work));
        restButton?.onClick.AddListener(() => OnActionClicked(ActionType.Rest));
        shoppingButton?.onClick.AddListener(() => OnActionClicked(ActionType.Shopping));

        if (workTitle != null) workTitle.text = "영업";
        if (workDescription != null) workDescription.text = "가게를 엽니다.";
        if (restTitle != null) restTitle.text = "휴식";
        if (restDescription != null) restDescription.text = "스태미너를 충전합니다.";
        if (shoppingTitle != null) shoppingTitle.text = "상가 이동";
        if (shoppingDescription != null) shoppingDescription.text = "상가로 이동합니다.";
    }

    public void Show()
    {
        UpdateUI();
        // 튜토리얼이 disable한 상태가 남아있을 수 있어 매 Show마다 default(활성) reset.
        if (workButton != null) workButton.interactable = true;
        if (restButton != null) restButton.interactable = true;
        if (shoppingButton != null) shoppingButton.interactable = true;
        // 프리팹 세팅에 관계없이 부모 캔버스 중앙 강제 배치.
        // (일부 화면비/해상도에서 root RectTransform anchor가 어긋나 화면 밖으로 밀리는 케이스 방어.)
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        gameObject.SetActive(true);
        UILockManager.Lock(UILockManager.Owner.PhaseSelection);

        // EventSystem 자동 선택(Submit=Space가 선택된 버튼을 클릭시킴) 방지.
        // 튜토리얼 진행 중 Space 입력이 Shopping을 실행하는 문제 회피.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>튜토리얼용. 특정 액션 버튼 활성/비활성 (Shopping만 유도할 때 등).
    /// Show() 다음에 호출. 다음 Show 시 자동 reset.</summary>
    public void SetActionEnabled(ActionType type, bool enabled)
    {
        Button b = type switch
        {
            ActionType.Work => workButton,
            ActionType.Rest => restButton,
            ActionType.Shopping => shoppingButton,
            _ => null,
        };
        if (b != null) b.interactable = enabled;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.PhaseSelection);
    }

    // ESC 닫기 제거: 페이즈 선택은 필수 액션이라 취소 없이 반드시 하나 골라야 함.
    // (이전엔 ESC로 Hide 가능 → UILock만 풀리고 유저가 다음 행동 없이 진행 불가 상태로 인식.)

    private void UpdateUI()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        PhaseType currentPhase = progress?.PhaseData?.Phase ?? PhaseType.Morning;
        if (phaseTypeText != null)
            phaseTypeText.text = $"{GetPhaseText(currentPhase)} 페이즈 선택";
    }

    private static string GetPhaseText(PhaseType phase) => phase switch
    {
        PhaseType.Preparation => "준비",
        PhaseType.Morning     => "아침",
        PhaseType.Afternoon   => "점심",
        PhaseType.Evening     => "저녁",
        PhaseType.Night       => "밤",
        _                     => "아침",
    };

    private void OnActionClicked(ActionType actionType)
    {
        actionExecutors[actionType]?.Invoke();
        OnActionExecuted?.Invoke(actionType);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
