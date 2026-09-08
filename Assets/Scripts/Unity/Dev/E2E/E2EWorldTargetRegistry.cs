using System.Collections.Generic;
using UnityEngine;

#if AFTERTASTE_E2E
/// <summary>월드 목적지와 플레이어의 화면상 상대 위치만 E2E에 제공한다.
/// 이동 자체는 브라우저 키보드 입력으로만 한다.</summary>
public static class E2EWorldTargetRegistry
{
    private static readonly Dictionary<string, Transform> Points = new();
    private static readonly Dictionary<string, Collider2D> ColliderPoints = new();
    private static readonly Dictionary<string, System.Func<Vector3>> DynamicPoints = new();

    public static void Register(string id, Transform point)
    {
        if (!string.IsNullOrWhiteSpace(id) && point != null) Points[id] = point;
    }

    public static void Unregister(string id, Transform point)
    {
        if (!string.IsNullOrWhiteSpace(id) && Points.TryGetValue(id, out var current) && current == point)
            Points.Remove(id);
    }

    public static void RegisterCollider(string id, Collider2D collider)
    {
        if (!string.IsNullOrWhiteSpace(id) && collider != null) ColliderPoints[id] = collider;
    }

    public static void UnregisterCollider(string id, Collider2D collider)
    {
        if (!string.IsNullOrWhiteSpace(id) && ColliderPoints.TryGetValue(id, out var current) && current == collider)
            ColliderPoints.Remove(id);
    }

    /// <summary>시간에 따라 바뀌는 월드 안내선의 읽기 전용 관측 지점.</summary>
    public static void RegisterDynamic(string id, System.Func<Vector3> positionProvider)
    {
        if (!string.IsNullOrWhiteSpace(id) && positionProvider != null) DynamicPoints[id] = positionProvider;
    }

    public static void UnregisterDynamic(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)) DynamicPoints.Remove(id);
    }

    public static bool TryGetWorldPosition(string id, out Vector3 position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (Points.TryGetValue(id, out var point) && point != null) { position = point.position; return true; }
        if (ColliderPoints.TryGetValue(id, out var collider) && collider != null) { position = collider.bounds.center; return true; }
        return false;
    }

    public static List<E2EUiTargetRegistry.TargetState> Capture()
    {
        var result = new List<E2EUiTargetRegistry.TargetState>();
        var stale = new List<string>();
        foreach (var (id, point) in Points)
        {
            if (point == null)
            {
                stale.Add(id);
                continue;
            }
            var screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(point.position) : Vector3.zero;
            bool visible = point.gameObject.activeInHierarchy && screenPoint.x >= 0f && screenPoint.x <= Screen.width && screenPoint.y >= 0f && screenPoint.y <= Screen.height;
            result.Add(new E2EUiTargetRegistry.TargetState
            {
                id = $"world.{id}",
                kind = "world",
                visible = visible,
                interactable = false,
                x = screenPoint.x / Screen.width,
                y = (Screen.height - screenPoint.y) / Screen.height,
                width = 0f,
                height = 0f,
            });
        }
        foreach (var (id, collider) in ColliderPoints)
        {
            if (collider == null)
            {
                stale.Add(id);
                continue;
            }
            var worldCenter = collider.bounds.center;
            var screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(worldCenter) : Vector3.zero;
            bool visible = collider.gameObject.activeInHierarchy && screenPoint.x >= 0f && screenPoint.x <= Screen.width && screenPoint.y >= 0f && screenPoint.y <= Screen.height;
            result.Add(new E2EUiTargetRegistry.TargetState
            {
                id = $"world.{id}", kind = "world", visible = visible, interactable = false,
                x = screenPoint.x / Screen.width, y = (Screen.height - screenPoint.y) / Screen.height,
                width = 0f, height = 0f,
            });
        }
        foreach (var (id, positionProvider) in DynamicPoints)
        {
            Vector3 worldPoint;
            try { worldPoint = positionProvider(); }
            catch { stale.Add(id); continue; }
            var screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPoint) : Vector3.zero;
            bool visible = screenPoint.x >= 0f && screenPoint.x <= Screen.width && screenPoint.y >= 0f && screenPoint.y <= Screen.height;
            result.Add(new E2EUiTargetRegistry.TargetState
            {
                id = $"world.{id}", kind = "world", visible = visible, interactable = false,
                x = screenPoint.x / Screen.width, y = (Screen.height - screenPoint.y) / Screen.height,
                width = 0f, height = 0f,
            });
        }
        foreach (var id in stale) Points.Remove(id);
        foreach (var id in stale) ColliderPoints.Remove(id);
        foreach (var id in stale) DynamicPoints.Remove(id);
        return result;
    }
}
#endif
