# Micro Review: UnifiedShopManager (+ shop 영역)

_Subagent 결과 — 442라인, 자체 Singleton, hidden dep 12, "통합"이라는 이름 vs 실제 분산_

## 1. 책임 분석 — 이름과 실제의 불일치

**UnifiedShopManager는 "통합"이지만 실제로는 UI 뷰 오케스트레이션만 담당.**

- **하는 일**: ShopBook 프리팹 스폰/표시, 탭 전환, 행(ShopListRow) 동적 생성, 상세 패널 제어
- **위임하는 일**: 실제 비즈니스 로직은 3개 독립 Singleton이 따로
  - `StorageUpgradeManager` (창고 업그레이드 상태 + TryUpgrade)
  - `ToolUpgradeManager` (도구 업그레이드 상태 + TryUpgrade)
  - `FarmUpgradeManager` (농장 업그레이드 상태 + TryUpgrade)

```
UnifiedShopManager (UI 오케스트레이션)
├── StorageUpgradeManager.Instance (독립 상태머신)
├── ToolUpgradeManager.Instance (독립 상태머신)
└── FarmUpgradeManager.Instance (독립 상태머신)
```

→ "통합"은 UI 흐름만. **도메인 로직은 완전히 분산**.

## 2. 식별된 안티패턴

### A. 자체 Singleton 4개 (UnifiedShopManager + 3개 Upgrade Manager)

**위치**: 
- UnifiedShopManager.cs:11-47
- StorageUpgradeManager.cs:4-17
- ToolUpgradeManager.cs:4-18
- FarmUpgradeManager.cs:4-16

```csharp
public class UnifiedShopManager : MonoBehaviour
{
    public static UnifiedShopManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        SpawnBook();
    }
}
```

**문제**:
1. `SingletonMonoBehaviour<T>` 베이스 미상속 — 4개 모두 수동 구현
2. OnDestroy 미구현 (NPE 위험)
3. DontDestroyOnLoad 정책 불일치 (UnifiedShopManager만 조건부)
4. 코드 중복

### B. Hidden Dependency Chain (.Instance 12회 in UnifiedShopManager)
```
L100  UISoundManager.Instance?.PlayUIBook()
L106  GlobalButtonSfxManager.Instance?.RegisterButtons()
L200  StatsSystem.Instance.GetDay()
L249/269  ToolUpgradeManager.Instance (2회)
L293/340/387  StatsSystem.Instance?.GetMoney() (3회)
L302/321  StorageUpgradeManager.Instance (2회)
L349/368  FarmUpgradeManager.Instance (2회)
```
- 직접 의존성 추적 어려움
- 런타임 NRE 위험 (혼합 — `.Instance?` vs `.Instance.`)
- Composition Root 부재

### C. ShopDetailPanel의 3-Layer 위반 (.Instance 12회)

**위치**: `Assets/Scripts/Entities/ItemShop/ShopDetailPanel.cs`

```csharp
// L122-134 (OnBuyClicked)
private void OnBuyClicked()
{
    if (itemFood == null || itemQty <= 0) return;
    int total = itemQty * itemUnitPrice;
    if (StatsSystem.Instance == null || StatsSystem.Instance.GetMoney() < total) return;
    if (InventoryManager.Instance != null && !InventoryManager.Instance.CanAcceptType(itemFood)) return;
    StatsSystem.Instance.SubMoney(total);
    SettlementManager.Instance?.AddExpense("재료 구매", total);
    InventoryManager.Instance?.AddFood(itemFood, itemQty);
    UnifiedShopManager.Instance?.NotifyItemPurchased(itemFood, itemQty);
}
```

```
ShopDetailPanel (View, Entities/)
├── StatsSystem.Instance
├── InventoryManager.Instance
├── SettlementManager.Instance
├── ToolUpgradeManager.Instance
├── StorageUpgradeManager.Instance
├── FarmUpgradeManager.Instance
└── UnifiedShopManager.Instance (콜백)
```
**위반 정도: 심각** (View가 6개 매니저 직접 호출)

## 3. 자체 Singleton 3개 (Upgrade) 분석

| Manager | 상태 | 책임 | Singleton 정당성 |
|---|---|---|---|
| StorageUpgradeManager | `Dictionary<string,int> levels` | CSV 파싱, 레벨 조회, 비용 검증, 업그레이드 | ✓ 전역 상태 필요 |
| ToolUpgradeManager | `Dictionary<string,int> toolLevels` | 동일 패턴 | ✓ 동일 |
| FarmUpgradeManager | `Dictionary<string,int> levels` | 동일 패턴 | ✓ 동일 |

**구조적 문제**: 3개가 거의 동일 패턴을 반복
- `LoadTable()` (CSV 파싱)
- `GetCurrentData()` / `GetNextData()` / `IsMax()` / `TryUpgrade()`
- `GetSaveData()` / `ApplySaveData()`

→ 베이스 클래스(`UpgradeManagerBase<TConfig>`) 추출로 코드 중복 제거 가능

**SingletonMonoBehaviour 미상속 이유 추정**:
- 작업 당시 "이미 작동 중인 패턴" 복사
- 일관성 (역설적으로 UnifiedShopManager도 미상속이라)

## 4. ShopDetailPanel .Instance 12회의 인젝션 전환

**현재 (안티패턴)**:
```csharp
ShopDetailPanel → StatsSystem.Instance.SubMoney()
```

**옵션 A: 서비스 인터페이스 주입**
```csharp
public class ShopDetailPanel : MonoBehaviour
{
    private IBuyService _buyService;
    public void Setup(IBuyService buyService) => _buyService = buyService;
    private void OnBuyClicked() => _buyService.PurchaseFood(itemFood, itemQty);
}
```

**옵션 B: Event/MessageBus 패턴**
```csharp
private void OnBuyClicked()
{
    EventBus.Emit(new BuyFoodEvent(itemFood, itemQty));
}
// 구독자: SettlementManager, InventoryManager 등이 각각 반응
```

## 5. 책임 분리 권고

### 권장 구조
```
Managers/ShopDomain/
├── IUpgradeService (인터페이스)
├── UpgradeService (구현: 3개 통합 또는 분리 유지)
└── ShopRepository (데이터 조회)

Managers/shop/
└── UnifiedShopManager (UI 오케스트레이션만, DI 주입)

Entities/ItemShop/
├── ShopDetailPanel (View, IUpgradeService 주입)
└── ShopListRow (View, Pure UI)

Composition Root (Managers 씬):
  → ShopDetailPanel.Setup(new UpgradeService())
```

### 핵심 추상화
1. **UpgradeManagerBase<TConfig>** — 3개 중복 제거 (Med, High impact)
2. **IBuyService / IUpgradeService** — Panel 의존성 주입 (High)
3. **SingletonMonoBehaviour<T> 베이스 통일** — 단순 마이그레이션 (Low, Med)

## 6. 우선순위 권고

| # | 항목 | 난이도 | 영향 | 우선순위 |
|---|---|---|---|---|
| 1 | ShopDetailPanel → IBuyService/IUpgradeService 주입 | Med | High | **High** |
| 2 | 4개 Manager → SingletonMonoBehaviour 베이스 통일 | Low | Med | **High** |
| 3 | UpgradeManagerBase<TConfig> 추출 (3개 중복 제거) | Med | Med | **High** |
| 4 | UnifiedShopManager → Composition Root 패턴 | Med | Low | Med |
| 5 | EventBus 도입 (.Instance 호출 제거) | High | Med | Med |
| 6 | 3개 UpgradeManager 통합 (UpgradeService 추상화) | High | High | Low (수익성 낮음, 큰 작업) |

### 추천 로드맵
- **Phase 1 (1-2시간)**: ShopDetailPanel `.Instance` → SerializeField/Setup 주입, 4개 → SingletonMonoBehaviour 베이스
- **Phase 2 (3-4시간)**: UpgradeManagerBase 추출, Composition Root 도입
- **Phase 3 (선택)**: EventBus, 테스트 커버리지

## 7. NRE 가드 현황 (혼재)
```csharp
UISoundManager.Instance?.PlayUIBook();        // ✓ Safe
GlobalButtonSfxManager.Instance?.RegisterButtons(...);  // ✓ Safe
UnifiedShopManager.Instance?.NotifyUpgradeApplied();   // ✓ Safe
// 그러나
StatsSystem.Instance.GetDay()    // ✗ L200 unsafe
StatsSystem.Instance.SubMoney()  // ✗ L129 unsafe
```
→ 모든 `.Instance` 접근에 `?.` 강제 또는 DI로 제거.

## 요약

UnifiedShopManager는 명목상 "통합"이지만 실제로는 UI 오케스트레이션만 통합. 도메인 로직은 3개 중복 Singleton에 분산. ShopDetailPanel의 3-Layer 위반(View가 6개 매니저 직접 호출)이 가장 심각. 4개 매니저 모두 SingletonMonoBehaviour 베이스 미상속.

**즉시 대응**: DI + Singleton 베이스 통일 + 코드 중복 제거.
