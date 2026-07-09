using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays visual feedback for order validation results
/// Shows grade, score, and reward with color-coded feedback
/// </summary>
public class ValidationFeedbackUI : SingletonMonoBehaviour<ValidationFeedbackUI>
{
    [Header("UI References")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float fadeDuration = 0.5f;

    private CancellationTokenSource _currentCts;

    protected override void OnSingletonAwake()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }

    protected override void OnDestroy()
    {
        _currentCts?.Cancel();
        _currentCts?.Dispose();
        base.OnDestroy();
    }

    /// <summary>
    /// Show validation feedback with grade, score, and reward
    /// </summary>
    public void ShowFeedback(string grade, float accuracyScore, int reward, string message = "")
    {
        if (feedbackPanel == null)
        {
            // Fallback to console if UI not set up (feedbackPanel SerializeField 미할당 = 씬 설정 오류).
            Debug.LogWarning($"[ValidationFeedbackUI] feedbackPanel 미할당 — grade={grade} score={accuracyScore:F2} reward={reward}");
            return;
        }

        RestartFeedback(ct => ShowFeedbackAsync(grade, accuracyScore, reward, message, ct));
    }

    /// <summary>
    /// Show simple message feedback
    /// </summary>
    public void ShowMessage(string message, float duration = 2.0f)
    {
        if (feedbackPanel == null)
        {
            return;
        }

        RestartFeedback(ct => ShowMessageAsync(message, duration, ct));
    }

    private void RestartFeedback(Func<CancellationToken, UniTaskVoid> task)
    {
        _currentCts?.Cancel();
        _currentCts?.Dispose();
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        task(_currentCts.Token).Forget();
    }

    private async UniTaskVoid ShowFeedbackAsync(string grade, float accuracyScore, int reward, string message, CancellationToken ct)
    {
        if (gradeText != null)
        {
            gradeText.text = grade;
            gradeText.color = GetGradeColorRGB(grade);
        }

        if (scoreText != null)
            scoreText.text = string.IsNullOrEmpty(message) ? $"{accuracyScore:P0}" : message;

        if (rewardText != null)
            rewardText.text = $"+{reward}원";

        feedbackPanel.SetActive(true);
        if (canvasGroup != null)
            await FadeCanvasGroupAsync(canvasGroup, 0f, 1f, fadeDuration, ct);

        await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), cancellationToken: ct);

        if (canvasGroup != null)
            await FadeCanvasGroupAsync(canvasGroup, 1f, 0f, fadeDuration, ct);

        feedbackPanel.SetActive(false);
    }

    private async UniTaskVoid ShowMessageAsync(string message, float duration, CancellationToken ct)
    {
        if (gradeText != null) gradeText.text = "";
        if (scoreText != null) scoreText.text = message;
        if (rewardText != null) rewardText.text = "";

        feedbackPanel.SetActive(true);
        if (canvasGroup != null)
            await FadeCanvasGroupAsync(canvasGroup, 0f, 1f, fadeDuration, ct);

        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);

        if (canvasGroup != null)
            await FadeCanvasGroupAsync(canvasGroup, 1f, 0f, fadeDuration, ct);

        feedbackPanel.SetActive(false);
    }

    private async UniTask FadeCanvasGroupAsync(CanvasGroup group, float startAlpha, float endAlpha, float duration, CancellationToken ct)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            await UniTask.Yield(cancellationToken: ct);
        }
        group.alpha = endAlpha;
    }

    /// <summary>
    /// Get color name for Debug.Log
    /// </summary>
    private string GetGradeColor(string grade)
    {
        switch (grade)
        {
            case "S": return "yellow";
            case "A": return "green";
            case "B": return "cyan";
            case "C": return "orange";
            case "D": return "red";
            case "F": return "red";
            default: return "white";
        }
    }

    /// <summary>
    /// Get RGB color for UI
    /// </summary>
    private Color GetGradeColorRGB(string grade)
    {
        switch (grade)
        {
            case "S": return new Color(1f, 0.84f, 0f); // Gold
            case "A": return new Color(0f, 0.8f, 0f); // Green
            case "B": return new Color(0f, 0.5f, 1f); // Blue
            case "C": return new Color(1f, 0.65f, 0f); // Orange
            case "D": return new Color(1f, 0.3f, 0f); // Red-Orange
            case "F": return new Color(1f, 0f, 0f); // Red
            default: return Color.white;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
