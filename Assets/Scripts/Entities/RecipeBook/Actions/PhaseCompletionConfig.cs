using System.Collections.Generic;

public static class PhaseCompletionConfig
{
    private static readonly Dictionary<PhaseType, PhaseCompletionRule> rules = new Dictionary<PhaseType, PhaseCompletionRule>
    {
        { PhaseType.Preparation, new PhaseCompletionRule(PhaseType.Preparation, CompletionMode.ALL, ActionType.MenuSelect, ActionType.PrepareIngredients) },
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
