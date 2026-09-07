#if AFTERTASTE_E2E
/// <summary>
/// 정책 기반 장기 E2E가 항상 같은 시작 상태에서 출발하도록 만든 전용 프로필.
/// fresh tutorial E2E와 구분되며, 런타임/브라우저에서 임의 호출할 수 없다.
/// </summary>
public static class AftertasteE2EProfileSetup
{
    private const int CampaignSeed = 42;

    public static bool IsCampaignProfile { get; private set; }

    /// <summary>새 게임 기본값이 적용된 직후 long-run 빌드에서만 호출한다.</summary>
#if AFTERTASTE_E2E_LONGRUN
    public static void ApplyAtNewGame(GameSessionRoot session)
    {
        if (IsCampaignProfile || session?.Stats == null || session.Progress?.PhaseData == null) return;

        session.Tutorial?.Complete();
        session.Stats.GetSaveData().immutableSeed = CampaignSeed;
        GameRandom.InitSession(CampaignSeed, CampaignSeed ^ 0x5F3759DF);
        GameRandom.InitDay(0);
        session.Weather?.UpdateWeather(0);
        IsCampaignProfile = true;
    }
#endif
}
#endif
