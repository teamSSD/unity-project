using System;
using UnityEngine;

public enum ClickState { None, Clicked, DragStart, Dragging, DragEnd }

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ClickStateUtil : MonoBehaviour
{
    [Header("카메라 / 피킹")]
    public Camera targetCamera;
    public LayerMask pickMask = ~0;
    public bool requireOverCollider = true;
    public bool requireTopMostAtPointer = true;
    [Min(1)] public int overlapBufferSize = 255;

    [Header("클릭 판정")]
    [Tooltip("이 시간 이내에 떼고, 이동도 작으면 Clicked로 간주 (unscaled)")]
    public float clickMaxDuration = 0.18f;
    [Tooltip("Clicked로 인정되는 최대 이동(픽셀)")]
    public float clickMaxMovePx = 6f;
    public Action OnClicked;
    public Action OnDragStart;
    public Action OnDragging;
    public Action OnDragEnd;
    public Action OnNone;

    public static bool globalLocked = false;

    private Collider2D col2d;
    private Collider2D[] overlapBuf;

    // 입력 추적
    private bool isDown;
    private bool dragging;
    private Vector2 downPosPx;
    private float  downTime;

    // 외부 조회용 현재 상태
    private ClickState current = ClickState.None;

    void Awake()
    {
        col2d = GetComponent<Collider2D>();
        overlapBuf = new Collider2D[Mathf.Max(1, overlapBufferSize)];
        if (targetCamera == null) targetCamera = Camera.main;
    }

    void Update()
    {
        if (globalLocked)
        {
            if (isDown) { isDown = false; dragging = false; }
            current = ClickState.None;
            return;
        }

        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;
        if (col2d == null) col2d = GetComponent<Collider2D>();

        current = DetectState(cam);

        InvokeFor(current);
    }

    public ClickState getState() => current;

    private ClickState DetectState(Camera cam)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TryBeginOverMe(cam))
            {
                isDown    = true;
                dragging  = false;
                downPosPx = Input.mousePosition;
                downTime  = Time.unscaledTime;
            }
            return ClickState.None;
        }

        if (!isDown) return ClickState.None;

        float heldSec = Time.unscaledTime - downTime;
        float movedPx = (((Vector2)Input.mousePosition) - downPosPx).magnitude;

        if (!dragging && (heldSec > clickMaxDuration || movedPx > clickMaxMovePx))
        {
            dragging = true;
            return ClickState.DragStart;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            return ClickState.Dragging;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDown = false;

            if (!dragging && heldSec <= clickMaxDuration && movedPx <= clickMaxMovePx)
            {
                return ClickState.Clicked;
            }

            if (!dragging)
            {
                dragging = true;
            }
            dragging = false;
            return ClickState.DragEnd;
        }

        return ClickState.None;
    }

    private void InvokeFor(ClickState state)
    {
        switch (state)
        {
            case ClickState.Clicked: SafeInvoke(OnClicked); break;
            case ClickState.DragStart: SafeInvoke(OnDragStart); break;
            case ClickState.Dragging: SafeInvoke(OnDragging); break;
            case ClickState.DragEnd: SafeInvoke(OnDragEnd); break;
            default: SafeInvoke(OnNone); break;
        }
    }

    private bool TryBeginOverMe(Camera cam)
    {
        if (!requireOverCollider) return true;

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(world, pickMask);
        if (hit == null || hit != col2d) return false;

        if (requireTopMostAtPointer && !IsTopMostAt(world)) return false;
        return true;
    }

    private bool IsTopMostAt(Vector2 worldPoint)
    {
        int count = Physics2D.OverlapCircleNonAlloc(worldPoint, 0.0005f, overlapBuf, pickMask);
        if (count <= 0) return false;

        Collider2D best = null;
        int bestLayerVal = int.MinValue;
        int bestOrder = int.MinValue;

        for (int i = 0; i < count; i++)
        {
            var c = overlapBuf[i];
            if (c == null) continue;

            var sr = c.GetComponent<SpriteRenderer>();
            int layerVal = sr ? SortingLayer.GetLayerValueFromID(sr.sortingLayerID) : 0;
            int order    = sr ? sr.sortingOrder : 0;

            bool better = layerVal > bestLayerVal || (layerVal == bestLayerVal && order > bestOrder);
            if (better) { bestLayerVal = layerVal; bestOrder = order; best = c; }
        }
        return best == col2d;
    }
    
    public void ForceDragStart()
    {
        isDown = true;
        dragging = true;
        downPosPx = Input.mousePosition;
        downTime = Time.unscaledTime;

        SafeInvoke(OnDragStart);
    }

    private static void SafeInvoke(Action cb)
    {
        try { cb?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
