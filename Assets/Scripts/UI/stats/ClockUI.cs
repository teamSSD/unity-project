using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private RectTransform hourHand;
    [SerializeField] private RectTransform minuteHand;

    void OnEnable()
    {
        StatsSystem.OnTimeChanged += UpdateClock;
    }

    void OnDisable()
    {
        StatsSystem.OnTimeChanged -= UpdateClock;
    }

    private void UpdateClock(int hour, int minute)
    {
        float minuteAngle = -(minute / 60f) * 360f;
        minuteHand.localRotation = Quaternion.Euler(0, 0, minuteAngle);

        float hourAngle = -((hour % 12) / 12f * 360f + (minute / 60f) * 30f);
        hourHand.localRotation = Quaternion.Euler(0, 0, hourAngle);
    }
}
