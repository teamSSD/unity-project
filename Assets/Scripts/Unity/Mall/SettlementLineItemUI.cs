using TMPro;
using UnityEngine;

public class SettlementLineItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI amountText;

    private static readonly Color ColorIncome  = new Color(0.18f, 0.70f, 0.35f);
    private static readonly Color ColorExpense = new Color(0.85f, 0.25f, 0.25f);
    private static readonly Color ColorNeutral = Color.white;

    public void Set(string label, int amount, bool isExpense)
    {
        labelText.text  = label;
        amountText.text = isExpense ? $"-{amount:N0}G" : $"+{amount:N0}G";
        amountText.color = isExpense ? ColorExpense : ColorIncome;
    }

    public void SetNeutral(string label, string valueText)
    {
        labelText.text   = label;
        amountText.text  = valueText;
        amountText.color = ColorNeutral;
    }
}
