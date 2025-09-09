using UnityEngine;

public enum ClickState { None, ClickStart, Clicking, ClickEnd }

/*
 * 2D 클릭 상태 유틸 (보이는 것만 선택 가능 + 겹침 시 최상위 선택 옵션)
 * - requireVisible: 화면에 보이는 경우에만 클릭 허용 (뷰포트 안 + 렌더러 on + 알파)
 * - requireTopMostAtPointer: 같은 지점에 여러 콜라이더가 있으면 sortingOrder 최상위만 허용
 * - pickMask: 선택 대상 레이어 필터
 */
[RequireComponent(typeof(Collider2D))]
public class ClickStateUtil : MonoBehaviour
{
    [Header("Visible 조건")]
    public bool requireVisible = true;
    [Range(0f, 1f)] public float minAlpha = 0.01f;

    [Header("겹침 처리")]
    public bool requireTopMostAtPointer = true;
    public LayerMask pickMask = ~0;

    [Header("성능/버퍼 (겹침 처리용)")]
    [Min(1)] public int overlapBufferSize = 16;

    private bool isDragging;
    private Camera cam;
    private Collider2D col2d;
    private SpriteRenderer sr;
    private ClickState currentState;
    private Collider2D[] overlapBuf;

    void Start()
    {
        cam = Camera.main;
        col2d = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        overlapBuf = new Collider2D[Mathf.Max(1, overlapBufferSize)];
        isDragging = false;
        currentState = ClickState.None;
    }

    void Update()
    {
        currentState = DistinguishState();
    }

    public ClickState GetClickState() => currentState;

    private ClickState DistinguishState()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);

            // 1) 내 콜라이더 위인지
            var hit = Physics2D.OverlapPoint(p, pickMask);
            if (hit == null || hit != col2d) goto NotClicked;

            // 2) 보이는 상태인지(옵션)
            if (requireVisible && !IsVisibleOnScreen()) goto NotClicked;

            // 3) 겹침 시 최상위인지(옵션)
            if (requireTopMostAtPointer && !IsTopMostAtPointer(p)) goto NotClicked;

            isDragging = true;
            return ClickState.ClickStart;
        }

    NotClicked:
        // 클릭 유지
        if (isDragging && Input.GetMouseButton(0))
            return ClickState.Clicking;

        // 클릭 종료
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            return ClickState.ClickEnd;
        }

        return ClickState.None;
    }

    // 화면 안 + 렌더러 on + 알파 체크
    private bool IsVisibleOnScreen()
    {
        var vp = cam.WorldToViewportPoint(transform.position);
        if (!(vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f))
            return false;

        if (sr != null) return sr.enabled && sr.color.a >= minAlpha;

        var rend = GetComponent<Renderer>();
        return rend == null || rend.enabled;
    }

    // 같은 점에 겹친 콜라이더 중 최상위(SpriteRenderer.sortingLayerID → sortingOrder)인지
    private bool IsTopMostAtPointer(Vector2 p)
    {
        int count = Physics2D.OverlapCircleNonAlloc(p, 0.0005f, overlapBuf, pickMask);
        if (count <= 0) return false;

        Collider2D best = null;
        int bestLayer = int.MinValue;
        int bestOrder = int.MinValue;

        for (int i = 0; i < count; i++)
        {
            var c = overlapBuf[i];
            if (c == null) continue;

            // 가시성 필터(옵션)
            if (requireVisible)
            {
                var cSr = c.GetComponent<SpriteRenderer>();
                if (cSr != null && (!cSr.enabled || cSr.color.a < minAlpha))
                    continue;
            }

            var csr = c.GetComponent<SpriteRenderer>();
            int layer = csr ? csr.sortingLayerID : 0;
            int order = csr ? csr.sortingOrder : 0;

            if (layer > bestLayer || (layer == bestLayer && order > bestOrder))
            {
                bestLayer = layer;
                bestOrder = order;
                best = c;
            }
        }
        return best == col2d;
    }
}
