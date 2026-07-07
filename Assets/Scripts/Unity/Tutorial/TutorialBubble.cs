using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 튜토리얼 말풍선 (ScreenSpaceOverlay canvas 자식).
/// 구조:
///  Root (pivot 0.5 0) — position = target screen point
///   ├─ Body (VLG+CSF 자동 크기)
///   │   ├─ OptionalImage (기본 비활성)
///   │   └─ Contents (TMP)
///   └─ Tail (pivot 0.5 0 by default) — tip at root origin
///
/// SetDirection에 따라 tail 회전 + body 반대편 배치.
/// </summary>
public class TutorialBubble : MonoBehaviour
{
    public enum TailDirection { Down, Up }

    [SerializeField] private RectTransform body;
    [SerializeField] private RectTransform tail;
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private Image optionalImage;

    [Header("Placement")]
    [SerializeField, Tooltip("body와 tail base 사이 seamless overlap (px)")]
    private float tailBodyOverlap = 25f;
    [SerializeField, Tooltip("Tail이 body의 어느 쪽에 붙을지. -1=body 왼쪽 코너, 0=중앙, +1=body 오른쪽 코너. 대략 ±0.5 권장.")]
    private float tailHorizontalFraction = -0.5f;

    private bool _interactive;
    private System.Action _onDismiss;
    private bool _spawnFrameSkipped;
    private bool _tutorialLocked;

    public void SetContents(string text)
    {
        if (contents != null) contents.text = text;
    }

    public void SetImage(Sprite sprite)
    {
        if (optionalImage == null) return;
        if (sprite == null) { optionalImage.gameObject.SetActive(false); return; }
        optionalImage.sprite = sprite;
        optionalImage.gameObject.SetActive(true);
    }

    public void EnableInteractiveDismiss(System.Action onDismissed)
    {
        _interactive = true;
        _onDismiss = onDismissed;
        if (!_tutorialLocked)
        {
            UILockManager.Lock(UILockManager.Owner.Tutorial);
            _tutorialLocked = true;
        }
    }

    /// <summary>스크린 좌표에 tail tip 꽂음. dir로 tail 방향/body 위치 결정.</summary>
    public void PlaceAtScreenPoint(Vector2 targetScreenPos, TailDirection dir = TailDirection.Down)
    {
        if (body != null) LayoutRebuilder.ForceRebuildLayoutImmediate(body);
        ApplyDirection(dir);
        transform.position = (Vector3)targetScreenPos;
    }

    public void PlaceAtWorldPoint(Vector3 worldPoint, TailDirection dir = TailDirection.Down)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 screenPos = cam.WorldToScreenPoint(worldPoint);
        PlaceAtScreenPoint(screenPos, dir);
    }

    public void PlaceNearRectTransform(RectTransform target, TailDirection dir = TailDirection.Down)
    {
        if (target == null) return;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners[0]=bl, [1]=tl, [2]=tr, [3]=br
        Vector2 tip = dir == TailDirection.Up
            ? new Vector2((corners[0].x + corners[3].x) * 0.5f, corners[0].y)  // 대상 bottom-center
            : new Vector2((corners[1].x + corners[2].x) * 0.5f, corners[1].y); // 대상 top-center
        PlaceAtScreenPoint(tip, dir);
    }

    /// <summary>tail 회전 + body offset. 세로 sprite라 Down/Up만 지원.
    /// tailHorizontalFraction: -1(body 왼쪽 끝) ~ +1(body 오른쪽 끝). body는 tail 반대편으로 밀림.</summary>
    private void ApplyDirection(TailDirection dir)
    {
        if (tail == null || body == null) return;
        float baseOffset = tail.rect.height - tailBodyOverlap;
        float bodyW = body.rect.width;
        // tail이 body의 X 방향으로 얼마나 오프셋됐는지 → body는 반대 방향으로 이동.
        float bodyOffsetX = -tailHorizontalFraction * 0.5f * bodyW;

        tail.pivot = new Vector2(0.5f, 0f);
        tail.anchoredPosition = Vector2.zero;

        if (dir == TailDirection.Up)
        {
            tail.localRotation = Quaternion.Euler(0, 0, 180);
            body.pivot = new Vector2(0.5f, 1f);
            body.anchoredPosition = new Vector2(bodyOffsetX, -baseOffset);
        }
        else // Down
        {
            tail.localRotation = Quaternion.identity;
            body.pivot = new Vector2(0.5f, 0f);
            body.anchoredPosition = new Vector2(bodyOffsetX, baseOffset);
        }
    }

    private void Update()
    {
        if (!_interactive) return;
        if (!_spawnFrameSkipped) { _spawnFrameSkipped = true; return; }
        if (Input.GetKeyDown(KeyCode.Space)) Dismiss();
    }

    private void Dismiss()
    {
        _interactive = false;
        var cb = _onDismiss;
        _onDismiss = null;
        if (_tutorialLocked)
        {
            UILockManager.Unlock(UILockManager.Owner.Tutorial);
            _tutorialLocked = false;
        }
        cb?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_tutorialLocked)
        {
            UILockManager.Unlock(UILockManager.Owner.Tutorial);
            _tutorialLocked = false;
        }
    }
}
