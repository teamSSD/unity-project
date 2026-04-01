using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Idle 씬의 페이즈별 Work/Rest/Shopping 액션 선택 UI.
/// 씬 전환은 IdleSceneController.TransitionToScene()을 통해 수행.
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

    private int currentPhaseIndex = 0;

    private Dictionary<ActionType, Action> actionExecutors;
    private IdleSceneController idleSceneController;

    private void Start()
    {
        idleSceneController = FindObjectOfType<IdleSceneController>();
        InitializeActionExecutors();
        SetupButtons();
        UpdateUI();
    }

    private void InitializeActionExecutors()
    {
        actionExecutors = new Dictionary<ActionType, Action>
        {
            [ActionType.Work] = () => TransitionScene("Cooking"),
            [ActionType.Rest] = () => {
                StatsSystem.SetStamina(100);
                ProgressSystem.instance?.PassPhase();
                UpdateUI();
            },
            [ActionType.Shopping] = () => TransitionScene("Scene_Mall")
        };
    }

    private void TransitionScene(string sceneName)
    {
        if (idleSceneController != null)
            idleSceneController.TransitionToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
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

    private void UpdateUI()
    {
        if (ProgressSystem.instance != null && ProgressSystem.instance.phaseData != null)
        {
            PhaseType currentPhase = ProgressSystem.instance.phaseData.Phase;

            if (phaseTypeText != null)
            {
                phaseTypeText.text = GetPhaseText(currentPhase);
            }

            if (currentPhase == PhaseType.Morning)
                currentPhaseIndex = 0;
            else if (currentPhase == PhaseType.Afternoon)
                currentPhaseIndex = 1;
            else if (currentPhase == PhaseType.Evening)
                currentPhaseIndex = 2;
        }
        else
        {
            if (phaseTypeText != null)
                phaseTypeText.text = "아침";
            currentPhaseIndex = 0;
        }
    }

    private string GetPhaseText(PhaseType phase)
    {
        switch (phase)
        {
            case PhaseType.Preparation:
                return "준비";
            case PhaseType.Morning:
                return "아침";
            case PhaseType.Afternoon:
                return "점심";
            case PhaseType.Evening:
                return "저녁";
            case PhaseType.Night:
                return "밤";
            default:
                return "아침";
        }
    }

    private void OnActionClicked(ActionType actionType)
    {
        ActionSelectionManager.Instance?.SetAction(currentPhaseIndex, actionType);
        actionExecutors[actionType]?.Invoke();
    }

    /// <summary>
    /// 외부에서 UI 업데이트 요청 시 호출
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }
}
