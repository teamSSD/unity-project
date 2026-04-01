using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 글로벌 UI 관리자 (Singleton)
/// - 씬 전환 시에도 유지되는 글로벌 Canvas 제공
/// - 모든 UI 요소가 이 Canvas를 사용
/// - DontDestroyOnLoad로 영구 유지
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    private static bool initialized = false;

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UIManager");
                instance = go.AddComponent<UIManager>();
            }

            // 최초 접근 시 자동 초기화
            if (!initialized && instance != null)
            {
                instance.Initialize();
                initialized = true;
            }

            return instance;
        }
    }

    public Canvas GlobalCanvas { get; private set; }
    public GameObject IngredientTooltip { get; private set; }
    public GameObject CookingToolTooltip { get; private set; }

    void Awake()
    {
        // Singleton 패턴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize()에서 리소스 로드 수행 (지연 로딩)
        Debug.Log("[UIManager] Awake completed");
    }

    /// <summary>
    /// 리소스 로딩 및 초기화 (게임 시작 시 한 번만 호출)
    /// </summary>
    public void Initialize()
    {
        // 글로벌 Canvas 생성
        CreateGlobalCanvas();

        // 글로벌 툴팁 생성
        CreateGlobalTooltips();

        Debug.Log("[UIManager] Initialized with GlobalCanvas and Tooltips");
    }

    private void CreateGlobalCanvas()
    {
        GameObject canvasObj = new GameObject("GlobalCanvas");
        canvasObj.transform.SetParent(transform);

        // Canvas 컴포넌트
        GlobalCanvas = canvasObj.AddComponent<Canvas>();
        GlobalCanvas.renderMode = RenderMode.WorldSpace;
        GlobalCanvas.worldCamera = Camera.main;
        GlobalCanvas.sortingLayerName = "UI";
        GlobalCanvas.sortingOrder = 100; // 항상 최상위

        // WorldSpace Canvas Scale
        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 1f);

        // GraphicRaycaster - UI 클릭 감지
        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("[UIManager] GlobalCanvas created (WorldSpace)");
    }

    private void CreateGlobalTooltips()
    {
        // Ingredient 툴팁 (재료/음식용) - 1개만
        GameObject ingredientPrefab = Resources.Load<GameObject>("Prefabs/cooking/IngredientDescription");
        if (ingredientPrefab != null)
        {
            IngredientTooltip = Instantiate(ingredientPrefab, GlobalCanvas.transform);
            IngredientTooltip.name = "GlobalIngredientTooltip";
            IngredientTooltip.SetActive(false);
            Debug.Log("[UIManager] IngredientTooltip created");
        }
        else
        {
            Debug.LogError("[UIManager] Failed to load IngredientDescription prefab");
        }

        // CookingTool 툴팁 (조리 도구용) - 1개만
        GameObject toolPrefab = Resources.Load<GameObject>("Prefabs/cooking/CookingToolDescription");
        if (toolPrefab != null)
        {
            CookingToolTooltip = Instantiate(toolPrefab, GlobalCanvas.transform);
            CookingToolTooltip.name = "GlobalCookingToolTooltip";
            CookingToolTooltip.SetActive(false);
            Debug.Log("[UIManager] CookingToolTooltip created");
        }
        else
        {
            Debug.LogError("[UIManager] Failed to load CookingToolDescription prefab");
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
