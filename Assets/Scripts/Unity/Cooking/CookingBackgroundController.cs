using UnityEngine;

public class CookingBackgroundController : MonoBehaviour
{
    [SerializeField] private GameObject bgMorning;
    [SerializeField] private GameObject bgEvening;
    [SerializeField] private GameObject bgNight;

    void Start()
    {
        ApplyPhase(ProgressSystem.Instance?.phaseData?.Phase ?? PhaseType.Morning);

        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.OnPhaseChanged += ApplyPhase;
    }

    void OnDestroy()
    {
        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.OnPhaseChanged -= ApplyPhase;
    }

    private void ApplyPhase(PhaseType phase)
    {
        bool isMorning = phase == PhaseType.Preparation
                      || phase == PhaseType.Morning
                      || phase == PhaseType.Afternoon;
        bool isEvening = phase == PhaseType.Evening;
        bool isNight   = phase == PhaseType.Night;

        if (bgMorning) bgMorning.SetActive(isMorning);
        if (bgEvening) bgEvening.SetActive(isEvening);
        if (bgNight)   bgNight.SetActive(isNight);
    }
}
