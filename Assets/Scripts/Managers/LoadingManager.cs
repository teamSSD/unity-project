using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : SingletonMonoBehaviour<LoadingManager>
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image progressBarFill;
    private TextMeshProUGUI loadingText;

    private bool isLoading = false;

    private const float FADE_DURATION = 0.3f;

    protected override void OnSingletonAwake()
    {
        BuildLoadingUI();
        canvas.enabled = false;
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Additive 씬 전환: 이전 씬 unload + 새 씬 additive load + SetActiveScene
    /// </summary>
    public void LoadSceneAdditive(string sceneName, string previousScene, System.Action onComplete = null)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAdditiveCoroutine(sceneName, previousScene, onComplete));
    }

    private IEnumerator LoadSceneAdditiveCoroutine(string sceneName, string previousScene, System.Action onComplete = null)
    {
        isLoading = true;
        UILockManager.Lock(UILockManager.Owner.Loading);

        // Fade in
        canvas.enabled = true;
        canvasGroup.alpha = 0f;
        progressBarFill.fillAmount = 0f;
        loadingText.text = "Loading...";

        float t = 0f;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Unload previous
        if (!string.IsNullOrEmpty(previousScene))
        {
            var scene = SceneManager.GetSceneByName(previousScene);
            if (scene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                    while (!unload.isDone) yield return null;
            }
        }

        // Additive load
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            progressBarFill.fillAmount = progress;
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            yield return null;
        }

        progressBarFill.fillAmount = 1f;
        loadingText.text = "Loading... 100%";
        yield return new WaitForSecondsRealtime(0.15f);

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        // Fade out
        t = 0f;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }

        canvas.enabled = false;
        isLoading = false;
        UILockManager.Unlock(UILockManager.Owner.Loading);
        onComplete?.Invoke();
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;
        UILockManager.Lock(UILockManager.Owner.Loading);

        // Fade in
        canvas.enabled = true;
        canvasGroup.alpha = 0f;
        progressBarFill.fillAmount = 0f;
        loadingText.text = "Loading...";

        float t = 0f;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Async load
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            progressBarFill.fillAmount = progress;
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            yield return null;
        }

        // 0.9 도달 — 100% 표시 후 씬 활성화
        progressBarFill.fillAmount = 1f;
        loadingText.text = "Loading... 100%";
        yield return new WaitForSecondsRealtime(0.15f);

        op.allowSceneActivation = true;

        // 씬 활성화 대기
        while (!op.isDone)
            yield return null;

        // Fade out
        t = 0f;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }

        canvas.enabled = false;
        isLoading = false;
        UILockManager.Unlock(UILockManager.Owner.Loading);
    }

    private void BuildLoadingUI()
    {
        // Canvas
        var canvasObj = new GameObject("LoadingCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        // Background (검은 화면)
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ProgressBar Container (하단 중앙)
        var barContainer = new GameObject("ProgressBar");
        barContainer.transform.SetParent(canvasObj.transform, false);
        var barContainerRect = barContainer.AddComponent<RectTransform>();
        barContainerRect.anchorMin = new Vector2(0.25f, 0.15f);
        barContainerRect.anchorMax = new Vector2(0.75f, 0.18f);
        barContainerRect.offsetMin = Vector2.zero;
        barContainerRect.offsetMax = Vector2.zero;

        // ProgressBar Background
        var barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(barContainer.transform, false);
        var barBgImage = barBgObj.AddComponent<Image>();
        barBgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = Vector2.zero;
        barBgRect.anchorMax = Vector2.one;
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;

        // ProgressBar Fill
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);
        progressBarFill = fillObj.AddComponent<Image>();
        progressBarFill.color = new Color(0.9f, 0.75f, 0.3f, 1f);
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillAmount = 0f;
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        // Loading Text (프로그래스 바 위)
        var textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        loadingText = textObj.AddComponent<TextMeshProUGUI>();
        loadingText.font = TMP_Settings.defaultFontAsset;
        loadingText.text = "Loading...";
        loadingText.fontSize = 28;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.25f, 0.18f);
        textRect.anchorMax = new Vector2(0.75f, 0.25f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

}
