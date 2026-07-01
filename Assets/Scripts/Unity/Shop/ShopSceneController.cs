using UnityEngine;

/// <summary>
/// Scene_Shop 씬 컨트롤러.
/// - 진입 시 플레이어를 ExitToMall X, 바닥 위에 정확히 배치 (gravity로 떨어지지 않게 collider bounds+offset 반영)
/// - 상점 안에서만 이동 속도 override (씬 나갈 때 원복)
/// - 문지방 걸리지 않도록 오른쪽 boundary 벽 자동 생성
/// </summary>
public class ShopSceneController : MonoBehaviour
{
    [SerializeField] private Transform entryPoint;
    [SerializeField] private BoxCollider2D floor;

    [Header("Shop 내 이동 튜닝")]
    [Tooltip("상점 안에서 PlayerMove.moveSpeed override 값. 씬 나갈 때 원래 값으로 복원.")]
    [SerializeField] private float moveSpeedOverride = 8f;

    [Header("오른쪽 문지방 boundary")]
    [Tooltip("이 X보다 오른쪽으로 못 가게 벽 생성 (문지방 걸림 방지)")]
    [SerializeField] private float rightBoundaryX = 11.01f;
    [SerializeField] private float wallThickness = 0.5f;
    [SerializeField] private float wallHeight = 20f;

    private PlayerMove playerMove;
    private float originalMoveSpeed;

    private void Start()
    {
        if (entryPoint == null)
        {
            var exit = GameObject.Find("ExitToMall");
            if (exit != null) entryPoint = exit.transform;
        }
        if (entryPoint == null) return;

        var player = GameObject.FindGameObjectWithTag(Tags.Player);
        if (player == null) return;

        // 1. 바닥에 정확히 착지 — collider bounds + offset 반영.
        var playerBox = player.GetComponent<BoxCollider2D>();
        float spawnY = entryPoint.position.y;
        if (floor != null && playerBox != null)
        {
            float floorTopY = floor.bounds.max.y;
            float pScaleY = player.transform.localScale.y;
            float pHalfY = playerBox.size.y * 0.5f * pScaleY;
            float pOffsetY = playerBox.offset.y * pScaleY;
            // player.transform.y + pOffsetY - pHalfY = floorTopY
            spawnY = floorTopY - pOffsetY + pHalfY;
        }
        player.transform.position = new Vector3(entryPoint.position.x, spawnY, 0f);

        // 2. 카메라 즉시 정렬
        var cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.transform.position = new Vector3(
                player.transform.position.x + cam.offset.x,
                player.transform.position.y + cam.offset.y,
                cam.transform.position.z);
        }

        // 3. 이동 속도 override
        playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            originalMoveSpeed = playerMove.moveSpeed;
            playerMove.moveSpeed = moveSpeedOverride;
        }

        // 4. 오른쪽 문지방 boundary 벽
        CreateRightBoundaryWall(spawnY);
    }

    private void CreateRightBoundaryWall(float centerY)
    {
        var wall = new GameObject("Wall_ShopThreshold");
        wall.transform.SetParent(transform);
        wall.transform.position = new Vector3(rightBoundaryX + wallThickness * 0.5f, centerY, 0f);
        var col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(wallThickness, wallHeight);
    }

    private void OnDestroy()
    {
        // moveSpeed 원복 (Player는 DontDestroyOnLoad라 다른 씬으로 값이 새면 안 됨)
        if (playerMove != null)
            playerMove.moveSpeed = originalMoveSpeed;
    }
}
