using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if AFTERTASTE_E2E
/// <summary>
/// 실제 WebGL 입력 테스트가 클릭할 수 있는 UI의 작은 관측 레지스트리.
/// 이 클래스는 버튼을 실행하지 않는다. 실행기는 반환된 화면 영역을 이용해
/// 브라우저의 실제 포인터 입력을 보낸다.
/// </summary>
public static class E2EUiTargetRegistry
{
    [Serializable]
    public sealed class TargetState
    {
        public string id;
        public string kind;
        public bool visible;
        public bool interactable;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    private static readonly Dictionary<string, Button> Buttons = new();
    // Button이 아닌 화면 자체의 실제 입력 지점도 있다. 예: 정산 화면은 어떤 키/클릭으로
    // 진행되며, 그 클릭은 별도 Button 이벤트가 아니라 SettlementController.Update에서 받는다.
    // 이 레지스트리는 그 지점을 관측만 제공한다. E2E는 여전히 브라우저 포인터 입력을 보낸다.
    private static readonly Dictionary<string, RectTransform> Rects = new();

    public static void Register(string id, Button button)
    {
        if (!string.IsNullOrWhiteSpace(id) && button != null) Buttons[id] = button;
    }

    public static void Unregister(string id, Button button)
    {
        if (!string.IsNullOrWhiteSpace(id) && Buttons.TryGetValue(id, out var current) && current == button)
            Buttons.Remove(id);
    }

    public static void RegisterRect(string id, RectTransform rect)
    {
        if (!string.IsNullOrWhiteSpace(id) && rect != null) Rects[id] = rect;
    }

    public static void UnregisterRect(string id, RectTransform rect)
    {
        if (!string.IsNullOrWhiteSpace(id) && Rects.TryGetValue(id, out var current) && current == rect)
            Rects.Remove(id);
    }

    public static List<TargetState> Capture()
    {
        var states = new List<TargetState>();
        var stale = new List<string>();
        foreach (var (id, button) in Buttons)
        {
            if (button == null)
            {
                stale.Add(id);
                continue;
            }

            var rect = button.transform as RectTransform;
            if (rect == null) continue;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = ToScreenPoint(button, corners[0]);
            var max = ToScreenPoint(button, corners[2]);
            float xMin = Mathf.Min(min.x, max.x);
            float xMax = Mathf.Max(min.x, max.x);
            float yMin = Mathf.Min(min.y, max.y);
            float yMax = Mathf.Max(min.y, max.y);
            bool insideMasks = ClipToParentMasks(button, ref xMin, ref xMax, ref yMin, ref yMax);
            bool visible = IsVisibleOnScreen(button) && insideMasks && IsInsideScreen(xMin, xMax, yMin, yMax);
            bool hasInputArea = xMax - xMin > 1f && yMax - yMin > 1f;

            states.Add(new TargetState
            {
                id = id,
                kind = "ui",
                visible = visible && hasInputArea,
                interactable = visible && hasInputArea && button.interactable,
                x = xMin / Screen.width,
                y = (Screen.height - yMax) / Screen.height,
                width = (xMax - xMin) / Screen.width,
                height = (yMax - yMin) / Screen.height,
            });
        }

        foreach (var id in stale) Buttons.Remove(id);

        stale.Clear();
        foreach (var (id, rect) in Rects)
        {
            if (rect == null)
            {
                stale.Add(id);
                continue;
            }

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = ToScreenPoint(rect, corners[0]);
            var max = ToScreenPoint(rect, corners[2]);
            float xMin = Mathf.Min(min.x, max.x);
            float xMax = Mathf.Max(min.x, max.x);
            float yMin = Mathf.Min(min.y, max.y);
            float yMax = Mathf.Max(min.y, max.y);
            bool visible = IsVisibleOnScreen(rect) && IsInsideScreen(xMin, xMax, yMin, yMax);
            bool hasInputArea = xMax - xMin > 1f && yMax - yMin > 1f;

            states.Add(new TargetState
            {
                id = id,
                kind = "ui-input-zone",
                visible = visible && hasInputArea,
                interactable = visible && hasInputArea,
                x = xMin / Screen.width,
                y = (Screen.height - yMax) / Screen.height,
                width = (xMax - xMin) / Screen.width,
                height = (yMax - yMin) / Screen.height,
            });
        }
        foreach (var id in stale) Rects.Remove(id);
        return states;
    }

    /// <summary>GameObject.activeInHierarchy만으로는 꺼진 Canvas의 자식을 보이는 것으로
    /// 오판한다. ConfirmModal처럼 Canvas.enabled로 표시하는 UI는 실제 포인터를 받을 수 있는
    /// 시점에만 E2E 타깃으로 노출해야 한다.</summary>
    private static bool IsVisibleOnScreen(Component component)
    {
        if (component == null || !component.gameObject.activeInHierarchy) return false;
        foreach (var canvas in component.GetComponentsInParent<Canvas>(true))
            if (!canvas.isActiveAndEnabled) return false;
        foreach (var group in component.GetComponentsInParent<CanvasGroup>(true))
            if (!group.isActiveAndEnabled || !group.interactable || !group.blocksRaycasts) return false;
        return true;
    }

    private static Vector2 ToScreenPoint(Component component, Vector3 worldPoint)
    {
        var canvas = component.GetComponentInParent<Canvas>();
        // ScreenSpaceOverlay에는 null 카메라가 맞지만, Cooking처럼 ScreenSpaceCamera UI는
        // 해당 캔버스 카메라를 사용해야 월드 좌표를 클릭 좌표로 잘못 해석하지 않는다.
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
    }

    /// <summary>ScrollRect viewport 밖의 버튼은 active 상태여도 실제 포인터를 받지 못한다.
    /// 부모 RectMask2D/Mask와 교차한 화면 영역만 E2E 클릭 영역으로 노출한다.</summary>
    private static bool ClipToParentMasks(Component component,
        ref float xMin, ref float xMax, ref float yMin, ref float yMax)
    {
        var clippedXMin = xMin;
        var clippedXMax = xMax;
        var clippedYMin = yMin;
        var clippedYMax = yMax;
        foreach (var mask in component.GetComponentsInParent<RectMask2D>(true))
        {
            if (!mask.isActiveAndEnabled) return false;
            IntersectWithRect(mask.rectTransform, mask,
                ref clippedXMin, ref clippedXMax, ref clippedYMin, ref clippedYMax);
            if (clippedXMax <= clippedXMin || clippedYMax <= clippedYMin) return false;
        }
        foreach (var mask in component.GetComponentsInParent<Mask>(true))
        {
            if (!mask.isActiveAndEnabled) return false;
            IntersectWithRect(mask.rectTransform, mask,
                ref clippedXMin, ref clippedXMax, ref clippedYMin, ref clippedYMax);
            if (clippedXMax <= clippedXMin || clippedYMax <= clippedYMin) return false;
        }
        xMin = clippedXMin;
        xMax = clippedXMax;
        yMin = clippedYMin;
        yMax = clippedYMax;
        return true;
    }

    private static void IntersectWithRect(RectTransform rect, Component context,
        ref float xMin, ref float xMax, ref float yMin, ref float yMax)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var min = ToScreenPoint(context, corners[0]);
        var max = ToScreenPoint(context, corners[2]);
        xMin = Mathf.Max(xMin, Mathf.Min(min.x, max.x));
        xMax = Mathf.Min(xMax, Mathf.Max(min.x, max.x));
        yMin = Mathf.Max(yMin, Mathf.Min(min.y, max.y));
        yMax = Mathf.Min(yMax, Mathf.Max(min.y, max.y));
    }

    private static bool IsInsideScreen(float xMin, float xMax, float yMin, float yMax) =>
        xMin >= 0f && yMin >= 0f && xMax <= Screen.width && yMax <= Screen.height;
}
#endif
