using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 중앙 유틸. 모든 게임플레이 씬 전환은 이 클래스를 통해 수행.
/// Additive Scene Loading: Managers 씬은 유지, 게임플레이 씬만 교체.
/// </summary>
public static class SceneLoader
{
    private static string currentGameplayScene;
    private static Vector3? _mallReturnPosition;

    public static string CurrentScene => currentGameplayScene;
    public static Vector3? MallReturnPosition => _mallReturnPosition;
    public static void SetMallReturnPosition(Vector3 pos) => _mallReturnPosition = pos;
    public static void ClearMallReturnPosition() => _mallReturnPosition = null;

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
        LoadSceneInternal(sceneName, initAction: null);
    }

    /// <summary>
    /// 씬 전환 + 페이드 인이 끝난 뒤(화면 가려진 상태)에 무거운 초기화 실행.
    /// 게임 시작/세이브 로드처럼 디스크 I/O 동기 호출에 사용.
    /// </summary>
    public static void LoadSceneWithInit(string sceneName, System.Action initAction)
    {
        LoadSceneInternal(sceneName, initAction);
    }

    private static void LoadSceneInternal(string sceneName, System.Action initAction)
    {
        UIFlowController.CloseAllForSceneTransition();
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadSceneAdditive(
                sceneName,
                currentGameplayScene,
                onComplete: () => currentGameplayScene = sceneName,
                initAction: initAction);
        }
        else
        {
            // LoadingManager 없는 fallback: init을 우선 동기 실행 후 직접 로드
            initAction?.Invoke();
            LoadSceneDirectAsync(sceneName).Forget();
        }
    }

    private static async UniTaskVoid LoadSceneDirectAsync(string sceneName)
    {
        string previousScene = currentGameplayScene;

        if (!string.IsNullOrEmpty(previousScene) && previousScene != sceneName)
        {
            var scene = SceneManager.GetSceneByName(previousScene);
            if (scene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null) await unload;
            }
        }

        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        currentGameplayScene = sceneName;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }
}
