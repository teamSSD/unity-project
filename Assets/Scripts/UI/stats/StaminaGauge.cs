using System;
using UnityEngine;
using UnityEngine.UI;

enum GaugeState
{
    Fill, Low, Severe
}
public class LinearGauge : MonoBehaviour
{
    public Action onGaugeEnd;
    [SerializeField] private Image fillImage;
    [SerializeField] private Sprite fillState;
    [SerializeField] private Sprite lowState;
    [SerializeField] private Sprite severeState;
    [SerializeField] private int maxValue = 100;
    private GaugeState currentState;

    void OnEnable()
    {
        StatsSystem.OnStaminaChanged += UpdateGauge;
        UpdateGauge(StatsSystem.GetStamina());
    }

    void OnDisable()
    {
        StatsSystem.OnStaminaChanged -= UpdateGauge;
    }

    private void UpdateGauge(int currentValue)
    {
        if (fillImage == null) Debug.LogWarning("Gauge Image Unset");

        if (currentValue > 40)
        {
            if (currentState != GaugeState.Fill)
            {
                currentState = GaugeState.Fill;
                fillImage.sprite = fillState;
            }
        }
        else if (currentValue <= 40 && currentValue > 15)
        {
            if (currentState != GaugeState.Low)
            {
                currentState = GaugeState.Low;
                fillImage.sprite = lowState;
            }
        }
        else if (currentValue <= 15)
        {
            if (currentState != GaugeState.Severe)
            {
                currentState = GaugeState.Severe;
                fillImage.sprite = severeState;
            }
        }

        fillImage.fillAmount = (float)currentValue / maxValue;
    }
}
