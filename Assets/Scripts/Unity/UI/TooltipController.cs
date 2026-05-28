using System;
using UnityEngine;
using UnityEngine.UI;

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
    private bool ownsTooltip = false;

    private Action updateTooltipContent;

    private Bounds pendingBounds;
    private bool pendingForceRight;
    private bool hasPendingPosition;

    public void RegisterContentUpdater(Action updater)
    {
        updateTooltipContent = updater;
    }

    public GameObject GetTooltipObject() => tooltipObject;

    public void RequestPositionNear(Bounds targetBounds, bool forceRight = false)
    {
        pendingBounds = targetBounds;
        pendingForceRight = forceRight;
        hasPendingPosition = true;
    }

    void Awake()
    {
        hoverStateUtil = GetComponent<HoverStateUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        if (UIManager.Instance == null)
        {
            Debug.LogWarning($"[TooltipController] {gameObject.name}: UIManager.Instance is null! Cannot get global tooltip.");
            return;
        }

        if (GetComponent<FoodModel>() != null)
            tooltipObject = UIManager.Instance.IngredientTooltip;
        else if (GetComponent<CookingToolModel>() != null)
            tooltipObject = UIManager.Instance.CookingToolTooltip;

        if (tooltipObject == null)
            Debug.LogError($"[TooltipController] {gameObject.name}: Failed to get global tooltip!");
    }

    void Update()
    {
        if (tooltipObject == null) return;

        if (clickStateUtil != null && clickStateUtil.getState() == ClickState.None &&
            hoverStateUtil != null && hoverStateUtil.IsHovering())
        {
            if (hoverClock < hoverThreshold && hoverClock + Time.deltaTime >= hoverThreshold)
            {
                if (!tooltipObject.activeSelf || ownsTooltip)
                {
                    ownsTooltip = true;
                    tooltipObject.SetActive(true);
                    updateTooltipContent?.Invoke();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipObject.GetComponent<RectTransform>());

                    if (hasPendingPosition)
                    {
                        hasPendingPosition = false;
                        var rt = tooltipObject.GetComponent<RectTransform>();
                        float scale = tooltipObject.transform.lossyScale.x;
                        Vector2 worldSize = rt.rect.size * scale;
                        tooltipObject.transform.position = WorldUIPositioner.Calculate(
                            Camera.main, pendingBounds, worldSize, pendingForceRight);
                    }
                }
            }
            hoverClock += Time.deltaTime;
            return;
        }

        if (ownsTooltip)
        {
            hoverClock = 0;
            ownsTooltip = false;
            tooltipObject.SetActive(false);
        }
        else
        {
            hoverClock = 0;
        }
    }

    void OnDestroy()
    {
        if (tooltipObject != null && ownsTooltip)
        {
            tooltipObject.SetActive(false);
            ownsTooltip = false;
        }
    }
}
