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
    [SerializeField, Tooltip("Tail 스프라이트 하단의 투명 padding 보정 (px). 실제 visual tip이 rect bottom보다 위에 있음.")]
    private float tailTipVisualPadding = 8f;
    [SerializeField, Tooltip("Tail이 body의 어느 쪽에 붙을지. -1=body 왼쪽 코너, 0=중앙, +1=body 오른쪽 코너. 대략 ±0.5 권장.")]
    private float tailHorizontalFraction = -0.5f;

    private bool _interactive;
    private System.Action _onDismiss;
    private bool _spawnFrameSkipped;
    private bool _tutorialLocked;
    private KeyCode _dismissKey = KeyCode.Space;

    // Dynamic follow: 매 프레임 다시 위치 계산 (카메라가 움직이는 튜토리얼용).
    private System.Func<Vector2> _screenPosGetter;
    private System.Func<Vector2, float> _fractionGetter;
    private TailDirection _followDir;

    public void SetDismissKey(KeyCode key) { _dismissKey = key; }

    /// <summary>레시피북 안내 파트 (레시피북 조작 안내) 여부. true = RecipeBook(1100) 위에
    /// 표시되도록 sub-canvas overrideSorting=1200 부여. false = 부모 TutorialOverlayCanvas(1000)
    /// 상속 → RecipeBook 뒤로 밀림 (창 관통 문제 방어).</summary>
    public void SetStayOnTopOfRecipeBook(bool value)
    {
        var canvas = GetComponent<Canvas>();
        if (value)
        {
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1200;
        }
        else if (canvas != null)
        {
            canvas.overrideSorting = false;
        }
    }

    /// <summary>매 프레임 screenPosGetter/fractionGetter를 재호출해 위치+fraction 재계산.
    /// fractionGetter는 screen 좌표를 받아 상황별 fraction 반환 (autoFlip 등).</summary>
    public void EnableDynamicFollow(System.Func<Vector2> screenPosGetter, TailDirection dir, System.Func<Vector2, float> fractionGetter)
    {
        _screenPosGetter = screenPosGetter;
        _fractionGetter = fractionGetter;
        _followDir = dir;
    }

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

    public void EnableInteractiveDismiss(System.Action onDismissed, bool lockInput = true)
    {
        _interactive = true;
        _onDismiss = onDismissed;
        if (lockInput && !_tutorialLocked)
        {
            UILockManager.Lock(UILockManager.Owner.Tutorial);
            _tutorialLocked = true;
        }
    }

    /// <summary>스크린 좌표에 tail tip 꽂음. dir로 tail 방향/body 위치 결정.
    /// horizontalFractionOverride 있으면 SerializeField tailHorizontalFraction 대신 사용.</summary>
    public void PlaceAtScreenPoint(Vector2 targetScreenPos, TailDirection dir = TailDirection.Down, float? horizontalFractionOverride = null)
    {
        if (body != null) LayoutRebuilder.ForceRebuildLayoutImmediate(body);
        ApplyDirection(dir, horizontalFractionOverride ?? tailHorizontalFraction);
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

    /// <summary>Tail scale.x/scale.y로 방향/좌우 반전. Sprite tip이 rect bottom-LEFT라 pivot (0,0)로 설정.
    /// tailHorizontalFraction: -1(body 왼쪽 끝) ~ +1(body 오른쪽 끝).
    /// - fraction > 0 → scale.x = -1 (스프라이트 좌우 반전, base가 왼쪽으로 뻗어 body 안쪽 향함)
    /// - Up 방향 → scale.y = -1 (스프라이트 상하 반전)</summary>
    private void ApplyDirection(TailDirection dir, float horizontalFraction)
    {
        if (tail == null || body == null) return;
        float baseOffset = tail.rect.height - tailBodyOverlap;
        float bodyW = body.rect.width;
        float bodyOffsetX = -horizontalFraction * 0.5f * bodyW;

        // Tail pivot을 (0, 0) = rect의 bottom-left에 두면 sprite tip과 pivot 위치 일치.
        tail.pivot = new Vector2(0f, 0f);
        tail.localRotation = Quaternion.identity;

        float scaleY = (dir == TailDirection.Up) ? -1f : 1f;
        // fraction > 0 (tail이 body 오른쪽에 붙음) → sprite도 좌우 반전해서 base가 body 안쪽(왼쪽)으로 향하게.
        float scaleX = (horizontalFraction > 0f) ? -1f : 1f;
        tail.localScale = new Vector3(scaleX, scaleY, 1f);

        // tip은 sprite texture y=padding에 있음. scale.y로 방향 반전 시 anchor y 부호도 반전.
        tail.anchoredPosition = new Vector2(0, -tailTipVisualPadding * scaleY);

        if (dir == TailDirection.Up)
        {
            body.pivot = new Vector2(0.5f, 1f);
            body.anchoredPosition = new Vector2(bodyOffsetX, -baseOffset);
        }
        else
        {
            body.pivot = new Vector2(0.5f, 0f);
            body.anchoredPosition = new Vector2(bodyOffsetX, baseOffset);
        }
    }

    private void Update()
    {
        // Settings/Shop 등 모달 UI 열림 시 bubble 숨김 (버그 4: 설정 위로 둥둥 떠다니는 문제).
        // BentoSelection/PhaseSelection 등은 튜토리얼과 공존하는 UI (튜토리얼이 이 UI 사용법을 안내) — 제외.
        bool blockedByModal =
            UILockManager.IsLockedBy(UILockManager.Owner.Settings) ||
            UILockManager.IsLockedBy(UILockManager.Owner.Shop) ||
            UILockManager.IsLockedBy(UILockManager.Owner.Loading);
        if (body != null && body.gameObject.activeSelf == blockedByModal) body.gameObject.SetActive(!blockedByModal);
        if (tail != null && tail.gameObject.activeSelf == blockedByModal) tail.gameObject.SetActive(!blockedByModal);
        if (blockedByModal) return; // 위치 재계산도 스킵.

        // Dynamic follow — 카메라 이동/target 이동 대응. dismiss와 무관.
        if (_screenPosGetter != null)
        {
            var pos = _screenPosGetter();
            float frac = _fractionGetter != null ? _fractionGetter(pos) : 0f;
            PlaceAtScreenPoint(pos, _followDir, frac);
        }

        if (!_interactive) return;
        if (!_spawnFrameSkipped) { _spawnFrameSkipped = true; return; }

        // Space dismiss 파트는 좌클릭도 대체 허용 (가이드성 안내 파트만 해당 — Tab/ESC/None 파트는 클릭 무시).
        bool keyPressed = Input.GetKeyDown(_dismissKey);
        bool clickAsSpace = _dismissKey == KeyCode.Space && Input.GetMouseButtonDown(0);
        if (keyPressed || clickAsSpace)
        {
            // 레시피북 메뉴 카드가 열린 상태에선 Space/클릭 dismiss 차단.
            // - IsMenuCardActive(static bool): CloseMenuCard 호출 시 즉시 false. Instance null 체크보다 결정론적.
            if (_dismissKey == KeyCode.Space && MenuCardController.IsMenuCardActive) return;
            Dismiss();
        }
    }

    /// <summary>외부에서 강제 dismiss (조건부 dismiss용, 예: 레시피북 닫힘 이벤트).</summary>
    public void DismissExternally() => Dismiss();

    private void Dismiss()
    {
        if (!_interactive) return; // 중복 호출 방지 (event + key press 등)
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
