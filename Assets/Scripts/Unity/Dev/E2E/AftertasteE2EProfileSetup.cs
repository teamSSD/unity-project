using System;
using UnityEngine;

#if AFTERTASTE_E2E
/// <summary>
/// E2E 전용 테스트 프로필 준비 경계.
/// 실제 플레이를 시작하기 전 한 번만 사용하며, 이후의 메뉴/구매/이동/요리는
/// 브라우저 입력으로만 수행한다. 관측 bridge와 분리해 의도치 않은 상태 변경을 막는다.
/// </summary>
public sealed class AftertasteE2EProfileSetup : MonoBehaviour
{
    private const string ObjectName = "AftertasteE2EProfileSetup";
    private const int CampaignSeed = 42;
    private static bool _created;

    public static bool IsCampaignProfile { get; private set; }

    [Serializable]
    private sealed class SetupRequest
    {
        public string profile;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (_created) return;
        _created = true;
        DontDestroyOnLoad(new GameObject(ObjectName).AddComponent<AftertasteE2EProfileSetup>().gameObject);
    }

    /// <summary>window.AftertasteE2E.setup가 호출한다. profile=campaign만 허용한다.</summary>
    public void ReceiveSetup(string json)
    {
        var request = JsonUtility.FromJson<SetupRequest>(json);
        if (request?.profile != "campaign")
        {
            Debug.LogWarning("[AftertasteE2E] Unsupported test profile request.");
            return;
        }

        var session = GameStateReporter.CurrentSession;
        bool initialMallState = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneNames.Mall
                             && session?.Progress?.PhaseData?.Day == 0
                             && session.Progress.PhaseData.Phase == PhaseType.Preparation;
        if (IsCampaignProfile || !initialMallState || session?.Stats == null)
        {
            Debug.LogWarning("[AftertasteE2E] Campaign profile is allowed once from a Day 0 Mall New Game only.");
            return;
        }

        session.Tutorial?.Complete();
        TutorialController.ClearActiveForE2E();
        session.Stats.GetSaveData().immutableSeed = CampaignSeed;
        GameRandom.InitSession(CampaignSeed, CampaignSeed ^ 0x5F3759DF);
        GameRandom.InitDay(session.Progress.PhaseData.Day);
        session.Weather?.UpdateWeather(session.Progress.PhaseData.Day);
        SaveManager.SaveAll();
        IsCampaignProfile = true;
        Debug.Log("[AftertasteE2E] Campaign profile prepared (seed=42).");
    }
}
#endif
