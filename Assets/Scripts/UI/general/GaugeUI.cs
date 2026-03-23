using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float animationSpeed = 5f;
    private float targetFillAmount = 1f;

    void Update()
    {
        if (fillImage != null && Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * animationSpeed);
        }
    }

    public void SetProgress(float current, float max)
    {
        if (max == 0) return;
        float progress = current / max;
        if (progress < 0) progress = 0;
        if (progress > 1) progress = 1;
        targetFillAmount = Mathf.Clamp01(progress);
    }

    public void SnapTo(float current, float max)
    {
        if (max == 0) return;
        float progress = current / max;
        if (progress < 0) progress = 0;
        if (progress > 1) progress = 1;

        targetFillAmount = Mathf.Clamp01(progress);

        if (fillImage != null)
        {
            fillImage.fillAmount = targetFillAmount;
        }
    }
}
