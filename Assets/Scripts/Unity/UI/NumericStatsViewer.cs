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
        var stats = GameSessionRoot.Instance?.Stats;
        if (!subscribed || stats == null) return;
        stats.OnMoneyChanged -= UpdateMoney;
        stats.OnStaminaChanged -= UpdateStamina;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (subscribed || stats == null) return;
        stats.OnMoneyChanged += UpdateMoney;
        stats.OnStaminaChanged += UpdateStamina;
        subscribed = true;
    }

    private void RefreshAll()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats == null) return;
        UpdateMoney(stats.GetMoney());
        UpdateStamina(stats.GetStamina());
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
