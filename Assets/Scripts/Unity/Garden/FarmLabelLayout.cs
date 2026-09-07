using UnityEngine;

/// <summary>작물 스프라이트의 실제 메시 윤곽을 기준으로 작물명 위치를 계산한다.</summary>
public static class FarmLabelLayout
{
    public static Bounds VisibleBounds(Vector2[] vertices, Bounds fallback)
    {
        if (vertices == null || vertices.Length == 0) return fallback;

        var min = vertices[0];
        var max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector2.Min(min, vertices[i]);
            max = Vector2.Max(max, vertices[i]);
        }

        var bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    public static Vector3 AboveCrop(
        Vector3 cropLocalPosition,
        Vector3 cropLocalScale,
        Bounds visibleSpriteBounds,
        float labelHalfHeight,
        float clearance)
    {
        float scaledTop = cropLocalScale.y >= 0f
            ? visibleSpriteBounds.max.y * cropLocalScale.y
            : visibleSpriteBounds.min.y * cropLocalScale.y;

        return new Vector3(
            cropLocalPosition.x + visibleSpriteBounds.center.x * cropLocalScale.x,
            cropLocalPosition.y + scaledTop + labelHalfHeight + clearance,
            cropLocalPosition.z);
    }
}
