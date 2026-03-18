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
    [SerializeField] private GameObject tooltipPrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float hoverThreshold = 1.0f;

    private HoverStateUtil hoverStateUtil;
    private ClickStateUtil clickStateUtil;
    private GameObject tooltipObject;
    private float hoverClock = 0f;

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
    /// Calculate screen position for tooltip with world space offset
    /// </summary>
    public Vector3 CalculateTooltipPosition(Vector3 worldOffset)
    {
        return Camera.main.WorldToScreenPoint(transform.position + worldOffset);
    }

    void Awake()
    {
        hoverStateUtil = GetComponent<HoverStateUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();
    }

    void Start()
    {
        if (tooltipPrefab != null && canvas != null)
        {
            tooltipObject = Instantiate(tooltipPrefab, canvas.transform);
            updateTooltipContent?.Invoke();
            tooltipObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[TooltipController] {gameObject.name}: tooltipPrefab or canvas is null!");
        }
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
                tooltipObject.SetActive(true);
                updateTooltipContent?.Invoke();
            }
            hoverClock += Time.deltaTime;
            return;
        }

        // Hide tooltip when not hovering
        hoverClock = 0;
        tooltipObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (tooltipObject != null)
        {
            Destroy(tooltipObject);
        }
    }
}
