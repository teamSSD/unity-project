using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumericStatsViewer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private Image moneyIcon;
    [SerializeField] private Image staminaIcon;

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
            moneyText.text = $"<b><color=#FFD700>G</color></b> {value:N0}";
    }

    private void UpdateStamina(int value)
    {
        if (staminaText != null)
            staminaText.text = $"{value}/100";
    }
}
