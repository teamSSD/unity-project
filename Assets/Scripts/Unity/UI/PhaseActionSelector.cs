using System;
using System.Collections.Generic;
using UnityEngine;
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
        gameObject.SetActive(true);
        UILockManager.Lock(UILockManager.Owner.PhaseSelection);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.PhaseSelection);
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

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
