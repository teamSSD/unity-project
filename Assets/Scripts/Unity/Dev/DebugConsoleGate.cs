using UnityEngine;
using IngameDebugConsole;

/// <summary>
/// IngameDebugConsole은 dev 전용. 릴리즈 빌드에서 자동 스트립.
/// - Editor: 유지 (개발 중 로그 확인)
/// - Development Build: 유지 (배포 전 QA)
/// - Release Build: 씬 로드 후 DebugLogManager 파괴 (유저 노출 X)
///
/// 씬을 안 건드리는 이유: Managers 씬에 IngameDebugConsole 프리팹이 있고
/// DontDestroyOnLoad로 유지되기 때문에, 부팅 후 한 번만 파괴하면 됨.
/// </summary>
public static class DebugConsoleGate
{
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StripInRelease()
    {
        var mgr = DebugLogManager.Instance;
        if (mgr != null) Object.Destroy(mgr.gameObject);
    }
#endif
}
