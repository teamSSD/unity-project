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
     Tooltip("Tail 앵커가 손님-쪽 edge에서 안쪽으로 얼마나 (0=edge, 0.3=30% 인셋, 0.5=버블 중앙).")]
    private float tailInsetFromCustomerEdge = 0.3f;
    [SerializeField, Tooltip("Tail tip이 버블 하단에서 얼마나 아래로 (rect units). 원래 스타일 = 132.")]
    private float tailBelowBubble = 132f;

    [Header("Target 위치 (bounds 정규화 좌표)")]
    [SerializeField, Range(-1f, 1f),
     Tooltip("X 위치: bounds.center 기준 extents.x 비율. 0=중앙, ±1=edge. 손님 얼굴이 중앙 근처면 0.")]
    private float targetXFractionFromCenter = 0f;
    [SerializeField, Range(-1f, 1f),
     Tooltip("Y 위치: bounds.center 기준 extents.y 비율. 0=중앙, 1=top edge. 얼굴이 상단 근처면 0.7~0.8.")]
    private float targetYFractionFromCenter = 0.7f;

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
        // Tail Y: 버블 하단에서 tailBelowBubble 만큼 아래 (tip이 sprite bottom-left이라 sprite는 이 위로 뻗음).
        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        float insetX = tailInsetFromCustomerEdge * rt.rect.width;
        float tailX = customerOnLeft ? -halfW + insetX : halfW - insetX;
        float tailY = -halfH - tailBelowBubble;
        tail.anchoredPosition = new Vector2(tailX, tailY);
        // Flip: pivot(0,0)에선 sprite가 anchor에서 up-right로 뻗음.
        // customerOnLeft → 손님 방향(왼쪽)으로 뻗어야 → flip. 반대 케이스는 flip X.
        tail.localScale = new Vector3(customerOnLeft ? -1f : 1f, 1f, 1f);

        // Target world point — 손님 bounds 정규화 위치 (extents 비율).
        // 크기 무관하게 얼굴/머리 영역을 일관되게 가리킴.
        Vector3 target = new Vector3(
            targetBounds.center.x + targetXFractionFromCenter * targetBounds.extents.x,
            targetBounds.center.y + targetYFractionFromCenter * targetBounds.extents.y,
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
