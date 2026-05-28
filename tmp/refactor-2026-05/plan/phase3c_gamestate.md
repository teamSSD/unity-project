# Phase 3-C: GameState + Service 분해 — 실행 계획

ADR-001 Option B 실행 단계. **매니저 26개 → stateful 매니저 0개**, 상태는 GameState POCO에, 로직은 stateless Service에, MonoBehaviour는 5-7개 Unity Adapter로만.

## 우선순위 결정 (ADR 권고 vs 실용 판단)

**ADR-001 RESOLVED**: "Cooking → Shop → Garden+Mall → 잔여"

**여기서 적용한 실용 순서**: **Garden → Shop → Mall → Cooking** (역순)

### 왜 ADR 순서를 뒤집나

- **테스트 부담**: 사용자가 매번 실기 검증 → 작은 feature부터 검증하는 게 안전
- **회귀 격리**: 작은 feature 깨져도 다른 feature 영향 최소
- **패턴 정착**: 작은 작업으로 GameState/Service 패턴 익히고 큰 작업(Cooking)에 적용
- **Cooking이 메인 루프**: 마지막에 다루면 다른 feature가 안정 상태일 때 손댐
- **사이즈 비교** (라인 수):
  - Garden: 472 (7 파일) ← 최소
  - Shop: 839 (5 파일)
  - Mall: ~1500 (15+ 파일)
  - Cooking: ~3000+ (가장 큼) ← 최후

ADR-001 "워밍업 폐기" 정신은 존중하되, "테스트 가능한 단위로 작게 진행"이 더 안전한 거버넌스. 패턴 자체는 동일하므로 순서 차이가 결과 차이로 이어지지 않음.

## Sub-Phase 분할

### Phase 3-C-0: Foundation (인프라)

GameState POCO + GameSessionRoot + 첫 Service (SoundService).

**왜 SoundService 먼저**:
- 영구 상태 없음 (PersistentData 불필요) → GameState 모양 정의 부담 없음
- Cross-cutting (모든 feature가 사용) → 일찍 만들수록 후속 작업 빨라짐
- 3개 매니저 (SoundManager 149줄 + UISoundManager 9줄 + GlobalButtonSfxManager 54줄) → 1개 SoundService 통합
- 패턴 검증: "MonoBehaviour 매니저 → POCO Service" 1차 사례

**산출**:
- `Game.Schema/State/GameState.cs` (빈 껍데기 + SessionState + PersistentData)
- `Game.Unity/Common/GameSessionRoot.cs` (Composition Root, Managers 씬 배치)
- `Game.Domain/Sound/SoundService.cs` (POCO, AudioSource 풀을 어댑터로 받음)
- `Game.Unity/Common/SoundServiceAdapter.cs` (MonoBehaviour, AudioSource 보유 + SoundService 위임)
- 3개 사운드 매니저 제거 (또는 어댑터로 위임)

**위험**: 낮음. 사운드는 실패 시 침묵만 — 게임 진행 영향 없음.

### Phase 3-C-1: Garden feature 마이그레이션

- `CropDataManager` (41줄) — 작물 마스터 데이터 + 캐싱
- `FarmUpgradeManager` (94줄) — 농장 업그레이드 레벨 (영구 상태)
- `Farm` (180줄) + `FarmTile` (82줄) + `FarmTileStorage` (43줄) — 농장 상태 (영구 + 휘발)

**산출**:
- `Game.Schema/State/Garden/GardenPersistent.cs` (업그레이드 레벨, 농장 타일 상태)
- `Game.Schema/State/Garden/GardenSession.cs` (성장 타이머 등 휘발)
- `Game.Domain/Garden/FarmService.cs` (작물 심기/수확/성장 로직)
- `Game.Domain/Garden/FarmUpgradeService.cs` (업그레이드 가격/실행)
- `Game.Domain/Garden/CropCatalogService.cs` (CropSpriteCatalog 조회)
- `Game.Unity/Garden/FarmAdapter.cs` (MonoBehaviour, Farm 씬에서 GameState 참조)
- 기존 매니저 3개 제거

**위험**: 중. 영구 상태(업그레이드 레벨, 농장 타일) 직렬화 형식 변경 → 세이브 호환 작업 필수. `[FormerlySerializedAs]` 또는 마이그레이션 코드 필요.

**검증**: 작물 심기 → 시간 경과 → 수확 + 업그레이드 1개 실행. 세이브 후 재실행 → 농장 상태 복원.

### Phase 3-C-2: Shop feature 마이그레이션

- `UnifiedShopManager` (439줄) — UI 매니저 (가장 큼)
- `StorageUpgradeManager` (94줄), `ToolUpgradeManager` (102줄) — 업그레이드 레벨

**산출**:
- `Game.Schema/State/Shop/ShopPersistent.cs` (업그레이드 레벨 통합)
- `Game.Domain/Shop/UpgradeService.cs` (3 업그레이드 매니저 통합 — Storage + Tool + Farm)
- `Game.Domain/Shop/PurchaseService.cs` (재료 구매)
- `Game.Unity/Shop/ShopUIAdapter.cs` (UnifiedShopManager UI 부분만)
- 기존 매니저 3개 제거

**위험**: 중-높음. 3개 업그레이드 매니저 통합 → 직렬화 형식 변경 (3 → 1). FarmUpgrade까지 흡수.

### Phase 3-C-3: Mall feature 마이그레이션

- `OrderManager` (Mall) — 배달 주문 (영구 + 휘발)
- `DialogueManager` — 대화 진행
- `DeliveryNpcDialogueInteraction` (339줄) + 관련 클래스들 — NPC 상호작용

**산출**:
- `Game.Schema/State/Mall/MallPersistent.cs` (DeliveryQuest 진행도)
- `Game.Schema/State/Mall/MallSession.cs` (오늘 주문)
- `Game.Domain/Mall/OrderService.cs`, `DeliveryQuestService.cs`, `DialogueService.cs`
- 기존 매니저 제거

**위험**: 높음. NPC 대화 분기 + 주문 상태 복잡. 회귀 시나리오 많음.

### Phase 3-C-4: Cooking feature 마이그레이션 (최대 + 최후)

- `CustomerManager` + 분할된 `CustomerSpawner`/`CustomerLifecycle`/`OrderTicketController`
- `CookingSceneManager`, `MiniGameManager`, `StatManager`
- `BaseStorage` 시리즈 (Refrigerator, UpperShelf, LowerShelf)
- TimeManager (이미 SingletonMonoBehaviour 통일 완료)

**산출**:
- `Game.Schema/State/Cooking/CookingSession.cs` (활성 손님, 활성 주문, 활성 재료)
- `Game.Domain/Cooking/CustomerService.cs`, `OrderService.cs` (Cooking 측), `MiniGameService.cs`, `CookingService.cs`
- 기존 매니저 제거 (10+ 개)

**위험**: 최고. 메인 게임 루프. 모든 매니저 분해 시 회귀 시나리오 가장 많음.

### Phase 3-C-5: 잔여 + 마무리

- `StatsSystem`, `ProgressSystem` → GameState 흡수 (DontDestroyOnLoad 매니저들)
- `InventoryManager` → PersistentData 흡수
- `WeatherSystem`, `SettlementManager`, `UnlockedFoodManager`, `RecipeDataManager`, `RecipeBookManager` 등
- 마지막 `.Instance` 사용 제거
- 매니저 5-7개로 수렴 (Adapter들만 남음)

### Phase 3-C-6: 검증 + 종료

- 모든 EditMode 테스트 GREEN (가능하면 신규 추가)
- 게이트: `self_rolled_singleton=0`, `awake_instance_hits=0`, `model_singleton_access=0`
- `.Instance` 호출 수: 318 → < 30 (Composition Root에서만)
- 매니저 수: 26 → 5-7
- 베이스라인 라인 수 분포 비교

## 시작 전 결정 (ADR-001 Open Questions)

| 질문 | 결정 |
|---|---|
| GameState 단일 vs 도메인 분리 | **시작은 단일** — `GameState`에 `Garden/Shop/Mall/Cooking` sub-state 필드. 비대해지면 분리 검토 |
| DI 컨테이너 | **수동 Composition Root** — `GameSessionRoot.Awake()`에서 명시적 wiring |
| 공존 전략 | **어댑터 패턴** — 마이그레이션 중 기존 매니저는 새 Service를 위임 호출. 한 feature 끝나면 매니저 제거 |
| 상태 변경 알림 | **SO Event** (Phase 2-B에서 카탈로그 만들어짐) + C# event 혼용 |

## 추정 시간

| Sub-phase | 추정 | 회귀 시간 (사용자) |
|---|---|---|
| 3-C-0 Foundation + SoundService | 1-2일 | 5분 (BGM/SFX 들리는지) |
| 3-C-1 Garden | 2-3일 | 10분 (심기/수확/업그레이드/세이브) |
| 3-C-2 Shop | 3-5일 | 20분 (UI 전체) |
| 3-C-3 Mall | 1주 | 30분 (NPC 대화 분기 전체) |
| 3-C-4 Cooking | 1-2주 | 1시간 (전 게임 루프) |
| 3-C-5 잔여 | 3-5일 | 30분 |
| 3-C-6 검증 + 종료 | 2-3일 | — |
| **합계** | **약 4-6주** | 매 sub-phase 후 회귀 검증 |

## 위험 + 완화

| 위험 | 영향 | 완화 |
|---|---|---|
| 직렬화 형식 변경 → 세이브 깨짐 | 사용자 진행 손실 | `[FormerlySerializedAs]` + 마이그레이션 코드. 변경 전후 세이브 검증 필수 |
| 공존 기간 중 두 시스템 충돌 | 데이터 불일치 | 어댑터 패턴 — 기존 매니저가 Service에 위임, 새/구 코드가 동일 GameState 접근 |
| 큰 feature(Cooking) 분해 중 회귀 다발 | 게임 진행 불가 | 작은 feature(Garden)부터 패턴 정착 후 Cooking. 매 매니저당 commit + 검증 |
| Service 의존성 그래프 복잡 | DI 누락 → NRE | GameSessionRoot에 명시적 wiring. 누락 시 Awake에서 즉시 fail-fast |

## 진행 원칙

1. **한 매니저 == 한 commit + 한 회귀 검증** (배치 금지)
2. **commit 사이에 컴파일 + 콘솔 에러 0 보장**
3. **세이브 형식 변경 시 마이그레이션 코드 별도 commit**
4. **각 sub-phase 끝나면 머지 + 사용자 검증**
5. **회귀 발견 시 즉시 sub-branch 되돌리기**

## 다음 단계

→ **Phase 3-C-0 (SoundService 통합)** 즉시 시작.
