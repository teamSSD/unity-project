using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 씬 진입점. Managers 씬 → GameStart 씬 순차 로드 후 자기 자신(Boot) unload.
/// </summary>
public class BootLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1. Managers 씬 additive 로드
        var managersOp = SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);
        while (!managersOp.isDone)
            yield return null;

        // 2. Managers 씬을 active로 설정 후 매니저 생성 (Boot이 아닌 Managers에 배치)
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.Managers));
        ManagerBootstrap.EnsureAll();

        // 3. GameStart 씬 additive 로드 (완료 대기)
        var gameStartOp = SceneManager.LoadSceneAsync(SceneNames.GameStart, LoadSceneMode.Additive);
        while (!gameStartOp.isDone)
            yield return null;

        SceneLoader.SetCurrentScene(SceneNames.GameStart);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.GameStart));

        // 4. Boot 씬 자체 unload
        SceneManager.UnloadSceneAsync(SceneNames.Boot);
    }
}
