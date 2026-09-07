using TMPro;
using UnityEngine;

public class SettlementLineItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI amountText;

    public void Set(string label, int amount, bool isExpense)
    {
        labelText.text  = label;
        labelText.color = UIColors.TextPrimary;
        amountText.text = isExpense ? $"-{amount:N0}G" : $"+{amount:N0}G";
        amountText.color = isExpense ? UIColors.Expense : UIColors.Income;
    }

    public void SetNeutral(string label, string valueText)
    {
        labelText.text   = label;
        labelText.color  = UIColors.TextPrimary;
        amountText.text  = valueText;
        amountText.color = UIColors.TextPrimary;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
