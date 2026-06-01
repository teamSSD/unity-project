using System;
using UnityEngine;
using UnityEngine.UI;

// TooltipController는 Food 또는 CookingTool 프리팹에 부착 — 두 Model 동시 require 불가 (XOR).
// 본문에서 GetComponent<FoodModel/CookingToolModel> null-check로 동적 분기.
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

        var ui = UIManager.Instance;
        if (ui == null)
        {
            Debug.LogWarning($"[TooltipController] {gameObject.name}: UIManager.Instance is null! Cannot get global tooltip.");
            return;
        }

        if (GetComponent<FoodModel>() != null)
            tooltipObject = ui.IngredientTooltip;
        else if (GetComponent<CookingToolModel>() != null)
            tooltipObject = ui.CookingToolTooltip;

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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
