using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public float smoothTime = 0.15f;
    public float maxSpeed = 12f;

    [Header("카메라 경계 — boundsSource 필수 (맵 sprite 자동 인식, sprite 교체 시 자동 재계산)")]
    public SpriteRenderer boundsSource;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (boundsSource == null)
            Debug.LogWarning($"[CameraFollow] {name}: boundsSource 미할당 — 카메라 경계 없음");
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            transform.position.z);

        if (boundsSource != null && cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            var b = boundsSource.bounds;
            targetPos.x = Mathf.Clamp(targetPos.x, b.min.x + halfW, b.max.x - halfW);
            targetPos.y = Mathf.Clamp(targetPos.y, b.min.y + halfH, b.max.y - halfH);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref velocity, smoothTime, maxSpeed);
    }
}
