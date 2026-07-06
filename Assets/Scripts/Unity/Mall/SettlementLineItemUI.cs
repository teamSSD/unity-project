using TMPro;
using UnityEngine;

public class SettlementLineItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI amountText;

    public void Set(string label, int amount, bool isExpense)
    {
        labelText.text  = label;
        amountText.text = isExpense ? $"-{amount:N0}G" : $"+{amount:N0}G";
        amountText.color = isExpense ? UIColors.Expense : UIColors.Income;
    }

    public void SetNeutral(string label, string valueText)
    {
        labelText.text   = label;
        amountText.text  = valueText;
        amountText.color = Color.white;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
