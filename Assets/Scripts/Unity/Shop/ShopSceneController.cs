using UnityEngine;

/// <summary>
/// Scene_Shop 씬 컨트롤러.
/// - 진입 시 플레이어를 entryPoint X, 바닥 위에 정확히 배치
/// - 카메라 즉시 스냅
/// (이동 속도는 PlayerMove.moveSpeed 그대로 — 씬별 override 안 함. 대신 카메라 orthographicSize로 이동감 조정)
/// </summary>
public class ShopSceneController : MonoBehaviour
{
    [SerializeField] private Transform entryPoint;
    [SerializeField] private BoxCollider2D floor;

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

        // 바닥에 정확히 착지 — collider bounds + offset 반영.
        var playerBox = player.GetComponent<BoxCollider2D>();
        float spawnY = entryPoint.position.y;
        if (floor != null && playerBox != null)
        {
            float floorTopY = floor.bounds.max.y;
            float pScaleY = player.transform.localScale.y;
            float pHalfY = playerBox.size.y * 0.5f * pScaleY;
            float pOffsetY = playerBox.offset.y * pScaleY;
            spawnY = floorTopY - pOffsetY + pHalfY;
        }
        player.transform.position = new Vector3(entryPoint.position.x, spawnY, 0f);

        // 카메라 즉시 정렬 (velocity 리셋 포함)
        Object.FindFirstObjectByType<CameraFollow>()?.SnapToPlayer();
    }
}
