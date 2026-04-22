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
        StatsSystem.Instance.OnTimeChanged += SetTargetTime;
        SetTargetTime(StatsSystem.Instance.GetHour(), StatsSystem.Instance.GetMinute());
        currentHourAngle  = targetHourAngle;
        currentFill       = targetFill;
        currentFillAngle  = targetFillAngle;
    }

    void OnDisable()
    {
        if (StatsSystem.Instance != null)
            StatsSystem.Instance.OnTimeChanged -= SetTargetTime;
    }

    private void SetTargetTime(int hour, int minute)
    {
        targetHourAngle = -((hour % 12) / 12f * 360f + (minute / 60f) * 30f) + hourHandOffset;

        int current = hour * 60 + minute;
        int start, end;

        if (TimeManager.Instance != null)
        {
            start = TimeManager.Instance.StartTimeMinutes;
            end   = TimeManager.Instance.EndTimeMinutes;
        }
        else if (ProgressSystem.Instance != null)
        {
            start = ProgressSystem.Instance.PhaseStartMinutes;
            end   = ProgressSystem.Instance.PhaseEndMinutes;
        }
        else return;

        targetFillAngle = -(current / 720f * 360f);
        targetFill      = Mathf.Clamp01((end - current) / 720f);

        // 페이즈/세션 전환 감지 → 중간에 0을 거치지 않고 즉시 스냅
        if (cachedEnd != end)
        {
            cachedEnd   = end;
            currentFill = targetFill;
        }
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
}
