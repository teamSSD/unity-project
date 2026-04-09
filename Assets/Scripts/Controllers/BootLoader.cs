using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 씬 진입점. Managers 씬을 additive 로드 후 GameStart 씬으로 전환.
/// </summary>
public class BootLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1. Managers 씬 additive 로드
        var managersOp = SceneManager.LoadSceneAsync("Managers", LoadSceneMode.Additive);
        while (!managersOp.isDone)
            yield return null;

        // 2. ManagerBootstrap 실행 (Managers 씬에 배치된 컴포넌트 or 코드 호출)
        ManagerBootstrap.EnsureAll();

        // 3. GameStart 씬을 additive 로드
        SceneLoader.LoadInitialScene("GameStart");
    }
}
