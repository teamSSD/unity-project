/// <summary>
/// 페이즈 시간 정보 제공 인터페이스.
/// ProgressService가 구현하여 가드닝 시스템에 주입.
/// </summary>
public interface TimePhaseProvider
{
    int CurrentPhaseIndex { get; }
    int TotalPhaseCount { get; }
    void NextPhase();
}
