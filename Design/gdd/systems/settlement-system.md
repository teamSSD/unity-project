# Settlement System

Aftertaste의 일일 정산 (수입/지출) 시스템. 하루 종료 시 Settlement 씬으로 전환, 카테고리별 in/out 을 요약 표시 후 PassDay 트리거.

관련 코드:
- `SettlementService` — POCO 정산 누적기 (`Assets/Scripts/Domain/Mall/SettlementService.cs`).
- `SettlementController` — Settlement 씬 UI (`Assets/Scripts/Unity/Mall/SettlementController.cs`).
- `SettlementExpenseAdapter` — IExpenseLog → Service 라우팅 (`Assets/Scripts/Unity/Common/SettlementExpenseAdapter.cs`).

## 1. SettlementService (POCO)

`Assets/Scripts/Domain/Mall/SettlementService.cs`.

### 1.1 상수

```csharp
public const int ManagementFee = 1000;
```
- **일일 관리비 1,000G**. PassDay 시 자동 차감.

### 1.2 상태

```csharp
private readonly Dictionary<string, int> incomeMap  = new();
private readonly Dictionary<string, int> expenseMap = new();
public int DayStartMoney { get; private set; }
```

- `incomeMap`/`expenseMap`: 카테고리 라벨 → 누적 금액.
- `DayStartMoney`: 하루 시작 시 스냅샷 (정산 UI에서 delta 계산 용도, 현재 UI 미사용).

### 1.3 API

```csharp
void AddIncome(string label, int amount);   // amount<=0이면 무시
void AddExpense(string label, int amount);  // 동일
IEnumerable<(string label, int amount)> GetIncomeEntries();
IEnumerable<(string label, int amount)> GetExpenseEntries();
int TotalIncome();
int TotalExpense();
void Reset(int currentMoney);   // PassDay 직전 → 스냅샷 후 클리어
```

`Reset` 은 `ProgressService.PassDay()` 안에서 관리비 차감 후에 호출됨 — 결과적으로 다음 날 첫 조회 시 `DayStartMoney` 는 관리비 차감 후 값.

## 2. 카테고리 (Income / Expense)

### 2.1 Income (수입)

**단일 진입점**: `CustomerManager.LogSessionSummary()` — `Assets/Scripts/Unity/Cooking/CustomerManager.cs:348`.

```csharp
if (sessionStats.TotalEarnings > 0)
    GameSessionRoot.Instance?.Settlement.AddIncome(
        PhaseToLabel(progress.PhaseData.Phase),
        sessionStats.TotalEarnings
    );
```

`PhaseToLabel(PhaseType)` — `CustomerManager.cs:353`.

| Phase | Label |
|-------|-------|
| Morning | `"아침 영업"` |
| Afternoon | `"점심 영업"` |
| Evening | `"저녁 영업"` |
| Night | `"야간 영업"` |
| (기타) | `"영업"` |

**주의**: 배달 (`OrderService.ConsumeBento`) 는 `_money.Add(reward)` 만 호출 — **Settlement에는 기록 안 됨** (현재 상태). 배달 수익은 Stats.money에는 반영되지만 정산 화면 수입에는 안 잡힘.

### 2.2 Expense (지출)

`IExpenseLog.Add(category, amount)` → `SettlementExpenseAdapter.Add` → `SettlementService.AddExpense`.

호출처 (모두 category 문자열 하드코딩):

| 호출처 | Category | 금액 |
|--------|----------|------|
| `PurchaseService.cs:86` (재료 구매 완료 시) | `"재료 구매"` | 구매 총액 |
| `ToolUpgradeService.cs:59` | `"업그레이드"` | `next.cost` |
| `StorageUpgradeService.cs:59` | `"업그레이드"` | `next.cost` |
| `FarmUpgradeService.cs:62` | `"업그레이드"` | `next.cost` |

세 업그레이드 서비스가 같은 `"업그레이드"` 카테고리를 씀 — Settlement UI에 하나의 항목으로 합쳐서 표시.

**관리비**: `SettlementController.BuildUI()` 에서 UI 렌더 시점에 명시 추가 (아래).

## 3. Settlement 씬 흐름

### 3.1 씬 전환 (진입)

경로 A: `ProgressService.PassPhase()` — Night phase 종료 시.
```csharp
if (pd.Phase == PhaseType.Night)
{
    SceneLoader.LoadScene(SceneNames.Settlement);
    return true;
}
```

경로 B: `PhaseActionSelector.[Rest]` — Night phase 에서 Rest 클릭 시 `PassPhase()` 호출 → 위 조건 매칭.

경로 C (튜토리얼): `MallSceneController.GoHome()` — Afternoon 페이즈 튜토리얼 마지막 Closing 스텝 완료 시.
```csharp
tc.Show(TutorialStepId.Closing, onDone: () => {
    tc.Complete();
    SceneLoader.LoadScene(SceneNames.Settlement);
});
```

### 3.2 SettlementController.Start()

`Assets/Scripts/Unity/Mall/SettlementController.cs:30`.

```csharp
BuildUI();
saveStatusText.text = "";
SaveRoutineAsync().Forget();
```

### 3.3 BuildUI (씬 진입 직후 렌더)

`SettlementController.cs:55`.

**Header**: `dayText.text = $"{PhaseData.Day}일차 정산"`.

**Income 섹션**:
- `Settlement.GetIncomeEntries()` 순회 → `SpawnLine(incomeContainer, label, amount, isExpense=false)`.
- `incomeTotalText.text = $"+{totalIncome:N0}G"`.

**Expense 섹션**:
- `Settlement.GetExpenseEntries()` 순회 → `SpawnLine(expenseContainer, label, amount, isExpense=true)`.
- 관리비 라인 추가: `SpawnLine(expenseContainer, "관리비", ManagementFee, true)` — **관리비는 UI 시점에만 추가되고 실제 차감은 PassDay 시 발생**.
- `expenseTotalText.text = $"-{totalExpense + ManagementFee:N0}G"`.

**Balance (하단 요약)**:
```csharp
int finalMoney = Stats.GetMoney() - ManagementFee;  // 관리비 미리 반영
int netChange  = totalIncome - totalExpense;
balanceText.text = $"{finalMoney:N0}G  ({sign}{netChange:N0})";
```

### 3.4 SaveRoutineAsync (씬 진입 프레임 다음)

`SettlementController.cs:46`.

```csharp
saveStatusText.text = "저장 중...";
await UniTask.Yield();
GameSessionRoot.Instance?.Progress.PassDay();  // 관리비 차감 + 저장 + Day++
saveStatusText.text = "아무 키나 눌러서 계속";
waitingForInput = true;
```

`PassDay()` 안에서:
1. `Stats.SubMoney(ManagementFee)` — **-1000G**.
2. `Day++`, `Phase = Preparation`.
3. `Stats.SetStamina(100)`.
4. `Inventory.AdvanceDay()` — 유통기한 감소.
5. `Weather.UpdateWeather(day)`.
6. `Settlement.Reset(currentMoney)` — 카테고리 클리어, 다음 하루 시작.
7. `SaveManager.SaveAll()`.

### 3.5 Update Loop (인풋 대기)

`SettlementController.cs:37`.

```csharp
if (waitingForInput && Input.anyKeyDown)
{
    waitingForInput = false;
    SceneLoader.LoadScene(SceneNames.Mall);
}
```

**아무 키** → Mall 씬 로드. 다음 Day의 Preparation 페이즈 시작.

## 4. 파산 (미구현)

`StatsService.SetMoney(int value)` — `Assets/Scripts/Domain/Common/StatsService.cs:99`.

```csharp
public void SetMoney(int value)
{
    var s = Stats; if (s == null) return;
    s.money = Mathf.Max(0, value);   // ← 0 이하로 안 떨어짐
    OnMoneyChanged?.Invoke(s.money);
}
```

- 관리비 차감이 잔액을 초과해도 그냥 0G로 clamp.
- **파산 게임오버 로직 없음**.
- 향후 파산 조건 도입 시 여기가 훅 지점.

## 5. SettlementLineItemUI

`SettlementController.SpawnLine(container, label, amount, isExpense)` — `SettlementController.cs:87`.

```csharp
var item = Instantiate(lineItemPrefab, container);
item.Set(label, amount, isExpense);
```

Prefab: `SettlementLineItemUI` (SerializeField 참조). 각 라인은 라벨/금액/± 기호로 렌더.

## 6. 요약 (수입 / 지출 카테고리 정리)

### 수입 (Income)
| Category | 발생 조건 |
|----------|-----------|
| 아침 영업 / 점심 영업 / 저녁 영업 / 야간 영업 | 각 phase Cooking 씬 종료 시, `sessionStats.TotalEarnings > 0` |
| (배달) | 현재 Settlement 미기록 — 향후 추가 여지 |

### 지출 (Expense)
| Category | 발생 조건 |
|----------|-----------|
| 재료 구매 | `PurchaseService` — Shop 씬에서 확정 구매 시 |
| 업그레이드 | Tool / Storage / Farm 세 서비스 공통 |
| 관리비 | Settlement UI에 자동 추가 (`ManagementFee = 1000G`) — 실제 차감은 `PassDay()` 에서 |
