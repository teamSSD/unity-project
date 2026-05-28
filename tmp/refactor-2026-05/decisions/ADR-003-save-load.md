# ADR-003: Save/Load Architecture

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Progressive → **ADR-001과 통합 진행**

## TL;DR (v2 결정 요약)

**ISaveable 중간 단계 폐기. GameState 직행.**
- `PersistenceService` 가 `GameState.persistent` 를 한 줄로 직렬화/역직렬화
- 매니저별 `GetSaveData/ApplySaveData` 는 매니저 자체와 함께 소멸
- piggyback 매니저 (RecipeDataManager, UnlockedFoodManager)는 PersistentData의 정식 필드로 흡수
- ADR-001의 GameState 마이그레이션 작업의 일부 (별도 단계 없음)

## v1과의 차이

| 항목 | v1 (보수) | v2 (수정) |
|---|---|---|
| 단계 | 2단계: ISaveable(단기) → GameState(장기) | **1단계**: GameState 직행 |
| ISaveable 인터페이스 | Phase 2 도입 | **폐기** (만들지 않음) |
| SaveManager | Phase 2에서 단순화, Phase 3에서 흡수 | **ADR-001 작업의 일부로 PersistenceService로 한 번에 교체** |
| piggyback 처리 | ISaveable로 정식화 (중간 단계) | PersistentData 필드로 직접 흡수 |

## Context

현재 SaveManager.cs (147라인):
- `SaveAll()`이 9개 매니저의 `.Instance.GetSaveData()` 호출 → GameSaveData 조립
- `LoadAll()`이 같은 9개에 `ApplySaveData()` 분배
- piggyback 매니저: RecipeDataManager/UnlockedFoodManager가 PhaseData에 얹혀 저장 (호출 순서 버그 이력 있음)
- 새 매니저 추가 = SaveManager 4곳 수정

ADR-001이 GameState로 가는데, ISaveable은 결국 버려질 중간 단계. 중간 단계 비용이 효과보다 큼.

## Decision

### 변화 후 구조

```csharp
// Schema/State (POCO, JSON 직렬화 대상)
[Serializable]
public class PersistentData {
    public PhaseData phase;
    public BasicStats stats;
    public InventorySaveData inventory;
    public OrderSaveData orders;
    public DeliveryQuestSaveData deliveryQuest;
    public ToolUpgradeSaveData toolUpgrades;
    public StorageUpgradeSaveData storageUpgrades;
    public FarmUpgradeSaveData farmUpgrades;
    public FarmTilesSaveData farmTiles;
    public UnlockedFoodSaveData unlockedFood;        // 신규 (piggyback 분리)
    public MenuSelectionSaveData menuSelections;     // 신규 (piggyback 분리)
}

// Schema/State (런타임 휘발성)
public class GameState {
    public PersistentData persistent;   // ← 직렬화 대상
    public SessionState session;        // ← 비직렬화 (이번 세션 한정)
    public KitchenState kitchen;
    public ShopState shop;
    public GardenState garden;
}

// Domain (Service, POCO)
public class PersistenceService {
    private readonly GameState _state;
    private readonly string _savePath;

    public PersistenceService(GameState state, string savePath) {
        _state = state;
        _savePath = savePath;
    }

    public void Save() {
        DataSaveUtil.SaveData(_state.persistent, _savePath);
    }

    public void Load() {
        _state.persistent = DataSaveUtil.LoadData(new PersistentData(), _savePath);
        // 알림은 GameSessionRoot가 GameEvent로 발행
    }

    public bool HasSave() => DataSaveUtil.HasFile<PersistentData>(_savePath);
}

// Unity (Adapter, MonoBehaviour)
// GameSessionRoot가 Awake에서 인스턴스화 + DI 진입점 역할만
```

### 매니저 → GameState 흡수 매핑

| 현재 매니저 데이터 | 흡수 위치 |
|---|---|
| StatsSystem (BasicStats) | `PersistentData.stats` |
| ProgressSystem (PhaseData) | `PersistentData.phase` (단, RecipeData/UnlockedFood piggyback 제거) |
| InventoryManager | `PersistentData.inventory` |
| OrderManager | `PersistentData.orders` + `GameState.session.activeOrders` |
| UnlockedFoodManager | `PersistentData.unlockedFood` (PhaseData에서 분리) |
| RecipeDataManager (메뉴 선택) | `PersistentData.menuSelections` (PhaseData에서 분리) |
| ToolUpgrade/StorageUpgrade/FarmUpgradeManager | `PersistentData.*Upgrades` |
| FarmTileStorage | `PersistentData.farmTiles` |
| CustomerManager 세션 통계 | `GameState.session.todayStats` (직렬화 X) |
| WeatherSystem | `PersistentData.phase.weather` 또는 `GameState.session.weather` |

### 데이터 마이그레이션 (기존 세이브 호환)

PersistenceService.Load 에 마이그레이션 로직 추가:
- 기존 `gamedata.json` (GameSaveData 형식) 발견 시 → PersistentData로 변환 → 새 형식으로 다시 저장
- piggyback 데이터(`phase.unlockedFood`, `phase.menuSelections`)도 새 필드로 이동
- 한 번 마이그레이션 후 정상 동작

## Consequences

### 긍정
- SaveManager 147 → ~30라인 (PersistenceService)
- 매니저별 GetSaveData/ApplySaveData 9쌍 모두 제거
- piggyback 자동 해결 (정식 필드)
- 새 데이터 추가 = PersistentData 필드 추가만
- 테스트: PersistentData/GameState 단위 테스트 자명

### 부정
- 단일 큰 마이그레이션 (ADR-001과 동시) — 회귀 위험 큼
- 기존 세이브 데이터 호환 마이그레이션 코드 1회 필요
- ADR-001 완료 전까지 SaveManager가 임시 잔존 (어쩔 수 없음)

### 마이그레이션 순서
1. Schema/State 정의 (PersistentData, GameState)
2. PersistenceService 작성 + 기존 GameSaveData → PersistentData 마이그레이션 로직
3. GameSessionRoot가 PersistenceService 인스턴스화
4. ADR-001의 feature 단위 매니저→Service 마이그레이션 시 해당 매니저의 GetSaveData/ApplySaveData도 제거 (데이터는 이미 PersistentData가 보유)
5. 모든 매니저 흡수 완료 후 SaveManager 파일 삭제

## Resolved Questions

1. ~~데이터 형식 (GameSaveData 유지 vs per-key bundle)~~ → **PersistentData (단일 POCO)**
2. ~~LoadOrder 결정 방식~~ → 불필요 (단일 객체 로드, 후속 알림 한 번)
3. ~~piggyback 분리 시점~~ → 마이그레이션 동시 분리

## Decision Required

- [x] ISaveable 폐기 + GameState 직행 동의
- [x] PersistentData 단일 POCO 형식
- [x] ADR-001 작업의 일부로 동시 진행
- [x] 기존 세이브 호환 마이그레이션 1회 수행
