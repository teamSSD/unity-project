using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손님 위 말풍선. Tail은 버블 bottom flat 영역에 base가 attach, sprite는 아래로 뻗어
/// tip이 손님 target에 닿음.
/// - Tail sprite pivot (0, 0): anchor = sprite bottom-left = tip.
/// - Sprite 전체가 anchor 위-오른쪽으로 뻗음. 따라서 sprite top이 base.
/// - Base가 bubble bottom (Y = -halfH)에 오도록 anchor.y = -halfH - tail.rect.height.
/// - Bubble world position = target - (tip local offset * canvas lossyScale).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private RectTransform tail;

    [Header("Tail attachment on bubble bottom flat area")]
    [SerializeField, Tooltip("Tail base가 버블 corner에서 최소 얼마나 안쪽에 붙어야 하나 (rect units). 라운드 코너 반경보다 크게.")]
    private float tailBaseMarginFromCorner = 50f;
    [SerializeField, Tooltip("Tail sprite top이 버블 bottom을 얼마나 뚫고 올라와 seamless 연결 (rect units). 원래 = 52.")]
    private float tailOverlapWithBubble = 52f;

    [Header("Target 위치 (bounds 정규화 좌표)")]
    [SerializeField, Range(-1f, 1f),
     Tooltip("X: 0=중앙, ±1=edge. 손님 얼굴이 sprite bounds 중앙이면 0.")]
    private float targetXFractionFromCenter = 0f;
    [SerializeField, Range(-1f, 1f),
     Tooltip("Y: 0=중앙, 1=top edge. 얼굴이 상단이면 0.7~0.8.")]
    private float targetYFractionFromCenter = 0.7f;

    [Header("Bubble world offset (최종 위치에 더함)")]
    [SerializeField, Tooltip("계산된 bubble 위치에 더할 world offset. 미세 조정용.")]
    private Vector2 bubbleWorldOffset = new Vector2(-0.78f, -0.42f);

    public void setContents(string text)
    {
        contents.text = text;
    }

    /// <summary>Bubble을 손님 근처에 배치.
    /// forceBubbleOnLeftOfCustomer: null=카메라 기준 자동, true=항상 손님 왼쪽, false=항상 오른쪽.
    /// bubbleWorldOffset이 특정 side 기준으로 캘리브된 경우 side를 고정해야 오프셋 유효.</summary>
    public void PlaceNear(Bounds targetBounds, bool? forceBubbleOnLeftOfCustomer = null)
    {
        Camera cam = Camera.main;

        RectTransform rt = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // bubble이 손님 왼쪽 → customerOnLeft(카메라 기준 손님이 왼쪽) = false.
        // 즉 forceBubbleOnLeft=true → customerOnLeft=false.
        bool customerOnLeft;
        if (forceBubbleOnLeftOfCustomer.HasValue)
            customerOnLeft = !forceBubbleOnLeftOfCustomer.Value;
        else
            customerOnLeft = cam.WorldToViewportPoint(targetBounds.center).x < 0.5f;

        float halfW = rt.rect.width * 0.5f;
        float halfH = rt.rect.height * 0.5f;
        float tailW = tail.rect.width;
        float tailH = tail.rect.height;

        // Tail base가 bubble bottom flat 영역 안쪽에 붙게.
        // Sprite (pivot 0,0)는 anchor에서 up-right로 뻗어 sprite top edge = base가 bubble bottom.
        //   anchor.y = -halfH - tailH → sprite Y from -halfH-tailH (tip) to -halfH (base).
        // customerOnLeft: 손님이 왼쪽 → tail base가 bubble bottom 왼쪽 flat 영역
        //   no-flip: base X from anchor.x to anchor.x + tailW.
        //   base left edge = anchor.x = -halfW + tailBaseMarginFromCorner.
        // customerOnRight (!customerOnLeft): base 오른쪽 flat 영역
        //   flip: sprite extends up-LEFT, base X from anchor.x - tailW to anchor.x.
        //   base right edge = anchor.x = halfW - tailBaseMarginFromCorner.
        float tailX = customerOnLeft ? -halfW + tailBaseMarginFromCorner
                                     : halfW - tailBaseMarginFromCorner;
        // tailY: tip Y. sprite top = anchor.y + tailH. Base가 bubble bottom을 overlap만큼 뚫고 올라오려면
        // sprite top = -halfH + overlap → anchor.y = -halfH + overlap - tailH.
        float tailY = -halfH + tailOverlapWithBubble - tailH;
        tail.anchoredPosition = new Vector2(tailX, tailY);
        tail.localScale = new Vector3(customerOnLeft ? 1f : -1f, 1f, 1f);

        // Target world point
        Vector3 target = new Vector3(
            targetBounds.center.x + targetXFractionFromCenter * targetBounds.extents.x,
            targetBounds.center.y + targetYFractionFromCenter * targetBounds.extents.y,
            targetBounds.center.z
        );

        // Bubble world = target - tail tip local offset (canvas lossyScale 반영) + 미세조정 offset.
        Vector3 tipLocalOffset = new Vector3(tailX, tailY, 0f);
        Vector3 tipWorldOffset = tipLocalOffset * rt.lossyScale.x;
        transform.position = target - tipWorldOffset + (Vector3)bubbleWorldOffset;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
