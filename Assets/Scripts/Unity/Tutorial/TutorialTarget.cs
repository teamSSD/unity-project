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

    public string Key => key;

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
    public Vector2 GetScreenPosition()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // Overlay canvas UI인 경우 WorldCorners = 스크린 픽셀 좌표.
            // 중앙점 반환 (호출부가 top-center 등을 원하면 TutorialBubble.PlaceNearRectTransform 사용).
            return new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
        }

        var cam = Camera.main;
        if (cam == null) return Vector2.zero;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Sprite bounds의 위쪽(bubble이 위에 오게 tail이 target 위쪽에 꽂힘).
            var topCenter = sr.bounds.center + Vector3.up * sr.bounds.extents.y;
            return cam.WorldToScreenPoint(topCenter);
        }

        return cam.WorldToScreenPoint(transform.position);
    }
}
