using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 튜토리얼 말풍선 (ScreenSpaceOverlay canvas 자식). ui_speechBubble 스프라이트 재활용.
/// 구조:
///  Root (RectTransform + TutorialBubble)
///   ├─ Body   (Image ui_speechBubble_1 sliced + VLG + CSF) — 텍스트 길이에 맞춰 자동 크기, pivot bottom-center
///   │   ├─ OptionalImage (기본 비활성)
///   │   └─ Contents (TMP)
///   └─ Tail   (Image ui_speechBubble_0, pivot tip 위치) — target 좌표에 tip이 꽂힘
///
/// 배치: Body는 위, Tail은 아래로 뻗음. Tail tip이 targetScreenPos.
/// </summary>
public class TutorialBubble : MonoBehaviour
{
    [SerializeField] private RectTransform body;
    [SerializeField] private RectTransform tail;
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private Image optionalImage;

    [Header("Placement")]
    [SerializeField, Tooltip("body bottom과 tail top 사이 seamless 겹침 (px)")]
    private float tailBodyOverlap = 20f;
    [SerializeField, Tooltip("target 위 추가 gap (px). 0이면 tail tip이 정확히 target에 꽂힘.")]
    private float extraGapAboveTarget = 0f;

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

    /// <summary>스크린 좌표에 tail tip 꽂고, body를 위에 배치.</summary>
    public void PlaceAtScreenPoint(Vector2 targetScreenPos)
    {
        if (body != null) LayoutRebuilder.ForceRebuildLayoutImmediate(body);
        if (tail != null) tail.position = (Vector3)(targetScreenPos + Vector2.up * extraGapAboveTarget);
        if (body != null)
        {
            float tailH = tail != null ? tail.rect.height : 0f;
            body.position = (Vector3)(targetScreenPos + new Vector2(0, tailH - tailBodyOverlap + extraGapAboveTarget));
        }
    }

    /// <summary>월드 좌표 → 스크린 변환 후 배치.</summary>
    public void PlaceAtWorldPoint(Vector3 worldPoint)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 screenPos = cam.WorldToScreenPoint(worldPoint);
        PlaceAtScreenPoint(screenPos);
    }

    /// <summary>UI RectTransform 위쪽 중앙에 tail tip 꽂음.</summary>
    public void PlaceNearRectTransform(RectTransform target)
    {
        if (target == null) return;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // Overlay canvas 하위 RectTransform의 GetWorldCorners는 스크린 픽셀 좌표를 반환.
        // corners[1]=top-left, corners[2]=top-right → top center
        Vector2 topCenter = new Vector2((corners[1].x + corners[2].x) * 0.5f, corners[1].y);
        PlaceAtScreenPoint(topCenter);
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
