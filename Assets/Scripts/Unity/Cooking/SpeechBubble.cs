using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손님 위 말풍선. Tail의 tip(sprite 왼쪽 아래)이 손님 상대적 target에 닿게 배치.
/// Tail sprite는 버블 edge 밖으로 뻗어나가고 sprite body가 버블 안쪽으로 들어가는 구조.
/// Bubble world position = target - (tail tip local offset * canvas lossyScale).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private RectTransform tail;

    [Header("Tail 배치")]
    [SerializeField, Tooltip("Tail tip이 버블 customer-facing edge에서 얼마나 밖으로 (rect units, 양수 = 밖). 작을수록 tail이 버블 body 안쪽에 안정.")]
    private float tailBeyondBubbleEdgeX = 15f;
    [SerializeField, Tooltip("Tail tip이 버블 하단에서 얼마나 아래로 (rect units). 작을수록 tail이 버블 하단에 밀착.")]
    private float tailBelowBubble = 40f;

    [Header("Target 위치 (bounds 정규화 좌표)")]
    [SerializeField, Range(-1f, 1f),
     Tooltip("X: 0=중앙, 1=customer-facing edge (양수 = 버블 반대편 edge 방향).")]
    private float targetXFractionTowardBubbleFacing = 0.9f;
    [SerializeField, Range(-1f, 1f),
     Tooltip("Y: 0=중앙, 1=top edge. 얼굴이 상단 근처면 0.7~0.8.")]
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

        // 손님이 카메라 왼쪽 절반 → 버블은 손님 오른쪽에 위치.
        // customerOnLeft = 손님이 카메라 왼쪽, 버블은 오른쪽 → 버블의 customer-facing edge = 버블 왼쪽 (min.x).
        // !customerOnLeft = 버블이 왼쪽 → customer-facing edge = 버블 오른쪽 (max.x).
        bool customerOnLeft = cam.WorldToViewportPoint(targetBounds.center).x < 0.5f;

        // Tail anchor: 버블 customer-facing edge 바깥으로 tailBeyondBubbleEdgeX 만큼 나감.
        // Tail의 RectTransform pivot(0,0)이 sprite bottom-left(=tip)이라 anchoredPosition = tip.
        // sprite body는 anchor에서 up-toward-bubble 방향으로 뻗어 버블 안으로 들어감.
        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        float tailX = customerOnLeft ? -halfW - tailBeyondBubbleEdgeX
                                     : halfW + tailBeyondBubbleEdgeX;
        float tailY = -halfH - tailBelowBubble;
        tail.anchoredPosition = new Vector2(tailX, tailY);
        // Sprite 방향:
        // - customerOnLeft: 버블이 오른쪽, tail anchor는 버블 왼쪽 밖. Sprite body가 오른쪽(버블) 향해 뻗음
        //   → pivot(0,0) + no-flip → 자연스레 up-right.
        // - !customerOnLeft: 버블이 왼쪽, tail anchor는 버블 오른쪽 밖. Sprite body가 왼쪽(버블) 향해 뻗음
        //   → flip (scale.x = -1) → up-left.
        tail.localScale = new Vector3(customerOnLeft ? 1f : -1f, 1f, 1f);

        // Target world point — 손님 bounds 정규화 위치.
        // X: customer-facing edge 방향으로 fraction만큼. 손님이 왼쪽이면 max.x 방향, 아니면 min.x 방향.
        float xSign = customerOnLeft ? +1f : -1f;
        Vector3 target = new Vector3(
            targetBounds.center.x + xSign * targetXFractionTowardBubbleFacing * targetBounds.extents.x,
            targetBounds.center.y + targetYFractionFromCenter * targetBounds.extents.y,
            targetBounds.center.z
        );

        // Bubble world = target - tail tip local offset (canvas lossyScale 반영).
        Vector3 tipLocalOffset = new Vector3(tailX, tailY, 0f);
        Vector3 tipWorldOffset = tipLocalOffset * rt.lossyScale.x;
        transform.position = target - tipWorldOffset;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
