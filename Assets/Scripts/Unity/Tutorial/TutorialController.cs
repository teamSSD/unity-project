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

        Vector2 screenPos = ResolveScreenPos(part.targetKey) + part.screenOffset;
        _active.PlaceAtScreenPoint(screenPos, part.tailDirection, part.tailHorizontalFraction);

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
