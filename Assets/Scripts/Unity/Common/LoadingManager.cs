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
    /// Additive 씬 전환: fade-in → initAction → 이전 씬 unload → 새 씬 additive load → SetActiveScene → fade-out
    /// initAction은 화면이 불투명한 상태에서 실행돼야 하는 무거운 동기 초기화(예: SaveManager.LoadAll).
    /// </summary>
    public void LoadSceneAdditive(string sceneName, string previousScene, System.Action onComplete = null, System.Action initAction = null)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAdditiveCoroutine(sceneName, previousScene, onComplete, initAction));
    }

    private IEnumerator LoadSceneAdditiveCoroutine(string sceneName, string previousScene, System.Action onComplete, System.Action initAction)
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

        // 페이드 인 직후 화면이 완전 가려진 상태에서 무거운 동기 초기화 실행
        if (initAction != null)
        {
            initAction.Invoke();
            yield return null; // 초기화 직후 한 프레임 양보
        }

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

        // 씬 활성화 = Awake/Start 일괄 실행 = 한 프레임 스파이크 가능.
        // 불투명한 동안 한 프레임 흘려보내 스파이크를 흡수.
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
