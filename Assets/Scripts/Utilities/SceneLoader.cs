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

    /// <summary>
    /// 게임플레이 씬 전환. 현재 씬을 unload하고 새 씬을 additive로 로드.
    /// LoadingManager가 있으면 페이드 효과 사용.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadSceneAdditive(sceneName, currentGameplayScene);
        }
        else
        {
            // LoadingManager 없을 때 (Boot 초기 단계)
            var runner = GetCoroutineRunner();
            runner.StartCoroutine(LoadSceneDirectCoroutine(sceneName));
        }

        currentGameplayScene = sceneName;
    }

    /// <summary>
    /// Boot에서 초기 씬 로드 (unload 대상 없음)
    /// </summary>
    public static void LoadInitialScene(string sceneName)
    {
        currentGameplayScene = sceneName;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public static string CurrentScene => currentGameplayScene;

    private static IEnumerator LoadSceneDirectCoroutine(string sceneName)
    {
        if (!string.IsNullOrEmpty(currentGameplayScene) && currentGameplayScene != sceneName)
        {
            var unload = SceneManager.UnloadSceneAsync(currentGameplayScene);
            if (unload != null)
                while (!unload.isDone) yield return null;
        }

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    private static MonoBehaviour GetCoroutineRunner()
    {
        // Managers 씬에 있는 아무 MonoBehaviour 사용
        if (LoadingManager.Instance != null) return LoadingManager.Instance;
        if (ProgressSystem.Instance != null) return ProgressSystem.Instance;
        return Object.FindFirstObjectByType<MonoBehaviour>();
    }
}
