using UnityEngine;
using UnityEngine.UI;

public class NumericStatsViewer : MonoBehaviour
{
    [SerializeField] private Text moneyText;
    [SerializeField] private Text staminaText;

    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshAll();
    }

    private void Start()
    {
        TrySubscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (!subscribed || StatsSystem.Instance == null)
            return;

        StatsSystem.Instance.OnMoneyChanged -= UpdateMoney;
        StatsSystem.Instance.OnStaminaChanged -= UpdateStamina;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || StatsSystem.Instance == null)
            return;

        StatsSystem.Instance.OnMoneyChanged += UpdateMoney;
        StatsSystem.Instance.OnStaminaChanged += UpdateStamina;
        subscribed = true;
    }

    private void RefreshAll()
    {
        if (StatsSystem.Instance == null)
            return;

        UpdateMoney(StatsSystem.Instance.GetMoney());
        UpdateStamina(StatsSystem.Instance.GetStamina());
    }

    private void UpdateMoney(int value)
    {
        if (moneyText != null)
            moneyText.text = $"G {value:N0}";
    }

    private void UpdateStamina(int value)
    {
        if (staminaText != null)
            staminaText.text = $"STA {value}";
    }
}
