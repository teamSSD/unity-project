using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// Diary UI의 Composition Root
/// DiaryModel 생성 및 모든 UI 컴포넌트에 주입
///
/// 역할:
/// - DiaryModel 생성 및 생명주기 관리
/// - UI 컴포넌트에 Model 주입 (Dependency Injection)
/// - ProgressSystem과 DiaryModel 동기화
/// - DontDestroyOnLoad로 씬 이동 시 상태 유지
/// - IUnlockedFoodProvider 구현 (해금된 음식 제공)
///
/// DiaryFlowManager를 대체합니다.
/// </summary>
public class DiaryUIController : MonoBehaviour, IUnlockedFoodProvider
{
    // ────────────────────────────────────────────────────────────
    // 싱글톤
    // ────────────────────────────────────────────────────────────

    private static DiaryUIController instance;
    public static DiaryUIController Instance => instance;

    // ────────────────────────────────────────────────────────────
    // Inspector 설정
    // ────────────────────────────────────────────────────────────

    [Header("UI Components")]
    [SerializeField] private BentoToggleList bentoToggleList;
    [SerializeField] private TextMeshProUGUI phaseLabelText;

    [Header("Settings")]
    [Tooltip("씬 이동 시 Diary 상태 유지 여부")]
    [SerializeField] private bool persistAcrossScenes = true;

    // ────────────────────────────────────────────────────────────
    // Core
    // ────────────────────────────────────────────────────────────

    private DiaryModel model;
    private IPhaseProgressor phaseProgressor;
    private bool isInitialized = false;

    // ────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// DiaryModel에 접근 (읽기 전용)
    /// </summary>
    public DiaryModel Model => model;

    // ────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 싱글톤 패턴 (씬 이동 시 파괴 방지)
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[DiaryUIController] Duplicate instance found. Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[DiaryUIController] DontDestroyOnLoad enabled - Diary state will persist across scenes");
        }

        // DiaryModel 생성
        CreateModel();

        Debug.Log("[DiaryUIController] Awake complete - Model created");
    }

    private void Start()
    {
        // 초기화 (yield return null 제거 - 명확한 순서 보장)
        Initialize();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (phaseProgressor != null && model != null)
        {
            if (ProgressSystem.instance != null)
            {
                ProgressSystem.instance.OnPhaseChanged -= model.SyncPhase;
            }

            model.OnPhaseChanged -= UpdatePhaseLabel;
        }

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("[DiaryUIController] Destroyed");
    }

    // ────────────────────────────────────────────────────────────
    // Initialization
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// DiaryModel 생성 (Awake에서 호출)
    /// </summary>
    private void CreateModel()
    {
        // IPhaseProgressor 어댑터 생성
        phaseProgressor = new ProgressSystemAdapter();

        // DiaryModel 생성
        model = new DiaryModel(phaseProgressor);

        Debug.Log("[DiaryUIController] DiaryModel created");
    }

    /// <summary>
    /// UI 초기화 및 Model 주입 (Start에서 호출)
    /// </summary>
    private void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning("[DiaryUIController] Already initialized");
            return;
        }

        Debug.Log("[DiaryUIController] Initializing UI components...");

        // 1. BentoToggleList 초기화
        if (bentoToggleList != null)
        {
            bentoToggleList.Initialize(model);
            Debug.Log("[DiaryUIController] BentoToggleList initialized");
        }
        else
        {
            Debug.LogWarning("[DiaryUIController] BentoToggleList not assigned in Inspector");
        }

        // 1-1. MenuToggleList 초기화 (Phase 2.3: IBentoToggle 주입)
        InitializeMenuToggleLists();

        // 2. ActionToggle 초기화 (씬의 모든 ActionToggle 찾기)
        InitializeActionToggles();

        // 2-1. PhaseRowVisual 초기화 (페이즈 행 시각 효과)
        InitializePhaseRowVisuals();

        // 3. ProgressSystem 연동
        ConnectToProgressSystem();

        // 4. 현재 페이즈 동기화
        SyncCurrentPhase();

        // 5. Phase 변경 이벤트 구독 (날짜 표시 업데이트)
        if (model != null)
        {
            model.OnPhaseChanged += UpdatePhaseLabel;
        }

        // 6. 초기 날짜 표시 업데이트
        UpdatePhaseLabel(model.CurrentPhase);

        isInitialized = true;
        Debug.Log("[DiaryUIController] Initialization complete");
    }

    /// <summary>
    /// 씬의 모든 MenuToggleList를 찾아서 의존성 주입 (Phase 2.3)
    /// </summary>
    private void InitializeMenuToggleLists()
    {
        var menuToggleLists = FindObjectsOfType<MenuToggleList>(true); // includeInactive = true

        if (menuToggleLists.Length == 0)
        {
            Debug.LogWarning("[DiaryUIController] No MenuToggleList found in scene");
            return;
        }

        Debug.Log($"[DiaryUIController] Found {menuToggleLists.Length} MenuToggleLists");

        foreach (var menuToggleList in menuToggleLists)
        {
            // Phase 4.1: DiaryModel 의존성 주입 추가
            menuToggleList.Initialize(this, bentoToggleList, model);
            Debug.Log($"[DiaryUIController] Initialized MenuToggleList ({menuToggleList.menuType}) with DiaryModel");
        }
    }

    /// <summary>
    /// 씬의 모든 ActionToggle을 찾아서 Model 주입
    /// </summary>
    private void InitializeActionToggles()
    {
        var actionToggles = FindObjectsOfType<ActionToggle>(true); // includeInactive = true

        if (actionToggles.Length == 0)
        {
            Debug.LogWarning("[DiaryUIController] No ActionToggle found in scene");
            return;
        }

        Debug.Log($"[DiaryUIController] Found {actionToggles.Length} ActionToggles");

        foreach (var toggle in actionToggles)
        {
            if (toggle.actionType == ActionType.None)
            {
                continue;
            }

            // DiaryModel에 액션 등록
            model.RegisterAction(toggle.phase, toggle.actionType);

            // ActionToggle에 DiaryModel 주입
            toggle.Initialize(model);

            Debug.Log($"[DiaryUIController] Initialized action: {toggle.phase} - {toggle.actionType}");
        }
    }

    /// <summary>
    /// 씬의 모든 PhaseRowVisual을 찾아서 Model 주입
    /// </summary>
    private void InitializePhaseRowVisuals()
    {
        var phaseRowVisuals = FindObjectsOfType<PhaseRowVisual>(true); // includeInactive = true

        if (phaseRowVisuals.Length == 0)
        {
            Debug.LogWarning("[DiaryUIController] No PhaseRowVisual found in scene");
            return;
        }

        Debug.Log($"[DiaryUIController] Found {phaseRowVisuals.Length} PhaseRowVisuals");

        foreach (var phaseRow in phaseRowVisuals)
        {
            phaseRow.Initialize(model);
            Debug.Log($"[DiaryUIController] Initialized PhaseRowVisual for phase: {phaseRow.phase}");
        }
    }

    /// <summary>
    /// ProgressSystem과 DiaryModel 연결
    /// </summary>
    private void ConnectToProgressSystem()
    {
        if (ProgressSystem.instance == null)
        {
            Debug.LogWarning("[DiaryUIController] ProgressSystem.instance is null - cannot connect");
            return;
        }

        // ProgressSystem의 OnPhaseChanged 이벤트를 DiaryModel.SyncPhase에 연결
        ProgressSystem.instance.OnPhaseChanged += model.SyncPhase;

        Debug.Log("[DiaryUIController] Connected to ProgressSystem.OnPhaseChanged");
    }

    /// <summary>
    /// 현재 페이즈 동기화 (초기화 시 호출)
    /// </summary>
    private void SyncCurrentPhase()
    {
        if (ProgressSystem.instance?.phaseData == null)
        {
            Debug.LogWarning("[DiaryUIController] ProgressSystem.phaseData is null - using default Preparation phase");
            model.SyncPhase(PhaseType.Preparation);
            return;
        }

        var currentPhase = ProgressSystem.instance.phaseData.Phase;
        model.SyncPhase(currentPhase);

        Debug.Log($"[DiaryUIController] Synced to current phase: {currentPhase}");
    }

    /// <summary>
    /// Phase Label 업데이트 (D+날짜)
    /// </summary>
    private void UpdatePhaseLabel(PhaseType phase)
    {
        if (phaseLabelText == null) return;

        int day = StatsSystem.GetDay();
        phaseLabelText.text = $"D+{day}";
    }

    /// <summary>
    /// Phase 타입에 해당하는 한글 이름 반환
    /// </summary>
    private string GetPhaseName(PhaseType phase)
    {
        switch (phase)
        {
            case PhaseType.Preparation:
                return "영업준비";
            case PhaseType.Morning:
                return "아침";
            case PhaseType.Afternoon:
                return "점심";
            case PhaseType.Evening:
                return "저녁";
            case PhaseType.Night:
                return "밤";
            default:
                return phase.ToString();
        }
    }

    // ────────────────────────────────────────────────────────────
    // Public Methods (기존 DiaryFlowManager API 호환)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 수동으로 초기화 (테스트용 또는 늦은 초기화)
    /// </summary>
    public void ManualInitialize()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// DiaryModel 상태 스냅샷 가져오기 (저장용)
    /// </summary>
    public DiaryStateSnapshot GetStateSnapshot()
    {
        return model?.CreateSnapshot();
    }

    /// <summary>
    /// DiaryModel 상태 복원 (로드용)
    /// </summary>
    public void RestoreStateSnapshot(DiaryStateSnapshot snapshot)
    {
        if (model == null)
        {
            Debug.LogError("[DiaryUIController] Model is null - cannot restore snapshot");
            return;
        }

        model.RestoreSnapshot(snapshot);
        Debug.Log("[DiaryUIController] State snapshot restored");
    }

    /// <summary>
    /// 현재 페이즈 강제 동기화 (디버깅용)
    /// </summary>
    public void ForceSyncPhase(PhaseType phase)
    {
        model?.SyncPhase(phase);
    }

    // ────────────────────────────────────────────────────────────
    // IUnlockedFoodProvider Implementation
    // ────────────────────────────────────────────────────────────

    private FoodData[] allFoods;

    private void LoadAllFoods()
    {
        if (allFoods == null)
        {
            allFoods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
            Debug.Log($"[DiaryUIController] Loaded {allFoods.Length} FoodData assets");
        }
    }

    /// <summary>
    /// 해금된 메인 메뉴 목록 (현재는 모든 메인 메뉴 반환)
    /// </summary>
    public List<FoodData> GetUnlockedMainFoods()
    {
        LoadAllFoods();
        return allFoods.Where(f => f.type == FoodType.MAIN).ToList();
    }

    /// <summary>
    /// 해금된 사이드 메뉴 목록 (현재는 모든 사이드 메뉴 반환)
    /// </summary>
    public List<FoodData> GetUnlockedSideFoods()
    {
        LoadAllFoods();
        return allFoods.Where(f => f.type == FoodType.SIDE).ToList();
    }

    /// <summary>
    /// 특정 음식이 해금되었는지 확인 (현재는 항상 true)
    /// </summary>
    public bool IsUnlocked(string foodId)
    {
        // TODO: 실제 해금 시스템 구현 필요
        return true;
    }

    // ────────────────────────────────────────────────────────────
    // Debug
    // ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug: Print Current State")]
    private void DebugPrintCurrentState()
    {
        if (model == null)
        {
            Debug.Log("[DiaryUIController] Model is null");
            return;
        }

        Debug.Log($"[DiaryUIController] Current Phase: {model.CurrentPhase}");
        Debug.Log($"[DiaryUIController] Bento Selections: {(model.HasAnyBentoSelection() ? "Yes" : "No")}");
        Debug.Log($"[DiaryUIController] Initialized: {isInitialized}");
    }

    [ContextMenu("Debug: Force Sync to Morning")]
    private void DebugForceMorning()
    {
        ForceSyncPhase(PhaseType.Morning);
        Debug.Log("[DiaryUIController] Force synced to Morning phase");
    }

    private void OnValidate()
    {
        // Inspector에서 BentoToggleList 자동 찾기
        if (bentoToggleList == null)
        {
            bentoToggleList = FindObjectOfType<BentoToggleList>();
            if (bentoToggleList != null)
            {
                Debug.Log("[DiaryUIController] Auto-found BentoToggleList");
            }
        }
    }
#endif
}
