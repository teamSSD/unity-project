using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 액션 토글 UI 컴포넌트
/// ActionDatabase와 연동하여 상태 표시 및 액션 실행을 처리합니다.
/// </summary>
public class ActionToggle : MonoBehaviour
{
    [Header("액션 데이터")]
    [Tooltip("이 토글이 나타내는 액션 타입")]
    public ActionType actionType;

    [Header("UI 참조")]
    [Tooltip("토글 컴포넌트")]
    public Toggle toggle;
    
    [Tooltip("설명 텍스트 (예: 가게를 엽니다)")]
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    
    [Tooltip("이름 텍스트 (예: 영업)")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    
    [Tooltip("아이콘 이미지")]
    [SerializeField] private Image iconImage;

    [Header("상태별 색상")]
    [SerializeField] private Color doneColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color disavailableColor = new Color(1f, 1f, 1f, 0.8f);

    private ActionState currentState = ActionState.Disavailable;

    // DiaryModel 참조 (DiaryUIController에서 주입)
    private DiaryModel diaryModel;

    private void Start()
    {
        InitializeUI();
        RegisterToManager();
        RefreshVisual();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void OnEnable()
    {
        if (diaryModel != null)
        {
            diaryModel.OnActionStateChanged += OnModelStateChanged;
        }
    }

    private void OnDisable()
    {
        if (diaryModel != null)
        {
            diaryModel.OnActionStateChanged -= OnModelStateChanged;
        }
    }

    /// <summary>
    /// ActionDatabase로부터 UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        if (actionType == ActionType.None)
        {
            Debug.LogError($"[ActionToggle] [{gameObject.name}] ActionType is None! Please assign a valid ActionType.", this);
            return;
        }

        if (!ActionDatabase.Exists(actionType))
        {
            Debug.LogError($"[ActionToggle] [{gameObject.name}] ActionType.{actionType} not found in ActionDatabase!", this);
            return;
        }

        // ActionDatabase에서 정보 가져오기
        var info = ActionDatabase.GetInfo(actionType);
        currentState = info.InitialState;

        // descriptionLabel이 없으면 자동 찾기
        if (descriptionLabel == null)
        {
            FindUIReferences();
        }

        // UI 텍스트 설정
        if (nameLabel != null)
        {
            nameLabel.text = info.DisplayName;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = info.Description;
        }

        if (iconImage != null && info.Icon != null)
        {
            iconImage.sprite = info.Icon;
        }
    }
    
    /// <summary>
    /// UI 참조 자동 찾기 (프리팹에 미리 추가된 것 사용)
    /// </summary>
    private void FindUIReferences()
    {
        // 기존 Label 오브젝트 찾기
        if (nameLabel == null)
        {
            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null)
            {
                nameLabel = labelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // 기존 DescriptionLabel 찾기 (프리팹에 미리 추가해야 함)
        if (descriptionLabel == null)
        {
            Transform descTransform = transform.Find("DescriptionLabel");
            if (descTransform != null)
            {
                descriptionLabel = descTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning($"[ActionToggle] DescriptionLabel not found on {gameObject.name}. Please assign it in the inspector or ensure a child named 'DescriptionLabel' exists.");
            }
        }
    }

    [Header("진행 정보")]
    [Tooltip("이 액션이 속한 페이즈")]
    public PhaseType phase;

    /// <summary>
    /// 매니저에 등록
    /// </summary>
    /// <summary>
    /// DiaryModel 주입 (DiaryUIController에서 호출)
    /// </summary>
    public void Initialize(DiaryModel model)
    {
        this.diaryModel = model;

        // DiaryModel에 이미 등록되어 있음 (DiaryUIController에서 처리)
        // 현재 상태만 가져오기
        if (actionType != ActionType.None)
        {
            currentState = diaryModel.GetState(phase, actionType);
            RefreshVisual();
        }

        Debug.Log($"[ActionToggle] {actionType} initialized with DiaryModel");
    }

    private void RegisterToManager()
    {
        // DiaryModel이 있으면 이미 DiaryUIController에서 등록했으므로 스킵
        // DiaryModel이 없으면 아무것도 하지 않음
    }

    /// <summary>
    /// DiaryModel 상태 변경 콜백 (NEW)
    /// </summary>
    private void OnModelStateChanged(PhaseType changedPhase, ActionType changedType, ActionState newState)
    {
        if (changedPhase == phase && changedType == actionType)
        {
            currentState = newState;
            RefreshVisual();
            Debug.Log($"[ActionToggle] {actionType} state changed to {newState} via DiaryModel");
        }
    }


    /// <summary>
    /// UI 새로고침
    /// </summary>
    private void RefreshVisual()
    {
        ChangeState(currentState);
    }

    /// <summary>
    /// 상태 변경 및 UI 업데이트
    /// </summary>
    public void ChangeState(ActionState newState)
    {
        currentState = newState;

        if (toggle == null || toggle.targetGraphic == null || !toggle) return;
        
        switch (newState)
        {
            case ActionState.Done:
                // Done: 옅은 색상, 변경 불가
                toggle.interactable = false;
                toggle.targetGraphic.color = doneColor;
                if (descriptionLabel != null) descriptionLabel.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                if (nameLabel != null) nameLabel.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                break;

            case ActionState.Selected:
                // Selected: 노란색 하이라이트, 변경 가능
                toggle.interactable = true;
                toggle.isOn = true;
                toggle.targetGraphic.color = new Color(0.9f, 0.9f, 0.5f);
                if (descriptionLabel != null) descriptionLabel.color = Color.black;
                if (nameLabel != null) nameLabel.color = Color.black;
                break;

            case ActionState.Available:
                // Available: 진한 색상, 변경 가능
                toggle.interactable = true;
                toggle.targetGraphic.color = availableColor;
                if (descriptionLabel != null) descriptionLabel.color = Color.black;
                if (nameLabel != null) nameLabel.color = Color.black;
                break;

            case ActionState.Disavailable:
                // Disavailable: 기본 색상, 변경 불가능
                toggle.interactable = false;
                toggle.targetGraphic.color = disavailableColor;
                if (descriptionLabel != null) descriptionLabel.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                if (nameLabel != null) nameLabel.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                break;
        }
    }

    /// <summary>
    /// 토글 선택 시 호출 (UnityEvent에서 연결)
    /// </summary>
    public void OnToggleValueChanged(bool isOn)
    {
        Debug.Log($"[ActionToggle] OnToggleValueChanged: {nameLabel?.text} ({actionType}), isOn: {isOn}");
        if (!isOn) return;

        bool success = false;

        if (diaryModel != null)
        {
            success = diaryModel.ExecuteAction(phase, actionType);
            Debug.Log($"[ActionToggle] ExecuteAction via DiaryModel: {actionType}, success: {success}");
        }

        if (!success && toggle != null)
        {
            // 실패 시 즉시 토글 원복 (이벤트 발생 없이)
            toggle.SetIsOnWithoutNotify(false);
        }
    }

    [ContextMenu("Test Click")]
    public void TestClick()
    {
        if (toggle != null) toggle.isOn = true;
    }

    /// <summary>
    /// 현재 상태 getter
    /// </summary>
    public ActionState GetCurrentState() => currentState;

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 값 변경 시 자동 검증
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // ActionType 검증
        if (actionType == ActionType.None)
        {
            UnityEditor.EditorGUIUtility.PingObject(this);
            Debug.LogError($"[ActionToggle] [{gameObject.name}] ActionType is None! Please assign a valid ActionType.", this);
        }
        else if (!ActionDatabase.Exists(actionType))
        {
            UnityEditor.EditorGUIUtility.PingObject(this);
            Debug.LogError($"[ActionToggle] [{gameObject.name}] ActionType.{actionType} not found in ActionDatabase!", this);
        }

        // UI 참조 검증
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
            if (toggle == null)
                Debug.LogError($"[ActionToggle] [{gameObject.name}] Toggle component is missing!", this);
        }

        if (nameLabel == null)
            Debug.LogWarning($"[ActionToggle] [{gameObject.name}] nameLabel is not assigned. Text will not display.", this);

        if (descriptionLabel == null)
            Debug.LogWarning($"[ActionToggle] [{gameObject.name}] descriptionLabel is not assigned. Description will not display.", this);
    }

    /// <summary>
    /// 자동으로 자식 오브젝트에서 UI 참조 찾기
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/ActionToggle/Auto Setup References")]
    private static void AutoSetupReferences(UnityEditor.MenuCommand command)
    {
        var actionToggle = command.context as ActionToggle;
        if (actionToggle == null) return;

        UnityEditor.Undo.RecordObject(actionToggle, "Auto Setup ActionToggle");

        // Toggle 자동 찾기
        if (actionToggle.toggle == null)
        {
            actionToggle.toggle = actionToggle.GetComponent<Toggle>();
            if (actionToggle.toggle != null)
                Debug.Log($"[ActionToggle] Auto-assigned Toggle component on {actionToggle.gameObject.name}");
        }

        // Label 자동 찾기
        if (actionToggle.nameLabel == null)
        {
            var labelTransform = actionToggle.transform.Find("Label");
            if (labelTransform != null)
            {
                actionToggle.nameLabel = labelTransform.GetComponent<TextMeshProUGUI>();
                if (actionToggle.nameLabel != null)
                    Debug.Log($"[ActionToggle] Auto-assigned nameLabel on {actionToggle.gameObject.name}");
            }
        }

        // DescriptionLabel 자동 찾기
        if (actionToggle.descriptionLabel == null)
        {
            var descTransform = actionToggle.transform.Find("DescriptionLabel");
            if (descTransform != null)
            {
                actionToggle.descriptionLabel = descTransform.GetComponent<TextMeshProUGUI>();
                if (actionToggle.descriptionLabel != null)
                    Debug.Log($"[ActionToggle] Auto-assigned descriptionLabel on {actionToggle.gameObject.name}");
            }
        }

        // IconImage 자동 찾기 (옵션)
        if (actionToggle.iconImage == null)
        {
            var iconTransform = actionToggle.transform.Find("Icon");
            if (iconTransform != null)
            {
                actionToggle.iconImage = iconTransform.GetComponent<Image>();
                if (actionToggle.iconImage != null)
                    Debug.Log($"[ActionToggle] Auto-assigned iconImage on {actionToggle.gameObject.name}");
            }
        }

        UnityEditor.EditorUtility.SetDirty(actionToggle);
        Debug.Log($"[ActionToggle] Auto-setup completed for {actionToggle.gameObject.name}");
    }
#endif
}
