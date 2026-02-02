using UnityEngine;

public class TempTimePhaseProvider : TimePhaseProvider
{
    public int CurrentPhaseIndex { get; private set; } = 0;
    public int TotalPhaseCount { get; private set; } = 5;

    public void NextPhase()
    {
        CurrentPhaseIndex = (CurrentPhaseIndex + 1) % TotalPhaseCount;
    }
}
