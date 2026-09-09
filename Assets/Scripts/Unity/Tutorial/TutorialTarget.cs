using UnityEngine;

/// <summary>
/// 튜토리얼 말풍선의 앵커 타겟. 씬/프리팹의 UI 요소나 world 오브젝트에 붙여 key로 등록.
/// TutorialController가 stepPart.targetKey로 조회 → 스크린 위치 반환.
///
/// 사용:
///   1. UI 요소 (Button 등)에 붙이면 RectTransform 사용 (Canvas.WorldCorners → screen 좌표).
///   2. World 오브젝트 (SpriteRenderer)에 붙이면 bounds.top → Camera.WorldToScreenPoint.
///   3. 아무 것도 없으면 transform.position → Camera.WorldToScreenPoint.
/// </summary>
[DisallowMultipleComponent]
public class TutorialTarget : MonoBehaviour
{
    [SerializeField, Tooltip("고유 key. TutorialStepPart.targetKey와 매치.")]
    private string key;

    [SerializeField,
     Tooltip("있으면 이 Transform의 world position을 anchor로 사용 (SpriteRenderer/RectTransform 무시). 자식 Empty GO를 원하는 위치에 두고 여기 wire.")]
    private Transform anchorOverride;

    [SerializeField, Range(0f, 1f),
     Tooltip("SpriteRenderer 대상의 수직 앵커 (anchorOverride 없을 때). 0=하단, 0.5=중앙, 1=상단.")]
    private float verticalAnchorFraction = 1f;

    [SerializeField,
     Tooltip("최종 스크린 좌표에 더할 1920x1080 기준 Canvas 오프셋. 미세 조정.")]
    private Vector2 screenOffset = Vector2.zero;

    public string Key => key;

    /// <summary>런타임 key 지정 (mock 컨트롤러가 스폰된 GO에 target 부여할 때).
    /// 이미 등록되어 있으면 재등록.</summary>
    public void SetKey(string newKey)
    {
        if (!string.IsNullOrEmpty(key) && key != newKey)
            TutorialController.UnregisterTarget(key);
        key = newKey;
        if (!string.IsNullOrEmpty(key) && isActiveAndEnabled)
            TutorialController.RegisterTarget(key, this);
    }

    /// <summary>런타임 vertical anchor 지정. 0=하단, 0.5=중앙, 1=상단.</summary>
    public void SetVerticalAnchor(float fraction) => verticalAnchorFraction = Mathf.Clamp01(fraction);

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(key))
            TutorialController.RegisterTarget(key, this);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(key))
            TutorialController.UnregisterTarget(key);
    }

    /// <summary>이 타겟의 화면(스크린) 위치 반환. bubble tail이 여기 꽂힘.</summary>
    public Vector2 GetScreenPosition(float referenceOffsetScale = 1f)
    {
        Vector2 scaledOffset = TutorialScreenPlacement.ScaleReferenceOffset(screenOffset, referenceOffsetScale);

        // anchorOverride가 있으면 그 world position을 스크린으로 변환.
        if (anchorOverride != null)
        {
            var cam0 = Camera.main;
            if (cam0 == null) return Vector2.zero;
            return (Vector2)cam0.WorldToScreenPoint(anchorOverride.position) + scaledOffset;
        }

        var rt = transform as RectTransform;
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var center = new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
            return center + scaledOffset;
        }

        var cam = Camera.main;
        if (cam == null) return Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // verticalAnchorFraction: 0=하단, 1=상단.
            float y = sr.bounds.min.y + sr.bounds.size.y * verticalAnchorFraction;
            var anchorWorld = new Vector3(sr.bounds.center.x, y, sr.bounds.center.z);
            return (Vector2)cam.WorldToScreenPoint(anchorWorld) + scaledOffset;
        }

        return (Vector2)cam.WorldToScreenPoint(transform.position) + scaledOffset;
    }
}
