using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SmoothMoneyText : MonoBehaviour
{
    private Text uiText;
    private int targetValue;
    private float currentDisplayValue;
    [SerializeField] private float smoothSpeed = 6f;

    void Awake()
    {
        uiText = GetComponent<Text>();
    }

    void OnEnable()
    {
        var stats = StatsSystem.Instance;
        if (stats == null) return;

        stats.OnMoneyChanged += SetTargetValue;
        int currentMoney = stats.GetMoney();
        SetTargetValue(currentMoney);
        currentDisplayValue = currentMoney;
    }

    void OnDisable()
    {
        var stats = StatsSystem.Instance;
        if (stats != null) stats.OnMoneyChanged -= SetTargetValue;
    }

    private void SetTargetValue(int newValue)
    {
        targetValue = newValue;
    }

    void Update()
    {
        currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * smoothSpeed);
        if (Mathf.Abs(currentDisplayValue - targetValue) < 1f)
            currentDisplayValue = targetValue;
        uiText.text = ((int)currentDisplayValue).ToString("N0");
    }
}
