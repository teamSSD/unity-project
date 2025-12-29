using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // 따라갈 캐릭터
    public Vector3 offset;   // 카메라와 플레이어 사이 거리
    public float smoothSpeed = 0.125f; // 카메라 이동 부드럽게

    void LateUpdate()
    {
        if (player == null) return;

        // 목표 위치 = 플레이어 위치 + 오프셋
        Vector3 targetPos = new Vector3(player.position.x + offset.x, transform.position.y, transform.position.z);

        // 부드럽게 이동
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);

        transform.position = smoothedPos;
    }
}
