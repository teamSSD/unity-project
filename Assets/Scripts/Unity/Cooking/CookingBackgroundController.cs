using UnityEngine;

public class CookingBackgroundController : MonoBehaviour
{
    [SerializeField] private GameObject bgMorning;
    [SerializeField] private GameObject bgEvening;
    [SerializeField] private GameObject bgNight;

    void Start()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        ApplyPhase(progress?.PhaseData?.Phase ?? PhaseType.Morning);
        if (progress != null) progress.OnPhaseChanged += ApplyPhase;
    }

    void OnDestroy()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null) progress.OnPhaseChanged -= ApplyPhase;
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
