using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private RectTransform hourHand;
    [SerializeField] private Image fillImage;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float hourHandOffset = 0f;

    private float targetHourAngle;
    private float currentHourAngle;
    private float targetFill;
    private float currentFill;
    private float targetFillAngle;
    private float currentFillAngle;
    private int cachedEnd = -1;

    void OnEnable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats == null) return;

        stats.OnTimeChanged += SetTargetTime;
        SetTargetTime(stats.GetHour(), stats.GetMinute());
        currentHourAngle  = targetHourAngle;
        currentFill       = targetFill;
        currentFillAngle  = targetFillAngle;
    }

    void OnDisable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnTimeChanged -= SetTargetTime;
    }

    private void SetTargetTime(int hour, int minute)
    {
        targetHourAngle = -((hour % 12) / 12f * 360f + (minute / 60f) * 30f) + hourHandOffset;

        int current = hour * 60 + minute;
        if (!TryGetPhaseEndMinutes(out int end)) return;

        targetFillAngle = -(current / 720f * 360f);
        targetFill      = Mathf.Clamp01((end - current) / 720f);

        // 페이즈/세션 전환 감지 → 중간에 0을 거치지 않고 즉시 스냅
        if (cachedEnd != end)
        {
            cachedEnd   = end;
            currentFill = targetFill;
        }
    }

    private static bool TryGetPhaseEndMinutes(out int end)
    {
        // ProgressService owns the current phase range. During a sub-scene transition,
        // the previous scene's TimeManager can still exist when the next phase is set.
        // Reading that stale timer first collapses the Mall clock range to zero.
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress?.PhaseData != null)
        {
            end = progress.PhaseEndMinutes;
            return true;
        }

        // Keep inspector-configured TimeManager values only as an isolated-scene fallback.
        var timeManager = TimeManager.Instance;
        if (timeManager != null)
        {
            end = timeManager.EndTimeMinutes;
            return true;
        }

        end = 0;
        return false;
    }

    void Update()
    {
        currentHourAngle = Mathf.LerpAngle(currentHourAngle, targetHourAngle, Time.deltaTime * rotationSpeed);
        hourHand.localRotation = Quaternion.Euler(0, 0, currentHourAngle);

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * rotationSpeed);
        currentFillAngle = Mathf.LerpAngle(currentFillAngle, targetFillAngle, Time.deltaTime * rotationSpeed);
        if (fillImage != null)
        {
            fillImage.fillAmount = currentFill;
            fillImage.rectTransform.localRotation = Quaternion.Euler(0, 0, currentFillAngle);
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
