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
    [Tooltip("이 비율 이하로 떨어지면 Low 상태(스프라이트) 전환.")]
    [SerializeField, Range(0f, 1f)] private float lowThresholdRatio = 0.4f;
    [Tooltip("이 비율 이하로 떨어지면 Severe 상태(스프라이트) 전환.")]
    [SerializeField, Range(0f, 1f)] private float severeThresholdRatio = 0.15f;
    private GaugeState currentState;

    void OnEnable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats == null) return;

        stats.OnStaminaChanged += UpdateGauge;
        UpdateGauge(stats.GetStamina());
    }

    void OnDisable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnStaminaChanged -= UpdateGauge;
    }

    private void UpdateGauge(int currentValue)
    {
        if (fillImage == null)
        {
            Debug.LogWarning("Gauge Image Unset");
            return;
        }

        float ratio = (float)currentValue / maxValue;

        if (ratio > lowThresholdRatio)
        {
            if (currentState != GaugeState.Fill)
            {
                currentState = GaugeState.Fill;
                fillImage.sprite = fillState;
            }
        }
        else if (ratio > severeThresholdRatio)
        {
            if (currentState != GaugeState.Low)
            {
                currentState = GaugeState.Low;
                fillImage.sprite = lowState;
            }
        }
        else
        {
            if (currentState != GaugeState.Severe)
            {
                currentState = GaugeState.Severe;
                fillImage.sprite = severeState;
            }
        }

        fillImage.fillAmount = ratio;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
