using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손님 위 말풍선. Tail의 tip(sprite 왼쪽 아래)이 손님 상대적 target에 정확히 닿게 배치.
/// Bubble world position = target - (tail tip local offset * canvas lossyScale).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private RectTransform tail;

    [Header("Tail 배치 (버블 내부)")]
    [SerializeField, Range(0f, 1f),
     Tooltip("Tail이 손님-쪽 edge에서 안쪽으로 얼마나 들어와 있나. 0=edge에 딱, 0.3=30% 인셋, 0.5=버블 중앙.")]
    private float tailInsetFromCustomerEdge = 0.3f;

    [Header("Target 오프셋 (bounds 기반, world unit)")]
    [SerializeField, Tooltip("손님 sprite edge에서 X offset. customerOnLeft면 max.x + x, 아니면 min.x - x. 양수 = 손님 더 바깥.")]
    private float targetXOffsetWorld = 0.5f;
    [SerializeField, Tooltip("손님 sprite top(bounds.max.y)에서 Y offset. 음수 = 아래로.")]
    private float targetYOffsetWorld = -0.6f;

    public void setContents(string text)
    {
        contents.text = text;
    }

    public void PlaceNear(Bounds targetBounds)
    {
        Camera cam = Camera.main;

        RectTransform rt = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // 손님이 카메라 왼쪽 절반 → 버블은 손님 오른쪽에 → 손님-쪽 edge는 버블 왼쪽
        bool customerOnLeft = cam.WorldToViewportPoint(targetBounds.center).x < 0.5f;

        // Tail anchoredPosition (버블 pivot 0.5,0.5 기준).
        // Tail의 RectTransform pivot이 (0,0)이라 sprite bottom-left(=tip)가 anchoredPosition 지점.
        // Tail X: 손님-쪽 edge에서 insetX만큼 안쪽.
        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        float insetX = tailInsetFromCustomerEdge * rt.rect.width;
        float tailX = customerOnLeft ? -halfW + insetX : halfW - insetX;
        float tailY = -halfH; // 버블 하단 = tail tip
        tail.anchoredPosition = new Vector2(tailX, tailY);
        // Flip: 손님이 오른쪽이면 sprite 반전 (tip 위치는 anchor 그대로, sprite 몸통만 반전).
        tail.localScale = new Vector3(customerOnLeft ? 1f : -1f, 1f, 1f);

        // Target world point
        Vector3 target = new Vector3(
            customerOnLeft ? targetBounds.max.x + targetXOffsetWorld
                           : targetBounds.min.x - targetXOffsetWorld,
            targetBounds.max.y + targetYOffsetWorld,
            targetBounds.center.z
        );

        // Bubble world position = target - (tail tip local offset * canvas lossyScale).
        // tail tip local = tail.anchoredPosition (pivot at tip이라 그대로).
        Vector3 tipLocalOffset = new Vector3(tailX, tailY, 0f);
        Vector3 tipWorldOffset = tipLocalOffset * rt.lossyScale.x;
        transform.position = target - tipWorldOffset;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
