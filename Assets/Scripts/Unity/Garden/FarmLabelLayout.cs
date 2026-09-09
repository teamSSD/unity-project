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

    public static Vector3 AboveElement(
        Vector3 lowerCenter,
        float lowerHalfHeight,
        float upperHalfHeight,
        float clearance)
    {
        return new Vector3(
            lowerCenter.x,
            lowerCenter.y + lowerHalfHeight + upperHalfHeight + clearance,
            lowerCenter.z);
    }

    public static float HalfHeightInAncestor(RectTransform rect, Transform ancestor)
    {
        Vector3 bottom = ancestor.InverseTransformPoint(
            rect.TransformPoint(new Vector3(0f, rect.rect.yMin, 0f)));
        Vector3 top = ancestor.InverseTransformPoint(
            rect.TransformPoint(new Vector3(0f, rect.rect.yMax, 0f)));
        return Mathf.Abs(top.y - bottom.y) * 0.5f;
    }
}
