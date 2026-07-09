using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 씬 진입점. Managers 씬 → GameStart 씬 순차 로드 후 자기 자신(Boot) unload.
/// </summary>
public class BootLoader : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        // 1. Managers 씬 additive 로드
        await SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);

        // 2. Managers 씬을 active로 설정 후 매니저 생성 (Boot이 아닌 Managers에 배치)
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.Managers));
        ManagerBootstrap.EnsureAll();

        // 3. GameStart 씬 additive 로드 (완료 대기)
        await SceneManager.LoadSceneAsync(SceneNames.GameStart, LoadSceneMode.Additive);

        SceneLoader.SetCurrentScene(SceneNames.GameStart);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.GameStart));

        // 4. Boot 씬 자체 unload (await하지 않음 — fire and forget)
        _ = SceneManager.UnloadSceneAsync(SceneNames.Boot);
    }
}
