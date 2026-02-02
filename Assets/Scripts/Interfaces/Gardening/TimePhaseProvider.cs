using UnityEngine;

public interface TimePhaseProvider
{
    int CurrentPhaseIndex { get; }
    int TotalPhaseCount { get; }

    public void NextPhase();
}
