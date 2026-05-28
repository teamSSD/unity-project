using Cysharp.Threading.Tasks;
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
        LoadSceneAsync(sceneName).Forget();
    }

    /// <summary>
    /// Additive 씬 전환: fade-in → initAction → 이전 씬 unload → 새 씬 additive load → SetActiveScene → fade-out
    /// initAction은 화면이 불투명한 상태에서 실행돼야 하는 무거운 동기 초기화(예: SaveManager.LoadAll).
    /// </summary>
    public void LoadSceneAdditive(string sceneName, string previousScene, System.Action onComplete = null, System.Action initAction = null)
    {
        if (isLoading) return;
        LoadSceneAdditiveAsync(sceneName, previousScene, onComplete, initAction).Forget();
    }

    private async UniTaskVoid LoadSceneAdditiveAsync(string sceneName, string previousScene, System.Action onComplete, System.Action initAction)
    {
        isLoading = true;
        UILockManager.Lock(UILockManager.Owner.Loading);

        // Fade in
        canvas.enabled = true;
        await FadeAsync(0f, 1f, FADE_DURATION);

        // 페이드 인 직후 화면이 완전 가려진 상태에서 무거운 동기 초기화 실행
        if (initAction != null)
        {
            initAction.Invoke();
            await UniTask.Yield(); // 초기화 직후 한 프레임 양보
        }

        // Unload previous
        if (!string.IsNullOrEmpty(previousScene))
        {
            var scene = SceneManager.GetSceneByName(previousScene);
            if (scene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null) await unload;
            }
        }

        // Additive load
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        // 씬 활성화 = Awake/Start 일괄 실행 = 한 프레임 스파이크 가능.
        // 불투명한 동안 한 프레임 흘려보내 스파이크를 흡수.
        await UniTask.Yield();

        // Fade out
        await FadeAsync(1f, 0f, FADE_DURATION);

        canvas.enabled = false;
        isLoading = false;
        UILockManager.Unlock(UILockManager.Owner.Loading);
        onComplete?.Invoke();
    }

    private async UniTaskVoid LoadSceneAsync(string sceneName)
    {
        isLoading = true;
        UILockManager.Lock(UILockManager.Owner.Loading);

        // Fade in
        canvas.enabled = true;
        await FadeAsync(0f, 1f, FADE_DURATION);

        // Async load
        await SceneManager.LoadSceneAsync(sceneName);

        // Fade out
        await FadeAsync(1f, 0f, FADE_DURATION);

        canvas.enabled = false;
        isLoading = false;
        UILockManager.Unlock(UILockManager.Owner.Loading);
    }

    private async UniTask FadeAsync(float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            await UniTask.Yield();
        }
        canvasGroup.alpha = to;
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
