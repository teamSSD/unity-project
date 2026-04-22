using UnityEngine;

/// <summary>
/// SpriteRenderer 범위를 기준으로 좌우 경계 벽 콜라이더를 런타임에 자동 생성.
/// Background 오브젝트에 부착.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicBoundaryWalls : MonoBehaviour
{
    [SerializeField] private float wallThickness = 0.5f;
    [SerializeField] private float wallHeight = 25f;

    void Awake()
    {
        var bounds = GetComponent<SpriteRenderer>().bounds;
        CreateWall("Wall_Left",  bounds.min.x - wallThickness * 0.5f, bounds.center.y);
        CreateWall("Wall_Right", bounds.max.x + wallThickness * 0.5f, bounds.center.y);
    }

    void CreateWall(string wallName, float x, float y)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(x, y, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(wallThickness, wallHeight);
    }
}
