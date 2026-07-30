#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor Play 모드에서만 DebugConsole 씬을 additive로 로드.
/// - DebugConsole.unity는 Build Settings에 없어 릴리즈 빌드에 미포함.
/// - Editor에서만 LoadSceneInPlayMode 로 Build Settings 우회.
/// </summary>
public static class DebugConsoleBootstrap
{
    private const string ScenePath = "Assets/Scenes/ForReal/DebugConsole.unity";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void LoadInEditor()
    {
        var s = SceneManager.GetSceneByPath(ScenePath);
        if (s.IsValid() && s.isLoaded) return;
        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
    }
}
#endif
