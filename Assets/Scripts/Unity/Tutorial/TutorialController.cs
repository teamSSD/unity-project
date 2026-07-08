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
            GameSessionRoot.Instance?.Tutorial?.MarkShown(stepId);
            SaveManager.SaveAll();
            onDone?.Invoke();
            return;
        }

        DestroyActive();

        var part = step.parts[idx];
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
        _active.SetDismissKey(part.dismissKey);

        // targetKey가 있으면 매 프레임 target 위치를 다시 계산 (카메라 이동/target 이동 대응).
        if (!string.IsNullOrEmpty(targetKey))
        {
            _active.EnableDynamicFollow(
                () => ResolveScreenPos(targetKey) + offset,
                dir,
                effectiveFraction);
        }

        OnPartShown?.Invoke(stepId, idx, part);

        _active.EnableInteractiveDismiss(() =>
        {
            _active = null;
            ShowPart(step, idx + 1, stepId, onDone);
        });
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
        SaveManager.SaveAll();
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
