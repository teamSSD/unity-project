# Settlement.unity — 정산 씬

> 파일: `Assets/Scenes/ForReal/Settlement.unity`
> 진입: Night 페이즈에서 `Progress.PassPhase()` 자동 로드, 또는 튜토리얼 `Closing` 완료 시.

## 역할 한 줄

**하루 정산 요약 화면**. 수입/지출 라인 아이템 목록 + 잔액 표시. "아무 키나 눌러서 계속" → PassDay + Mall 복귀.

## GameObject 계층

```
Settlement.unity (root)
├── EventSystem
├── Main Camera
├── Canvas                    (ScreenSpaceOverlay 예상)
│   ├── 배경                  Settlement 배경
│   ├── 테이블                정산 테이블 UI 컨테이너
│   │   ├── dayText           "N일차 정산"
│   │   ├── incomeContainer   수입 라인 아이템 부모
│   │   │   └── incomeTotalText  "+총액G"
│   │   ├── expenseContainer  지출 라인 아이템 부모
│   │   │   └── expenseTotalText "-총액G"
│   │   └── balanceText       "finalMoneyG (+netChange)"
│   └── SaveStatusText        "저장 중..." → "아무 키나 눌러서 계속"
```

## 붙어있는 스크립트

- `Game.Runtime::SettlementController` (Canvas 또는 테이블 GO)
- TextMeshProUGUI × 5 (dayText, incomeTotalText, expenseTotalText, balanceText, saveStatusText)
- EventSystem / StandaloneInputModule
- CanvasScaler / GraphicRaycaster

## SettlementController — 씬 컨트롤러

[`SettlementController.cs`](../../../Assets/Scripts/Unity/Mall/SettlementController.cs)

### 인스펙터 필드
```csharp
[SerializeField] TextMeshProUGUI dayText;
[SerializeField] TextMeshProUGUI incomeTotalText;
[SerializeField] Transform       incomeContainer;
[SerializeField] TextMeshProUGUI expenseTotalText;
[SerializeField] Transform       expenseContainer;
[SerializeField] TextMeshProUGUI balanceText;
[SerializeField] SettlementLineItemUI lineItemPrefab;   // 라인 아이템 템플릿
[SerializeField] TextMeshProUGUI saveStatusText;        // "저장 중..." → "아무 키나 눌러서 계속"
```

전 필드 필수 (OnValidate → `RequiredFieldValidator.Validate`).

### Start() — UI 빌드 + 저장 루틴 시작
```csharp
BuildUI();
if (saveStatusText != null) saveStatusText.text = "";
SaveRoutineAsync().Forget();
```

### BuildUI()
`GameSessionRoot.Instance.Settlement / Progress / Stats` 참조:

1. **Day 헤더**: `dayText = "{Day}일차 정산"`
2. **수입 라인**: `Settlement.GetIncomeEntries()` 순회 → 각 항목 `SpawnLine(incomeContainer, label, amount, false)`
   - `incomeTotalText = "+{totalIncome:N0}G"`
3. **지출 라인**: `Settlement.GetExpenseEntries()` 순회 → `SpawnLine(expenseContainer, label, amount, true)`
   - **관리비 라인 추가**: `SpawnLine(expenseContainer, "관리비", SettlementService.ManagementFee, true)`
   - `expenseTotalText = "-{totalExpense:N0}G"`
4. **잔액**: `finalMoney = Stats.GetMoney() - ManagementFee` (미리 반영)
   - `balanceText = "{finalMoney:N0}G  ({+/-}{netChange:N0})"`

### SaveRoutineAsync() — 저장 + 입력 대기
```csharp
saveStatusText.text = "저장 중...";
await UniTask.Yield();                       // 다음 프레임까지 양보
GameSessionRoot.Instance.Progress.PassDay();  // ← Day 증가 + 재초기화 트리거
saveStatusText.text = "아무 키나 눌러서 계속";
waitingForInput = true;
```

`PassDay()` 내부에서:
- Day 증가
- Stats 리셋 (스태미나/시간 초기화)
- ManagementFee 차감 (Money 감소)
- SaveManager.SaveAll() (저장 IO)
- 새 Day에 대한 Weather 갱신, 시드 재초기화 등

### Update() — 입력 감지
```csharp
if (waitingForInput && Input.anyKeyDown) {
    waitingForInput = false;
    SceneLoader.LoadScene(Mall);
}
```

## SettlementService — 도메인 로직

[`Assets/Scripts/Domain/Mall/SettlementService.cs`](../../../Assets/Scripts/Domain/Mall/SettlementService.cs)

- `GetIncomeEntries() → IEnumerable<(label, amount)>` — Order/Delivery/기타 수입
- `GetExpenseEntries() → IEnumerable<(label, amount)>` — Purchase, Upgrade 등의 지출
- `TotalIncome()` / `TotalExpense()`
- `ManagementFee` (static const) — 매일 고정 관리비

**입력 소스**: 하루 동안 `StatsMoneyAdapter` + `SettlementExpenseAdapter`를 통해 각 서비스가 기록. `GameSessionRoot.WireCatalogAndUpgrades()`에서 wire.

## PassDay 트리거 위치

- `SaveRoutineAsync()`에서 즉시 호출 → 유저가 정산 UI를 보는 사이에 이미 완료됨
- 따라서 표시되는 잔액은 "PassDay 후 값 (관리비 반영)"과 일치

## 종료 조건

**아무 키 (Input.anyKeyDown)** → `SceneLoader.LoadScene(Mall)`.
Mall Start 시 `Progress.PhaseData.Phase = Preparation`이므로 새 하루 첫 페이즈로 진입.

## 관련 시스템

- [Mall 씬](./scene-mall.md) — 진입/복귀 (Night → Settlement → Mall Preparation)
- [Cooking 씬](./scene-cooking.md) — Night 페이즈 종료가 자동으로 Settlement 로드
- `Progress.PassDay()` — Day 증가 + 재초기화 + 저장
- `SaveManager.SaveAll()` — gamedata.json
- `SettlementLineItemUI` prefab — `Set(label, amount, isExpense)` 인터페이스
