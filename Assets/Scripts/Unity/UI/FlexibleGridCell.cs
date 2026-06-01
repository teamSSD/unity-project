using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GridLayoutGroup의 cellSize.x를 컨테이너 너비에 맞춰 자동 계산.
/// 패딩과 spacing을 고려해 columns개가 정확히 맞도록 설정.
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(RectTransform))]
public class FlexibleGridCell : MonoBehaviour
{
    private GridLayoutGroup grid;
    private RectTransform rt;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rt = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        UpdateCellSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        if (grid == null || rt == null) return;
        if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount) return;

        int cols = grid.constraintCount;
        if (cols <= 0) return;

        float width = rt.rect.width;
        if (width <= 0) return;

        float available = width - grid.padding.left - grid.padding.right - grid.spacing.x * (cols - 1);
        float cellWidth = available / cols;

        if (cellWidth > 0 && !Mathf.Approximately(grid.cellSize.x, cellWidth))
        {
            grid.cellSize = new Vector2(cellWidth, grid.cellSize.y);
        }
    }
}
