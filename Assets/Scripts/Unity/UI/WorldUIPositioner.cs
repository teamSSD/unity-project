using UnityEngine;

public static class WorldUIPositioner
{
    private const float GAP = 0.6f;

    /// <summary>
    /// 패널을 대상 스프라이트 좌/우에 배치.
    /// forceRight=true: 항상 오른쪽 (재료용)
    /// forceRight=false: 화면 중심 방향 (요리도구/말풍선용)
    /// Y: 패널 < 스프라이트 → 스프라이트 상단 기준, 패널 ≥ 스프라이트 → center 기준. 화면 경계 클램프.
    /// </summary>
    public static Vector3 Calculate(Camera cam, Bounds target, Vector2 panelWorldSize, bool forceRight = false)
    {
        bool placeRight = forceRight || cam.WorldToViewportPoint(target.center).x < 0.5f;

        float x = placeRight
            ? target.max.x + GAP + panelWorldSize.x * 0.5f
            : target.min.x - GAP - panelWorldSize.x * 0.5f;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        // Y: 패널이 스프라이트보다 작으면 상단 기준, 크거나 같으면 center 기준
        float halfPanelH = panelWorldSize.y * 0.5f;
        float yRaw = (panelWorldSize.y < target.size.y)
            ? target.max.y - halfPanelH
            : target.center.y;

        float yMin = c.y - halfH + halfPanelH;
        float yMax = c.y + halfH - halfPanelH;
        float y = (yMin <= yMax)
            ? Mathf.Clamp(yRaw, yMin, yMax)
            : c.y;

        // X도 화면 경계 클램프
        x = Mathf.Clamp(x, c.x - halfW + panelWorldSize.x * 0.5f, c.x + halfW - panelWorldSize.x * 0.5f);

        return new Vector3(x, y, target.center.z);
    }
}
