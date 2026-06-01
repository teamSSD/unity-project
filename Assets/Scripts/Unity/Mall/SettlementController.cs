using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SettlementController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Income")]
    [SerializeField] private TextMeshProUGUI incomeTotalText;
    [SerializeField] private Transform       incomeContainer;

    [Header("Expense")]
    [SerializeField] private TextMeshProUGUI expenseTotalText;
    [SerializeField] private Transform       expenseContainer;

    [Header("Balance")]
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Prefab")]
    [SerializeField] private SettlementLineItemUI lineItemPrefab;

    [Header("Save Status")]
    [SerializeField] private TextMeshProUGUI saveStatusText;

    private bool waitingForInput = false;

    void Start()
    {
        BuildUI();
        if (saveStatusText != null) saveStatusText.text = "";
        SaveRoutineAsync().Forget();
    }

    void Update()
    {
        if (waitingForInput && Input.anyKeyDown)
        {
            waitingForInput = false;
            SceneLoader.LoadScene(SceneNames.Mall);
        }
    }

    private async UniTaskVoid SaveRoutineAsync()
    {
        if (saveStatusText != null) saveStatusText.text = "저장 중...";
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        ProgressSystem.Instance.PassDay();
        if (saveStatusText != null) saveStatusText.text = "아무 키나 눌러서 계속";
        waitingForInput = true;
    }

    private void BuildUI()
    {
        var sm = SettlementManager.Instance;
        var ps = ProgressSystem.Instance;
        var ss = GameSessionRoot.Instance?.Stats;

        if (ps != null)
            dayText.text = $"{ps.phaseData.Day}일차 정산";

        foreach (var (label, amount) in sm.GetIncomeEntries())
            SpawnLine(incomeContainer, label, amount, false);

        int totalIncome = sm.TotalIncome();
        incomeTotalText.text = $"+{totalIncome:N0}G";

        foreach (var (label, amount) in sm.GetExpenseEntries())
            SpawnLine(expenseContainer, label, amount, true);
        SpawnLine(expenseContainer, "관리비", SettlementManager.ManagementFee, true);

        int totalExpense = sm.TotalExpense() + SettlementManager.ManagementFee;
        expenseTotalText.text = $"-{totalExpense:N0}G";

        if (ss != null)
        {
            // ManagementFee는 PassDay에서 차감되므로 미리 반영
            int finalMoney = ss.GetMoney() - SettlementManager.ManagementFee;
            int netChange  = totalIncome - totalExpense;
            string sign    = netChange >= 0 ? "+" : "";
            balanceText.text = $"{finalMoney:N0}G  ({sign}{netChange:N0})";
        }
    }

    private void SpawnLine(Transform container, string label, int amount, bool isExpense)
    {
        var item = Instantiate(lineItemPrefab, container);
        item.Set(label, amount, isExpense);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
