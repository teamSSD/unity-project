using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public float smoothTime = 0.15f;
    public float maxSpeed = 12f;

    [Header("카메라 경계 (boundsSource 할당 시 자동 계산, 없으면 수동값 사용)")]
    public SpriteRenderer boundsSource;
    public Vector2 xBoundsManual = new Vector2(-33f, 33f);
    public Vector2 yBoundsManual = new Vector2(-10f, 8f);

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            transform.position.z);

        Vector2 xB = xBoundsManual;
        Vector2 yB = yBoundsManual;

        if (boundsSource != null && cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            var b = boundsSource.bounds;
            xB = new Vector2(b.min.x + halfW, b.max.x - halfW);
            yB = new Vector2(b.min.y + halfH, b.max.y - halfH);
        }

        targetPos.x = Mathf.Clamp(targetPos.x, xB.x, xB.y);
        targetPos.y = Mathf.Clamp(targetPos.y, yB.x, yB.y);

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref velocity, smoothTime, maxSpeed);
    }
}
