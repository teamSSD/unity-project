using System;
using UnityEngine;

public class TempTimePhaseProvider : MonoBehaviour, TimePhaseProvider
{
    public static TempTimePhaseProvider Instance { get; private set; }

    public event Action OnPhaseChanged;

    public int CurrentPhaseIndex { get; private set; } = 0;

    public int TotalPhaseCount { get; private set; } = 5;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void NextPhase()
    {
        CurrentPhaseIndex++;
        int displayPhase = ((CurrentPhaseIndex - 1) % TotalPhaseCount) + 1;
        Debug.Log($"current Phase: {displayPhase}, cumulative Phase: {CurrentPhaseIndex}");

        OnPhaseChanged?.Invoke();
    }
}
