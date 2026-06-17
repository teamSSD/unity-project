using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 글로벌 UI 관리자 (Singleton)
/// - 씬 전환 시에도 유지되는 글로벌 Canvas 제공
/// - 모든 UI 요소가 이 Canvas를 사용
/// - DontDestroyOnLoad로 영구 유지
/// - Initialize()는 Camera.main 의존 → Cooking 씬 진입 시 lazy init
/// </summary>
public class UIManager : SingletonMonoBehaviour<UIManager>
{
    private bool isInitialized = false;

    public Canvas GlobalCanvas { get; private set; }

    private GameObject _tooltip;

    public GameObject Tooltip
    {
        get
        {
            EnsureInitialized();
            return _tooltip;
        }
    }

    protected override void OnSingletonAwake()
    {
        Debug.Log("[UIManager] Awake completed");
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
            Initialize();
    }

    /// <summary>
    /// 리소스 로딩 및 초기화 (Camera.main 필요 → Cooking 씬에서 호출)
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;

        CreateGlobalCanvas();
        CreateGlobalTooltips();
        isInitialized = true;

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
        // 재료/요리도구 공용 툴팁 - 1개만
        GameObject prefab = CatalogProvider.Prefabs?.cookingToolDescription;
        if (prefab != null)
        {
            _tooltip = Instantiate(prefab, GlobalCanvas.transform);
            _tooltip.name = "GlobalTooltip";
            _tooltip.SetActive(false);
        }
        else
        {
            Debug.LogError("[UIManager] Failed to load tooltip prefab");
        }
    }
}
