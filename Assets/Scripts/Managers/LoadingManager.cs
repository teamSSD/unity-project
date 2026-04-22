using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : SingletonMonoBehaviour<LoadingManager>
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;

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
        while (!op.isDone) yield return null;

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
    }

}
