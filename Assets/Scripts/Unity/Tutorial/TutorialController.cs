using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 스텝 오케스트레이터. Managers 씬 singleton.
/// - Catalog + TutorialBubble prefab + Overlay canvas 참조
/// - Show(stepId): 스텝의 여러 파트를 순차 표시 (space dismiss로 다음 파트)
/// - 마지막 파트 dismiss → MarkShown(stepId) + save
/// - Target 위치는 TutorialTarget 등록 key → GetScreenPosition
/// - 활성 bubble 1개만 유지
/// </summary>
public class TutorialController : SingletonMonoBehaviour<TutorialController>
{
    [SerializeField] private TutorialStepCatalog catalog;
    [SerializeField] private TutorialBubble bubblePrefab;
    [SerializeField] private RectTransform overlayCanvas;

    private static readonly Dictionary<string, TutorialTarget> _targets = new();
    private TutorialBubble _active;

    /// <summary>파트가 표시될 때 발화 (카메라 focus 등 부수 처리용).</summary>
    public event System.Action<int, int, TutorialStepPart> OnPartShown;

    /// <summary>key로 등록된 TutorialTarget 조회 (world position 필요할 때).</summary>
    public TutorialTarget GetTarget(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _targets.TryGetValue(key, out var t) ? t : null;
    }

    // ── Target registry ─────────────────────────────
    public static void RegisterTarget(string key, TutorialTarget t)
    {
        if (string.IsNullOrEmpty(key) || t == null) return;
        _targets[key] = t;
    }

    public static void UnregisterTarget(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _targets.Remove(key);
    }

    // ── State ───────────────────────────────────────
    public bool IsCompleted => GameSessionRoot.Instance?.Tutorial?.IsCompleted ?? true;
    public bool HasShown(int stepId) => GameSessionRoot.Instance?.Tutorial?.HasShown(stepId) ?? true;
    public bool CanShow(int stepId) => !IsCompleted && !HasShown(stepId);

    // ── Show step ──────────────────────────────────
    /// <summary>스텝의 여러 파트를 순차 표시. 모두 dismiss 후 MarkShown + save + onDone.</summary>
    public void Show(int stepId, System.Action onDone = null)
    {
        if (catalog == null || bubblePrefab == null || overlayCanvas == null)
        {
            Debug.LogError("[TutorialController] catalog/prefab/canvas 미할당");
            return;
        }
        var step = catalog.Get(stepId);
        if (step == null || step.parts.Count == 0)
        {
            Debug.LogWarning($"[TutorialController] step {stepId} 미존재/빈 파트");
            return;
        }
        ShowPart(step, 0, stepId, onDone);
    }

    private void ShowPart(TutorialStepData step, int idx, int stepId, System.Action onDone)
    {
        if (idx >= step.parts.Count)
        {
            // 튜토리얼 종료 시 모든 RecipeBook 튜토리얼 flag 해제.
            RecipeBookManager.TutorialForceOpen = false;
            RecipeBookManager.TutorialBlockOpen = false;
            GameSessionRoot.Instance?.Tutorial?.MarkShown(stepId);
            SaveManager.SaveAll();
            onDone?.Invoke();
            return;
        }

        DestroyActive();

        var part = step.parts[idx];

        // 파트 단위 flag 적용 — 다음 파트에서 override되거나 종료 시 해제.
        RecipeBookManager.TutorialForceOpen = part.forceRecipeBookOpen;
        RecipeBookManager.TutorialBlockOpen = part.blockRecipeBookOpen;
        _active = Instantiate(bubblePrefab, overlayCanvas);
        _active.SetContents(part.message);
        _active.SetImage(part.optionalImage);

        var targetKey = part.targetKey;
        var offset = part.screenOffset;
        var dir = part.tailDirection;
        var baseFrac = part.tailHorizontalFraction;
        bool autoFlip = part.autoFlipByScreenSide;

        System.Func<Vector2, float> effectiveFraction = (screenPos) =>
        {
            if (!autoFlip) return baseFrac;
            // Target이 스크린 우측 절반 → tail이 body 오른쪽에 붙게 (fraction > 0). 좌측 절반 → 음수.
            float sign = screenPos.x > Screen.width * 0.5f ? +1f : -1f;
            return sign * Mathf.Abs(baseFrac);
        };

        Vector2 initialScreenPos = ResolveScreenPos(targetKey) + offset;
        _active.PlaceAtScreenPoint(initialScreenPos, dir, effectiveFraction(initialScreenPos));

        // dismissOnRecipeBookClose 파트는 dismissKey를 None으로 (키 입력으로 안 넘어감).
        _active.SetDismissKey(part.dismissOnRecipeBookClose ? KeyCode.None : part.dismissKey);

        // 레시피북 안내 파트(강제 오픈 or 닫힘 감지)만 RecipeBook 창 위에 유지 (order 1200).
        // 그 외 파트는 부모 상속(1000) → RecipeBook(1100) 뒤로 밀림.
        _active.SetStayOnTopOfRecipeBook(part.forceRecipeBookOpen || part.dismissOnRecipeBookClose);

        // targetKey가 있으면 매 프레임 target 위치를 다시 계산 (카메라 이동/target 이동 대응).
        if (!string.IsNullOrEmpty(targetKey))
        {
            _active.EnableDynamicFollow(
                () => ResolveScreenPos(targetKey) + offset,
                dir,
                effectiveFraction);
        }

        // 레시피북 실제 닫힘 이벤트 훅 (카드 ESC 닫힘과 구분).
        System.Action bookClosedHandler = null;
        var bubbleRef = _active;
        if (part.dismissOnRecipeBookClose && RecipeBookManager.HasInstance)
        {
            bookClosedHandler = () => {
                if (bubbleRef != null) bubbleRef.DismissExternally();
            };
            RecipeBookManager.Instance.OnRecipeBookClosed += bookClosedHandler;
        }

        OnPartShown?.Invoke(stepId, idx, part);

        _active.EnableInteractiveDismiss(() =>
        {
            if (bookClosedHandler != null && RecipeBookManager.HasInstance)
                RecipeBookManager.Instance.OnRecipeBookClosed -= bookClosedHandler;
            _active = null;
            ShowPart(step, idx + 1, stepId, onDone);
        }, lockInput: !part.allowSceneInteraction);
    }

    private Vector2 ResolveScreenPos(string key)
    {
        if (!string.IsNullOrEmpty(key) && _targets.TryGetValue(key, out var t) && t != null)
            return t.GetScreenPosition();
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public void Complete()
    {
        GameSessionRoot.Instance?.Tutorial?.Complete();
        // 저장 상태만 완료로 바꾸면, 이미 표시 중인 bubble의 Tutorial lock은 남는다.
        // 새 게임 직후 E2E 프로필처럼 완료 상태를 강제하는 경로에서도 입력이 막히지 않게
        // 화면·레시피북 플래그·lock을 한 단위로 정리한다.
        RecipeBookManager.TutorialForceOpen = false;
        RecipeBookManager.TutorialBlockOpen = false;
        DestroyActive();
        SaveManager.SaveAll();
    }

    /// <summary>외부 조건으로 현재 활성 파트 강제 dismiss. mock 컨트롤러가 게임 이벤트 → 튜토리얼 진행 훅으로 사용.</summary>
    public void DismissActivePart()
    {
        if (_active != null) _active.DismissExternally();
    }

    private void DestroyActive()
    {
        if (_active != null)
        {
            Destroy(_active.gameObject);
            _active = null;
        }
    }
}
