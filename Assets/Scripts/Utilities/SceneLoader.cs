using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 중앙 유틸. 모든 게임플레이 씬 전환은 이 클래스를 통해 수행.
/// Additive Scene Loading: Managers 씬은 유지, 게임플레이 씬만 교체.
/// </summary>
public static class SceneLoader
{
    private static string currentGameplayScene;

    public static string CurrentScene => currentGameplayScene;

    /// <summary>
    /// BootLoader에서 초기 씬 이름 설정용 (로드 완료 후 호출)
    /// </summary>
    public static void SetCurrentScene(string sceneName)
    {
        currentGameplayScene = sceneName;
    }

    /// <summary>
    /// 게임플레이 씬 전환. 현재 씬을 unload하고 새 씬을 additive로 로드.
    /// currentGameplayScene은 비동기 완료 후에만 갱신.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadSceneAdditive(sceneName, currentGameplayScene, () =>
            {
                currentGameplayScene = sceneName;
            });
        }
        else
        {
            var runner = GetCoroutineRunner();
            if (runner != null)
                runner.StartCoroutine(LoadSceneDirectCoroutine(sceneName));
        }
    }

    private static IEnumerator LoadSceneDirectCoroutine(string sceneName)
    {
        string previousScene = currentGameplayScene;

        if (!string.IsNullOrEmpty(previousScene) && previousScene != sceneName)
        {
            var scene = SceneManager.GetSceneByName(previousScene);
            if (scene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                    while (!unload.isDone) yield return null;
            }
        }

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        currentGameplayScene = sceneName;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    private static MonoBehaviour GetCoroutineRunner()
    {
        if (LoadingManager.Instance != null) return LoadingManager.Instance;
        if (ProgressSystem.Instance != null) return ProgressSystem.Instance;
        return Object.FindFirstObjectByType<MonoBehaviour>();
    }
}
