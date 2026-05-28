# ADR-001: State Architecture — Managers vs GameState

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Blocking Phase 2

## TL;DR (v2 결정 요약)

매니저 26개 → **stateful 매니저 0개**. 모든 상태는 `GameState` POCO에 모이고, 모든 로직은 stateless `Service`로 분해. Unity Adapter(MonoBehaviour 라이프사이클 hook만)는 5-7개로 잔존. Save는 `JsonUtility.ToJson(state.persistent)` 한 줄 (ADR-003 v2 참조).

**마이그레이션 순서: feature-first 직행** (워밍업 단계 폐기). Cooking → Shop → Garden + Mall → 잔여 순으로 GameState 흡수.

## Context (왜 결정해야 하나)

베이스라인이 보여주는 문제:
- **매니저 26개** (DEV 원칙 권장 2-3개의 8.6배)
- **`.Instance` 호출 318회** (전역 결합)
- **테스트 커버리지 7.6%** — 대부분 매니저가 MonoBehaviour + Singleton이라 Mock 불가
- **SaveManager 147라인이 9개 매니저를 알아야 조립 가능**
- **piggyback 매니저**: RecipeDataManager/UnlockedFoodManager가 PhaseData에 얹혀 저장 → 데이터 모델 이상 신호
- **Entities/ 25파일에서 `.Instance` 접근** — 3-Layer 위반의 근본 원인이 "내 상태를 어디서 찾지?" → 글로벌 조회

본질적 질문: **매니저 = 전역 상태 보관소** 모델이 옳은가? 아니면 **상태와 행위를 분리**해야 하나?

## Options

### A. 현 매니저 패턴 유지, DI로만 정리
- 26개 매니저 모두 유지
- `.Instance` 호출을 DI 주입으로 교체
- Composition Root 패턴 확장
- **장**: 변경 최소, 위험 낮음, 단기 효과 빠름
- **단**: 매니저 수 그대로 → 테스트성 한계, Save 복잡성 그대로, "어디에 어떤 상태가 있는지" 여전히 분산

### B. GameState POCO + Slim Services + Persistent Data SO (★ 추천)
- **GameState**: 세션 동안의 휘발성 상태 (활성 손님, 오늘 주문, 현재 페이즈, 세션 통계)
- **PersistentData**: 영구 데이터 (인벤토리, 진행도, 잠금 해제, 업그레이드 레벨)
- **Services** (DI 주입): stateless 로직 (BuyService, UpgradeService, CookingService, SaveService)
- **Unity Adapters** (MonoBehaviour): Unity 라이프사이클 hook, scene wiring
- 매니저 목표 수: **5-7개**
- **장**: Save 자동화 (한 줄), 테스트 폭증 (POCO 단위), 의존성 명료
- **단**: 큰 마이그레이션 (1-2개월), 단계별 코드 깨짐 위험, 패러다임 전환 비용

### C. ECS / DOTS
- 데이터 지향, 컴포넌트 + 시스템 + 엔티티
- **장**: 성능 우수, 데이터 중심, Unity 권장 방향
- **단**: 학습 곡선 크고 이 규모(15K라인)엔 과함, 기존 코드 거의 전면 재작성

## Recommendation: B

이유:
- 사용자가 명시: "거대한 레거시 청소엔 아키텍처 변경 필요"
- A는 6개월 뒤 같은 자리. 위반은 정리되지만 구조적 한계 동일
- C는 이 규모/일정에 과함
- B는 점진 마이그레이션 가능 + 효과가 모든 지표에 (`.Instance`, Save, 테스트, Entities 위반, piggyback) 동시 적중

### B 안의 구체화

#### 매니저 분류 (현재 → 목표)

| 현재 매니저 | 목표 처리 |
|---|---|
| StatsSystem, ProgressSystem | → **GameState** (세션 상태로 흡수) |
| InventoryManager | → **PersistentData** + InventoryService |
| OrderManager | → **GameState** (오늘 주문) + **PersistentData** (배달 퀘스트) |
| UnlockedFoodManager, RecipeDataManager | → **PersistentData** (잠금 해제, 메뉴 선택) |
| ToolUpgrade/StorageUpgrade/FarmUpgradeManager (3개) | → **PersistentData** + **UpgradeService** 단일화 |
| CropDataManager | → **PersistentData** + CropService |
| UnifiedShopManager | → **ShopService** (stateless) + UI view |
| SoundManager, UISoundManager, GlobalButtonSfxManager (3개) | → **SoundService** 단일화 |
| LoadingManager + SceneLoader (static) | → **SceneCoordinator** (MonoBehaviour 1개) |
| UIManager, HUDManager, SettingsUIManager | → **UICoordinator** + 각 UI 패널 컴포넌트 |
| TimeManager | → **GameClock** (Unity adapter, GameState.Time 갱신) |
| WeatherSystem | → **WeatherService** + GameState.Weather |
| SettlementManager | → **SettlementService** + GameState.TodaySettlement |
| ActionSelectionManager | → **ActionService** + GameState.SelectedActions |
| RecipeBookManager, RecipeLookupService | → **RecipeService** (static 가능) |
| ValidationFeedbackUI | → UI 컴포넌트 (Singleton 불필요, DI) |
| MenuCardController (자체 Singleton) | → UI 컴포넌트 분할 (Singleton 제거, MenuCardController 미시 리뷰 참조) |

**Stateful 매니저: 0개**. 모든 상태는 GameState. 로직은 Service (POCO).

**Unity Adapter (MonoBehaviour, 라이프사이클 hook만)**: 5-7개
- GameSessionRoot (Composition Root, GameState 보관, Service 인스턴스화)
- SceneCoordinator (씬 전환 + 페이드)
- SoundService Adapter (AudioSource 보유 — MonoBehaviour 필수)
- UICoordinator (HUD/Settings/Loading 진입점)
- GameClock (Update에서 Time 갱신)
- (옵션) InputService Adapter

이들은 "내부 상태"를 보유하지 않음. Service 호출 + GameState 참조만.

#### 데이터 흐름

```
[디스크] gamedata.json
   ↕ PersistenceService
[PersistentData POCO] (인벤토리, 진행도, 잠금, 업그레이드)
   ↓ 게임 시작 시 로드
[GameState POCO] (세션 동안 휘발성 + persistent 참조)
   ↓ DI 주입
[Services] (stateless 로직)
   ↓ 호출
[Unity Adapters / UI] (MonoBehaviour, 컴포넌트)
```

GameState는 한 클래스로 시작 (필드들이 도메인별 sub-state):
```csharp
public class GameState {
    public PersistentData persistent;   // 디스크 직렬화 대상
    public SessionState session;        // 세션 동안만
    public KitchenState kitchen;        // 쿠킹 씬 활성 시
    public ShopState shop;              // 상점 씬 활성 시
    // ...
}
```

도메인별 분리 여부는 Open Question.

#### Service 인터페이스 예시
```csharp
public interface IUpgradeService {
    UpgradeResult TryUpgrade(UpgradeKey key);
    UpgradeData GetCurrent(UpgradeKey key);
    UpgradeData GetNext(UpgradeKey key);
}

public class UpgradeService : IUpgradeService {
    private readonly GameState _state;
    private readonly UpgradeTable _table;  // ScriptableObject

    public UpgradeService(GameState state, UpgradeTable table) { ... }
    
    public UpgradeResult TryUpgrade(UpgradeKey key) {
        // pure logic, no .Instance, no MonoBehaviour
    }
}
```

#### Composition Root
- `GameSessionRoot` (MonoBehaviour, Managers 씬에 한 개)가 진입점
- Awake에서 GameState 생성, 모든 Service 인스턴스화, DI 컨테이너 또는 수동 주입
- 씬 진입 시 `MallSceneController` / `CookingSceneController` 등이 자기 자식 컴포넌트에 Service 주입

## Consequences

### 긍정
- `.Instance` 호출 318 → 거의 0 (DI 주입)
- Save = `JsonUtility.ToJson(state.persistent)` 한 줄
- 테스트 = POCO 인스턴스화 → 모든 Service 단위 테스트 가능
- piggyback 자동 해결 (PhaseData → PersistentData.Progress)
- Entities/ 위반 자동 해결 (Service 주입받으므로 .Instance 불필요)
- 매니저 수 26 → 5-7
- 새 기능 추가 시 매니저 추가 압력 사라짐

### 부정
- 마이그레이션 코드 변경 면적이 매우 큼 (모든 .Instance 호출 변경)
- 마이그레이션 중간 단계에서 "절반은 옛 방식, 절반은 새 방식" 공존 기간 길어짐
- 1-2개월 작업 + 회귀 검증 부담
- 팀에 패러다임 학습 비용 (DI, POCO 상태)

### 마이그레이션 전략 (개략)
- 단계 1: GameState/PersistentData 정의, GameSessionRoot 생성, 기존 매니저는 그대로 두고 새 골격만 구축
- 단계 2: 매니저 한 개씩 Service로 추출, 새 골격에 등록, 호출자는 점진 이동
- 단계 3: 모든 호출 이동 완료된 매니저 제거
- 매니저 우선순위: SoundManager 3개 통합 → Upgrade 3개 통합 → 작은 매니저들 → 큰 매니저들 (Stats/Progress/Inventory)
- 각 단계마다 회귀 테스트 + 베이스라인 재측정

## Open Questions

1. **GameState 단일 vs 도메인 분리** (KitchenState, ShopState 등)?
   - 단일: Save/Load 간단, 의존성 그래프 단순
   - 분리: 도메인별 격리 우수, 씬 진입/종료에 따라 일부만 로드 가능
   - **임시 추천**: 단일 GameState로 시작, 비대해지면 분리

2. **DI 컨테이너 도입 (예: VContainer, Zenject) vs 수동 Composition Root**?
   - 도입: 보일러플레이트 감소, 자동 wiring
   - 수동: 의존성 명시적, 디버그 쉬움, 외부 의존 없음
   - **임시 추천**: 수동으로 시작 (Composition Root 패턴 익숙해진 뒤 도입 검토)

3. **매니저 마이그레이션 순서** — **RESOLVED**:
   - **feature-first 직행** (워밍업 폐기). 순서: Cooking → Shop → Garden + Mall → 잔여
   - 작은 매니저 통합(사운드 3개 → 1개 등)은 해당 feature 작업 중 자연히 흡수

4. **마이그레이션 중 두 시스템 공존을 어떻게 견딜까**?
   - 어댑터 패턴: 기존 매니저가 새 Service를 위임 호출
   - 또는: 매니저 전체를 한 PR에 교체 (작은 매니저부터)

5. **State 변경 알림은 어떻게?**
   - 이벤트 (C# event / Action)
   - SO Event (Hipple 스타일)
   - Observer 등록
   - **연관**: ADR-002의 SO 도입 결정

## Decision Required

- [ ] **B 채택 동의** (또는 A/C 또는 수정)
- [ ] Open Question 1 (단일 vs 분리) — 시작 입장
- [ ] Open Question 2 (DI 컨테이너) — 도입 여부
- [ ] Open Question 3 (마이그레이션 순서)
- [ ] Open Question 4 (공존 전략)
- [ ] Open Question 5 (상태 알림 메커니즘)

답이 명확하지 않은 항목은 "임시 추천 채택, Phase 2/3에서 재검토"도 OK.
