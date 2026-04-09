using System;
using UnityEngine;

/// <summary>
/// Manages tooltip display behavior for hoverable objects
/// Separates UI presentation logic from game model logic
/// </summary>
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(ClickStateUtil))]
public class TooltipController : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [SerializeField] private float hoverThreshold = 1.0f;

    private HoverStateUtil hoverStateUtil;
    private ClickStateUtil clickStateUtil;
    private GameObject tooltipObject;
    private float hoverClock = 0f;
    private bool ownsTooltip = false; // 현재 이 컨트롤러가 툴팁을 소유하고 있는지

    private Action updateTooltipContent;

    /// <summary>
    /// Register a callback to update tooltip content when displayed
    /// </summary>
    public void RegisterContentUpdater(Action updater)
    {
        updateTooltipContent = updater;
    }

    /// <summary>
    /// Get the instantiated tooltip GameObject
    /// </summary>
    public GameObject GetTooltipObject() => tooltipObject;

    /// <summary>
    /// Position tooltip in world space with offset
    /// </summary>
    public void PositionTooltip(Vector3 worldOffset)
    {
        if (tooltipObject == null) return;
        tooltipObject.transform.position = transform.position + worldOffset;
    }

    void Awake()
    {
        hoverStateUtil = GetComponent<HoverStateUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        // 글로벌 툴팁 참조 (타입별 분기)
        if (UIManager.Instance == null)
        {
            Debug.LogWarning($"[TooltipController] {gameObject.name}: UIManager.Instance is null! Cannot get global tooltip.");
            return;
        }

        if (GetComponent<FoodModel>() != null)
        {
            tooltipObject = UIManager.Instance.IngredientTooltip;
        }
        else if (GetComponent<CookingToolModel>() != null)
        {
            tooltipObject = UIManager.Instance.CookingToolTooltip;
        }

        if (tooltipObject == null)
        {
            Debug.LogError($"[TooltipController] {gameObject.name}: Failed to get global tooltip!");
        }
    }

    void Start()
    {
        // 글로벌 툴팁 사용 - Instantiate 불필요
        // updateTooltipContent는 첫 표시 시 Update()에서 호출됨
    }

    void Update()
    {
        if (tooltipObject == null) return;

        // Show tooltip after hovering for threshold duration
        if (clickStateUtil != null && clickStateUtil.getState() == ClickState.None &&
            hoverStateUtil != null && hoverStateUtil.IsHovering())
        {
            if (hoverClock < hoverThreshold && hoverClock + Time.deltaTime >= hoverThreshold)
            {
                // 툴팁 표시 전 소유권 획득
                if (!tooltipObject.activeSelf || ownsTooltip)
                {
                    ownsTooltip = true; // 소유권 획득
                    tooltipObject.SetActive(true);
                    updateTooltipContent?.Invoke();
                }
            }
            hoverClock += Time.deltaTime;
            return;
        }

        // Hide tooltip when not hovering - 소유자만 숨길 수 있음
        if (ownsTooltip)
        {
            hoverClock = 0;
            ownsTooltip = false; // 소유권 해제
            tooltipObject.SetActive(false);
        }
        else
        {
            hoverClock = 0; // 시계만 리셋
        }
    }

    void OnDestroy()
    {
        // 글로벌 툴팁은 파괴하지 않음 (UIManager가 관리)
        // 소유자만 숨기기
        if (tooltipObject != null && ownsTooltip)
        {
            tooltipObject.SetActive(false);
            ownsTooltip = false;
        }
    }
}
