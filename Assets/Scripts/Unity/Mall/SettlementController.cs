using Cysharp.Threading.Tasks;
using Game.Domain.Mall;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private Button continueButton;

    void Start()
    {
        // 이전 씬에서 InputField(도시락 이름 등) 사용 시 브라우저 IME가 켜져있으면
        // 정산 화면의 Input.anyKeyDown이 keystroke을 못 잡는다 (WebGL 한글 IME 이슈).
        // 명시적으로 IME off. (버그 9 재현 케이스)
        Input.imeCompositionMode = IMECompositionMode.Off;

        BuildUI();
        InitializeContinueButton();
        if (saveStatusText != null) saveStatusText.text = "";
        SaveRoutineAsync().Forget();
    }

    void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EUiTargetRegistry.Unregister("settlement.continue", continueButton);
#endif
        // IME 원상복구 — Start에서 Off 세팅한 게 static 프로퍼티라 씬 넘어가도 유지되어
        // 이후 InputField(도시락 이름 등)에서 한글 입력 안 되는 이슈 방지.
        Input.imeCompositionMode = IMECompositionMode.Auto;
    }

    void Update()
    {
        // IME/포커스 상관없이 진행 가능하도록 anyKey + 마우스 클릭 모두 인정.
        // Input.anyKeyDown은 이미 마우스 버튼 포함하지만, WebGL IME 활성 시 keystroke 유실 방어용.
        if (waitingForInput && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            ContinueToMall();
    }

    private async UniTaskVoid SaveRoutineAsync()
    {
        if (saveStatusText != null) saveStatusText.text = "저장 중...";
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

        // PassDay 내부 예외가 UniTaskVoid에 삼켜져 waitingForInput이 세팅 안 되던 이슈 (버그 9).
        // try/finally로 정산 화면에서 항상 다음으로 넘어갈 수 있게 보장 + 예외는 로그로 노출.
        try
        {
            GameSessionRoot.Instance?.Progress.PassDay();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SettlementController] PassDay 실패 (day={GameSessionRoot.Instance?.Progress?.PhaseData?.Day}): {ex}");
        }
        finally
        {
            if (saveStatusText != null) saveStatusText.text = "계속하기";
            waitingForInput = true;
            if (continueButton != null) continueButton.interactable = true;
#if AFTERTASTE_E2E
            // 저장/PassDay가 끝난 실제 입력 가능 시점에만 실제 Button을 노출한다.
            E2EUiTargetRegistry.Register("settlement.continue", continueButton);
#endif
        }
    }

    private void InitializeContinueButton()
    {
        if (saveStatusText == null) return;
        saveStatusText.raycastTarget = true;
        continueButton = saveStatusText.GetComponent<Button>();
        if (continueButton == null) continueButton = saveStatusText.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = saveStatusText;
        continueButton.interactable = false;
        continueButton.onClick.AddListener(ContinueToMall);
    }

    private void ContinueToMall()
    {
        if (!waitingForInput) return;
        waitingForInput = false;
        if (continueButton != null) continueButton.interactable = false;
        SceneLoader.LoadScene(SceneNames.Mall);
    }

    private void BuildUI()
    {
        var sm = GameSessionRoot.Instance?.Settlement;
        var ps = GameSessionRoot.Instance?.Progress;
        var ss = GameSessionRoot.Instance?.Stats;

        if (ps != null)
            dayText.text = $"{ps.PhaseData.Day}일차 정산";

        foreach (var (label, amount) in sm.GetIncomeEntries())
            SpawnLine(incomeContainer, label, amount, false);

        int totalIncome = sm.TotalIncome();
        incomeTotalText.text = $"+{totalIncome:N0}G";

        foreach (var (label, amount) in sm.GetExpenseEntries())
            SpawnLine(expenseContainer, label, amount, true);
        SpawnLine(expenseContainer, "관리비", SettlementService.ManagementFee, true);

        int totalExpense = sm.TotalExpense() + SettlementService.ManagementFee;
        expenseTotalText.text = $"-{totalExpense:N0}G";

        if (ss != null)
        {
            // ManagementFee는 PassDay에서 차감되므로 미리 반영
            int finalMoney = SettlementService.ProjectBalanceAfterManagementFee(ss.GetMoney());
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
