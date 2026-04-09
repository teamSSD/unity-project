using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private RectTransform hourHand;
    [SerializeField] private RectTransform minuteHand;
    [SerializeField] private float rotationSpeed = 5f;

    private float targetMinuteAngle;
    private float targetHourAngle;
    private float currentMinuteAngle;
    private float currentHourAngle;

    void OnEnable()
    {
        StatsSystem.Instance.OnTimeChanged += SetTargetTime;
        SetTargetTime(StatsSystem.Instance.GetHour(), StatsSystem.Instance.GetMinute());
    }

    void OnDisable()
    {
        StatsSystem.Instance.OnTimeChanged -= SetTargetTime;
    }

    private void SetTargetTime(int hour, int minute)
    {
        targetMinuteAngle = -(minute / 60f) * 360f;
        targetHourAngle = -((hour % 12) / 12f * 360f + (minute / 60f) * 30f);
    }

    void Update()
    {
        currentMinuteAngle = Mathf.LerpAngle(currentMinuteAngle, targetMinuteAngle, Time.deltaTime * rotationSpeed);
        currentHourAngle = Mathf.LerpAngle(currentHourAngle, targetHourAngle, Time.deltaTime * rotationSpeed);

        minuteHand.localRotation = Quaternion.Euler(0, 0, currentMinuteAngle);
        hourHand.localRotation = Quaternion.Euler(0, 0, currentHourAngle);
    }
}
