using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Depricated! <- 나중에 이 파일 삭제할 예정

/// <summary>
/// 영업 선택 UI
/// - 아침/점심/저녁 탭으로 구분
/// - 각 시간대별로 영업/휴식/상가 선택
/// - ActionSelectionManager와 연동
/// </summary>
public class ActionSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private ToggleGroup tabGroup;

    [Header("Tab Toggles")]
    [SerializeField] private Toggle morningTab;
    [SerializeField] private Toggle afternoonTab;
    [SerializeField] private Toggle eveningTab;

    [Header("Action Buttons - Morning")]
    [SerializeField] private Button morningWorkBtn;
    [SerializeField] private Button morningRestBtn;
    [SerializeField] private Button morningShoppingBtn;

    [Header("Action Buttons - Afternoon")]
    [SerializeField] private Button afternoonWorkBtn;
    [SerializeField] private Button afternoonRestBtn;
    [SerializeField] private Button afternoonShoppingBtn;

    [Header("Action Buttons - Evening")]
    [SerializeField] private Button eveningWorkBtn;
    [SerializeField] private Button eveningRestBtn;
    [SerializeField] private Button eveningShoppingBtn;

    [Header("Confirm")]
    [SerializeField] private Button confirmButton;

    private int currentTabIndex = 0; // 0=아침, 1=점심, 2=저녁

    private void Start()
    {
        InitializeUI();
        SetupListeners();
        RefreshUI();
    }

    private void InitializeUI()
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }

        // 탭 초기화
        if (morningTab != null) morningTab.isOn = true;
        currentTabIndex = 0;

        Debug.Log("[ActionSelectionUI] Initialized");
    }

    private void SetupListeners()
    {
        // 탭 리스너
        if (morningTab != null) morningTab.onValueChanged.AddListener((isOn) => { if (isOn) OnTabChanged(0); });
        if (afternoonTab != null) afternoonTab.onValueChanged.AddListener((isOn) => { if (isOn) OnTabChanged(1); });
        if (eveningTab != null) eveningTab.onValueChanged.AddListener((isOn) => { if (isOn) OnTabChanged(2); });

        // 아침 버튼 리스너
        if (morningWorkBtn != null) morningWorkBtn.onClick.AddListener(() => OnActionSelected(0, ActionType.Work));
        if (morningRestBtn != null) morningRestBtn.onClick.AddListener(() => OnActionSelected(0, ActionType.Rest));
        if (morningShoppingBtn != null) morningShoppingBtn.onClick.AddListener(() => OnActionSelected(0, ActionType.Shopping));

        // 점심 버튼 리스너
        if (afternoonWorkBtn != null) afternoonWorkBtn.onClick.AddListener(() => OnActionSelected(1, ActionType.Work));
        if (afternoonRestBtn != null) afternoonRestBtn.onClick.AddListener(() => OnActionSelected(1, ActionType.Rest));
        if (afternoonShoppingBtn != null) afternoonShoppingBtn.onClick.AddListener(() => OnActionSelected(1, ActionType.Shopping));

        // 저녁 버튼 리스너
        if (eveningWorkBtn != null) eveningWorkBtn.onClick.AddListener(() => OnActionSelected(2, ActionType.Work));
        if (eveningRestBtn != null) eveningRestBtn.onClick.AddListener(() => OnActionSelected(2, ActionType.Rest));
        if (eveningShoppingBtn != null) eveningShoppingBtn.onClick.AddListener(() => OnActionSelected(2, ActionType.Shopping));

        // 확인 버튼
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnTabChanged(int tabIndex)
    {
        currentTabIndex = tabIndex;
        RefreshUI();
        Debug.Log($"[ActionSelectionUI] Tab changed to: {tabIndex}");
    }

    private void OnActionSelected(int timeIndex, ActionType actionType)
    {
        if (ActionSelectionManager.Instance == null)
        {
            Debug.LogError("[ActionSelectionUI] ActionSelectionManager not found!");
            return;
        }

        ActionSelectionManager.Instance.SetAction(timeIndex, actionType);
        RefreshUI();

        Debug.Log($"[ActionSelectionUI] Action selected: {actionType} for time index {timeIndex}");
    }

    private void RefreshUI()
    {
        if (ActionSelectionManager.Instance == null) return;

        // 각 시간대별 선택 상태 업데이트
        UpdateButtonState(0, morningWorkBtn, morningRestBtn, morningShoppingBtn);
        UpdateButtonState(1, afternoonWorkBtn, afternoonRestBtn, afternoonShoppingBtn);
        UpdateButtonState(2, eveningWorkBtn, eveningRestBtn, eveningShoppingBtn);

        // 확인 버튼 활성화 (현재 탭에 선택이 있을 때만)
        if (confirmButton != null)
        {
            var currentAction = ActionSelectionManager.Instance.GetAction(currentTabIndex);
            confirmButton.interactable = (currentAction != null && currentAction.HasSelection());
        }
    }

    private void UpdateButtonState(int timeIndex, Button workBtn, Button restBtn, Button shoppingBtn)
    {
        var action = ActionSelectionManager.Instance.GetAction(timeIndex);
        if (action == null) return;

        // 선택 상태 표시 (버튼 색상 변경)
        Color selectedColor = new Color(0.9f, 0.9f, 0.5f); // 노란색
        Color normalColor = Color.white;

        if (workBtn != null)
        {
            var colors = workBtn.colors;
            colors.normalColor = (action.SelectedAction == ActionType.Work) ? selectedColor : normalColor;
            workBtn.colors = colors;
        }

        if (restBtn != null)
        {
            var colors = restBtn.colors;
            colors.normalColor = (action.SelectedAction == ActionType.Rest) ? selectedColor : normalColor;
            restBtn.colors = colors;
        }

        if (shoppingBtn != null)
        {
            var colors = shoppingBtn.colors;
            colors.normalColor = (action.SelectedAction == ActionType.Shopping) ? selectedColor : normalColor;
            shoppingBtn.colors = colors;
        }
    }

    private void OnConfirm()
    {
        Debug.Log("[ActionSelectionUI] Confirm button clicked");

        if (ActionSelectionManager.Instance == null) return;

        // 현재 탭의 선택 확인
        var currentAction = ActionSelectionManager.Instance.GetAction(currentTabIndex);
        if (currentAction == null || !currentAction.HasSelection())
        {
            Debug.LogWarning($"[ActionSelectionUI] No action selected for current tab {currentTabIndex}!");
            return;
        }

        // UI 닫기
        Close();

        // 현재 탭의 액션 실행
        ExecuteCurrentAction();
    }

    private void ExecuteCurrentAction()
    {
        if (ActionSelectionManager.Instance == null) return;

        var action = ActionSelectionManager.Instance.GetAction(currentTabIndex);
        if (action == null || !action.HasSelection()) return;

        Debug.Log($"[ActionSelectionUI] Executing action: {action.SelectedAction} for tab {currentTabIndex}");

        // 진행 상태 저장
        if (ProgressSystem.instance != null)
        {
            ProgressSystem.instance.flush();
        }
        StatsSystem.flush();

        // 액션에 따라 분기
        switch (action.SelectedAction)
        {
            case ActionType.Work:
                // 영업 - Cooking 씬으로 전환
                SceneManager.LoadScene("Cooking");
                break;

            case ActionType.Rest:
                // 휴식 - 스태미너 충전 후 페이즈 진행
                StatsSystem.SetStamina(100);
                if (ProgressSystem.instance != null)
                {
                    ProgressSystem.instance.PassPhase();
                    ProgressSystem.instance.flush();
                }
                Debug.Log("[ActionSelectionUI] Rested - stamina restored, phase advanced");
                break;

            case ActionType.Shopping:
                // 상가 이동 - Scene_Mall으로 전환
                SceneManager.LoadScene("Scene_Mall");
                break;
        }
    }

    /// <summary>
    /// UI 열기
    /// </summary>
    public void Open()
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(true);
        }

        RefreshUI();
        Debug.Log("[ActionSelectionUI] Opened");
    }

    /// <summary>
    /// UI 닫기
    /// </summary>
    public void Close()
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }

        Debug.Log("[ActionSelectionUI] Closed");
    }
}
