using System.Collections.Generic;

public enum CompletionMode
{
    ALL,    // 모든 액션 완료 필요 (영업 준비)
    SINGLE  // 하나의 액션만 완료 필요 (아침/점심/저녁/밤)
}

public class PhaseCompletionRule
{
    public PhaseType Phase { get; }
    public CompletionMode Mode { get; }
    public HashSet<ActionType> RequiredActions { get; }

    public PhaseCompletionRule(PhaseType phase, CompletionMode mode, params ActionType[] requiredActions)
    {
        Phase = phase;
        Mode = mode;
        RequiredActions = requiredActions != null ? new HashSet<ActionType>(requiredActions) : new HashSet<ActionType>();
    }
}
