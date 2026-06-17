using UnityEngine;

/// <summary>
/// Scene_Shop 씬 컨트롤러.
/// 진입 시 플레이어를 ExitToMall 문의 X에, Y는 Floor 윗면에 캐릭터 발이 닿도록 배치.
/// 카메라도 플레이어 위치 + offset으로 즉시 정렬.
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

        float spawnY = entryPoint.position.y;
        var playerBox = player.GetComponent<BoxCollider2D>();
        if (floor != null && playerBox != null)
        {
            float floorTopY = floor.transform.position.y + floor.size.y * floor.transform.localScale.y * 0.5f;
            float playerHalfY = playerBox.size.y * player.transform.localScale.y * 0.5f;
            spawnY = floorTopY + playerHalfY;
        }
        player.transform.position = new Vector3(entryPoint.position.x, spawnY, 0f);

        var cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.transform.position = new Vector3(
                player.transform.position.x + cam.offset.x,
                player.transform.position.y + cam.offset.y,
                cam.transform.position.z);
        }
    }
}
