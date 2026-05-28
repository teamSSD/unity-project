# Phase 3: GameState + Service 마이그레이션

## 목표

ADR-001 결정사항(POCO GameState + Slim Services)을 feature-by-feature로 적용. Singleton MonoBehaviour 위주의 매니저 패턴을 GameState/Service 패턴으로 분해. `self_rolled_singleton`, `awake_instance_hits` 게이트 0 달성.

## 선결 조건

- [x] Phase 2 완료 (asmdef, UniTask, Catalog, DOMAIN.md)
- [ ] **Phase 2 결과 실기 검증 완료** (사용자 플레이 + 회귀 없음 확인) ← **현재 대기 중**
- [ ] Phase 3 진입 승인

## Phase 3 진입 전 회귀 검증 (필수)

자산 1316개 이동 + Catalog 전환은 GUID 보존 기반이라 안전 추정이지만, 실기 검증 한 번 필요:

1. Boot → GameStart → New Game → Mall 진입
2. Cooking 씬 진입 → 손님 1명 주문 처리 → Settlement 정상
3. Mall → Shop → 재료 1개 구매 + UpgradeShop 진입 확인
4. Mall → Garden → 작물 1개 심기/수확
5. Mall → Delivery NPC 대화 → 주문 받기 → 조리/배달
6. 세이브 후 재실행 → 데이터 로드 정상 (특히 InventorySaveData, OrderSaveData, *UpgradeSaveData)
7. BGM/SFX 정상 재생 (Mall/Cooking/Night 시점별)
8. 씬 전환 페이드 정상

회귀 시 `refactor/2026-05` 되돌리고 카탈로그/자산 이동 디버깅.

## 현재 게이트 상태 (Phase 2 종료 시점)

| 게이트 | 현재 | 목표 | Phase |
|---|---:|---:|---|
| `self_rolled_singleton` | 8 | 0 | 3 |
| `awake_instance_hits` | 18 | 0 | 3 |
| `model_singleton_access` | 0 | 0 | 3 (3-A 후) |
| `hidden_dep_files` | 69 | (감소 목표) | 3 |

## Sub-Phase 분할 (위험도순)

### Phase 3-A: SingletonMonoBehaviour 패턴 통일 (자체 Instance → 공통 기반)

대상 8개 self-rolled singleton. **카테고리별 위험도 다름.**

#### 3-A-1: 안전군 (씬 수명, DontDestroyOnLoad 없음)
- `TimeManager` (Cooking 씬 전용)
- `ValidationFeedbackUI` (씬 수명)
- `MenuCardController` (씬 수명, 단 `instance` 필드 + `=>` 패턴 — 특수)

**위험**: 매우 낮음. `Awake()` 동작이 기반 클래스와 동일. `OnDestroy()` override 시 `base.OnDestroy()` 호출 필수.

#### 3-A-2: 위험군 (DontDestroyOnLoad 사용 — 설계 결정 필요)
- `CropDataManager`
- `FarmUpgradeManager`
- `StorageUpgradeManager`
- `ToolUpgradeManager`
- `UnifiedShopManager` (조건부 DontDestroyOnLoad)

**문제**: `SingletonMonoBehaviour<T>` 기반은 "Managers 씬 배치 전제 + DontDestroyOnLoad 불필요"라 명시. DontDestroyOnLoad 매니저 4개는:
- **Option A**: Managers 씬으로 이동 + DontDestroyOnLoad 제거 (CatalogProvider처럼)
- **Option B**: `OnSingletonAwake()`에서 `DontDestroyOnLoad(gameObject)` 호출 (기반 클래스 컨벤션 변경)
- **Option C**: 이 4개는 GameState 마이그레이션(3-B)에서 함께 POCO화 (Singleton 자체 제거)

**Phase 3 ADR-001 정신**: Singleton 의존성을 줄이는 게 목표 → **Option C 선호**. 3-A에서는 Option C 의존성 매니저는 건드리지 않음.

### Phase 3-B: Awake() init 순서 race 제거 (18 hits)

`Awake()/Start()/OnEnable()` 본문에서 타 Singleton의 `.Instance` 접근 18건. Unity 초기화 순서 비결정성으로 NRE 잠재 위험.

**전략**:
1. **Awake() 접근 5건**: `Start()`로 이동 (가장 안전)
2. **Start() 접근 9건**: 대부분 안전 (Awake보다 보장 강함). 필요한 것만 `OnEnable()` 또는 이벤트 구독으로 전환.
3. **OnEnable() 접근 4건**: Singleton의 `OnTimeChanged` 등 이벤트 구독. 안전한 패턴이라 유지.

**구체 파일** (`tmp/refactor-2026-05/data/di_awake_instance.tsv` 참조):
```
Awake/Start/OnEnable 18건 (file:line):
  GameStart.cs:11, MallSceneController.cs:21, SoundManager.cs:34,
  TestSceneInit.cs:5, CookingBackgroundController.cs:9,
  CookingSceneManager.cs:20, LowerShelf.cs:9, OrderTicketModel.cs:32,
  Refrigerator.cs:9, StatManager.cs:14, UpperShelf.cs:9,
  WaitingCustomer.cs:53, Farm.cs:25, ClockUI.cs:19, MoneyUI.cs:17,
  SettingsController.cs:24, StaminaGauge.cs:19, TooltipController.cs:38
```

**위험**: 중간. 잘못 옮기면 첫 프레임 NRE 또는 UI 초기 상태 어긋남.

### Phase 3-C: GameState 분리 (Singleton 매니저 → POCO + Slim Service)

ADR-001 본격 적용. Singleton MonoBehaviour 매니저들을 POCO `GameState` + 얇은 Service로 분해.

**대표 후보**: `InventoryManager` → `InventoryState (POCO) + InventoryService`
- POCO에는 Dictionary<FoodData, int> + 직렬화 메서드만
- Service에는 SaveManager 호출 + Catalog 조회 등 부수효과

**선결**: ADR-001 본 ADR 재검토 (현재 ACCEPTED 상태인지 확인) + 매니저 우선순위 결정.

### Phase 3-D: 검증 + 베이스라인 재측정

- 모든 EditMode 테스트 GREEN
- 핵심 시나리오 회귀 0
- 게이트: `self_rolled_singleton=0`, `awake_instance_hits=0`, `model_singleton_access=0`
- 베이스라인 라인 수 변동 ±5%

## 추정 시간

| sub-phase | 추정 | 최악 |
|---|---|---|
| 3-A-1 안전군 (3개) | 0.5일 | 1일 |
| 3-A-2 위험군 — Option 결정 후 (5개) | 2일 | 1주 |
| 3-B Awake race 제거 (18건) | 1-2일 | 1주 |
| 3-C GameState/Service 분해 (매니저당) | 2-3일/매니저 | 1주/매니저 |
| 3-D 검증 | 1-2일 | 1주 |

대형 작업. 매 sub-phase 후 회귀 검증 필수 (테스트 못하는 상황이면 진행 불가).

## 사용자 검토 시점

- **3-A-2 진입 전**: DontDestroyOnLoad 매니저 4개에 대해 Option A/B/C 결정
- **3-C 진입 전**: 매니저별 GameState/Service 분해 우선순위 (어느 매니저부터?)
- **3-C 각 매니저 PR 후**: 실기 회귀 검증
- **3-D 종료**: Phase 3 완료 승인

## 위험 + 완화

| 위험 | 영향 | 완화 |
|---|---|---|
| Singleton 변환이 init 순서 race 노출 | NRE, 첫 프레임 깨짐 | 3-A-1 먼저 (안전군), 3-B에서 Awake → Start 이동 후 3-A-2 진행 |
| DontDestroyOnLoad 제거 시 세이브 누락 | 데이터 영구 손실 위험 | 3-A-2는 GameState 분해(3-C)와 함께 처리, 중간 상태 금지 |
| 매니저 분해 중 이벤트 미구독 → 침묵 버그 | 동작 누락 (에러 없음) | 각 매니저별 회귀 시나리오 명시 후 진행 |
| GameState shape 미결 → 작업 지연 | 일정 누적 | ADR-001 본 ADR 재확정 후 진행, 임시 시작 금지 |

## 권장 진입 순서

```
Phase 2 회귀 검증 (사용자) ─→ 3-A-1 안전군 (3개) ─→ 3-B Awake race 제거 ─┐
                                                                          ├─→ 3-C GameState 분해
                                                3-A-2 위험군 (Option 결정) ┘
                                                                          ├─→ 3-D 검증 + 종료
```

3-A-1과 3-B는 병렬 가능 (의존성 적음). 3-A-2는 Option 결정 대기, 3-C는 위험군 결정 후 시작.
