using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private bool reverse;
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
        float progress = Mathf.Clamp01(current / max);
        targetFillAmount = reverse ? 1f - progress : progress;
    }

    public void SnapTo(float current, float max)
    {
        if (max == 0) return;
        float progress = Mathf.Clamp01(current / max);
        targetFillAmount = reverse ? 1f - progress : progress;

        if (fillImage != null)
        {
            fillImage.fillAmount = targetFillAmount;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
