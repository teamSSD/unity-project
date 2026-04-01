using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HorizontalCameraMove : MonoBehaviour
{
    [Range(0f, 0.5f)] [SerializeField] private float edgeZone = 0.1f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float inputSmoothTime = 0.2f;
    [SerializeField] private KeyCode holdKey = KeyCode.None;
    [SerializeField] private Collider2D worldCollider;

    private Camera cam;
    private Transform tr;
    private float smoothedInput;
    private float inputVelocity;
    private float prevRawInput;

    void Awake()
    {
        cam = GetComponent<Camera>();
        tr = transform;
    }

    void LateUpdate()
    {
        if (!IsActive()) return;

        float raw = GetRawInput();
        ResetSmoothingIfDirectionFlipped(raw);
        SmoothInputTowards(raw);

        Vector3 pos = tr.position;
        pos.x += smoothedInput * moveSpeed * Time.deltaTime;

        Bounds map = GetBoundsOrView();
        ClampX(ref pos, map);

        tr.position = pos;
    }

    bool IsActive()
    {
        return holdKey == KeyCode.None || Input.GetKey(holdKey);
    }

    float GetRawInput()
    {
        // 키보드 입력
        float kb = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  kb = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kb =  1f;
        if (kb != 0f) return kb;

        // 마우스 엣지 입력 (키보드 미사용 시 폴백)
        float mx = Input.mousePosition.x / Mathf.Max(1f, (float)Screen.width);
        float leftEnd = edgeZone;
        float rightBeg = 1f - edgeZone;

        if (mx <= leftEnd)  return -Mathf.InverseLerp(leftEnd, 0f, mx);
        if (mx >= rightBeg) return  Mathf.InverseLerp(rightBeg, 1f, mx);
        return 0f;
    }

    void ResetSmoothingIfDirectionFlipped(float raw)
    {
        if (Mathf.Sign(raw) != Mathf.Sign(prevRawInput)) inputVelocity = 0f;
        prevRawInput = raw;
    }

    void SmoothInputTowards(float raw)
    {
        smoothedInput = Mathf.SmoothDamp(smoothedInput, raw, ref inputVelocity, inputSmoothTime);
    }

    Bounds GetBoundsOrView()
    {
        return worldCollider != null ? worldCollider.bounds : CurrentViewBounds();
    }

    void ClampX(ref Vector3 pos, Bounds map)
    {
        Vector2 half = GetCameraHalfSize();
        float minX = map.min.x + half.x;
        float maxX = map.max.x - half.x;
        if (minX > maxX)
        {
            float mid = (map.min.x + map.max.x) * 0.5f;
            minX = maxX = mid;
        }

        float beforeX = pos.x;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (!Mathf.Approximately(beforeX, pos.x) && Mathf.Abs(smoothedInput) > 0f)
        {
            smoothedInput = 0f;
            inputVelocity = 0f;
        }

        pos.y = tr.position.y;
        pos.z = tr.position.z;
    }

    Vector2 GetCameraHalfSize()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        return new Vector2(halfW, halfH);
    }

    Bounds CurrentViewBounds()
    {
        Vector2 half = GetCameraHalfSize();
        var c = tr.position;
        return new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(half.x * 2f, half.y * 2f, 1f));
    }
}
