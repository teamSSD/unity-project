using System.Collections.Generic;

public static class PhaseCompletionConfig
{
    private static readonly Dictionary<PhaseType, PhaseCompletionRule> rules = new Dictionary<PhaseType, PhaseCompletionRule>
    {
        // 모든 페이즈는 SINGLE 모드 (하나의 액션 완료로 페이즈 완료)
        { PhaseType.Morning, new PhaseCompletionRule(PhaseType.Morning, CompletionMode.SINGLE) },
        { PhaseType.Afternoon, new PhaseCompletionRule(PhaseType.Afternoon, CompletionMode.SINGLE) },
        { PhaseType.Evening, new PhaseCompletionRule(PhaseType.Evening, CompletionMode.SINGLE) },
        { PhaseType.Night, new PhaseCompletionRule(PhaseType.Night, CompletionMode.SINGLE) }
    };

    public static PhaseCompletionRule GetRule(PhaseType phase)
    {
        return rules.TryGetValue(phase, out var rule) ? rule : new PhaseCompletionRule(phase, CompletionMode.SINGLE);
    }
}
