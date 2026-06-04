using UnityEngine;

/// <summary>
/// SpriteRenderer 범위 기준 4방향 경계 벽 콜라이더를 런타임에 자동 생성.
/// Background 오브젝트에 부착 — 맵 sprite 크기 변경 시 다음 Awake에서 자동 재계산.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicBoundaryWalls : MonoBehaviour
{
    [SerializeField] private float wallThickness = 0.5f;

    void Awake()
    {
        var bounds = GetComponent<SpriteRenderer>().bounds;
        float w = bounds.size.x;
        float h = bounds.size.y;

        CreateWall("Wall_Left",   bounds.min.x - wallThickness * 0.5f, bounds.center.y, wallThickness, h);
        CreateWall("Wall_Right",  bounds.max.x + wallThickness * 0.5f, bounds.center.y, wallThickness, h);
        CreateWall("Wall_Bottom", bounds.center.x, bounds.min.y - wallThickness * 0.5f, w, wallThickness);
        CreateWall("Wall_Top",    bounds.center.x, bounds.max.y + wallThickness * 0.5f, w, wallThickness);
    }

    void CreateWall(string wallName, float x, float y, float sizeX, float sizeY)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(x, y, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(sizeX, sizeY);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
