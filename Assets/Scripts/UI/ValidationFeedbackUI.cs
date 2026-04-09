using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays visual feedback for order validation results
/// Shows grade, score, and reward with color-coded feedback
/// </summary>
public class ValidationFeedbackUI : MonoBehaviour
{
    public static ValidationFeedbackUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine currentFeedback;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Hide panel initially
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Show validation feedback with grade, score, and reward
    /// </summary>
    public void ShowFeedback(string grade, float accuracyScore, int reward, string message = "")
    {
        if (feedbackPanel == null)
        {
            // Fallback to console if UI not set up
            Debug.Log($"<color={GetGradeColor(grade)}>[Order Complete] Grade: {grade} | Score: {accuracyScore:F2} | Reward: {reward}원</color>");
            return;
        }

        // Cancel previous feedback if showing
        if (currentFeedback != null)
        {
            StopCoroutine(currentFeedback);
        }

        currentFeedback = StartCoroutine(ShowFeedbackCoroutine(grade, accuracyScore, reward, message));
    }

    private IEnumerator ShowFeedbackCoroutine(string grade, float accuracyScore, int reward, string message)
    {
        // Update text
        if (gradeText != null)
        {
            gradeText.text = grade;
            gradeText.color = GetGradeColorRGB(grade);
        }

        if (scoreText != null)
        {
            scoreText.text = string.IsNullOrEmpty(message) ? $"{accuracyScore:P0}" : message;
        }

        if (rewardText != null)
        {
            rewardText.text = $"+{reward}원";
        }

        // Fade in
        feedbackPanel.SetActive(true);
        if (canvasGroup != null)
        {
            yield return FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration);
        }

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (canvasGroup != null)
        {
            yield return FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration);
        }

        feedbackPanel.SetActive(false);
        currentFeedback = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
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

    /// <summary>
    /// Show simple message feedback
    /// </summary>
    public void ShowMessage(string message, float duration = 2.0f)
    {
        if (feedbackPanel == null)
        {
            Debug.Log($"[Feedback] {message}");
            return;
        }

        if (currentFeedback != null)
        {
            StopCoroutine(currentFeedback);
        }

        currentFeedback = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        if (gradeText != null) gradeText.text = "";
        if (scoreText != null) scoreText.text = message;
        if (rewardText != null) rewardText.text = "";

        feedbackPanel.SetActive(true);
        if (canvasGroup != null)
        {
            yield return FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration);
        }

        yield return new WaitForSeconds(duration);

        if (canvasGroup != null)
        {
            yield return FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration);
        }

        feedbackPanel.SetActive(false);
        currentFeedback = null;
    }
}
