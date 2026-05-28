# Master Review — 거시+미시 통합

_작성: 2026-05-28 / 입력: baseline.md, review_macro.md, review_micro_*.md (4건)_

> 이 문서는 Phase 1 코드 리뷰의 통합본. 개별 리뷰의 모든 상세는 원본 md를 참조. 여기서는 **cross-cutting 발견**과 **통합 우선순위**에 집중한다.

---

## 1. 가장 큰 발견 (Top 5)

### #1 — 선언된 아키텍처와 실제 구현의 8배 갭
| 항목 | DEV 원칙 v2.0 | 실제 | 갭 |
|---|---:|---:|---:|
| Singleton 매니저 | 2-3 | **26** | 8.6× |
| `.Instance` 호출 | 0 (Composition Root 외) | **318** | — |
| Awake/Start의 `.Instance` | 0 | **17** | — |
| Entities/ 안의 `.Instance` | 0 | **25 파일** | — |
| 테스트 커버리지 | (목표 명시 없음) | **7.6%** | — |

→ **원칙 문서는 살아있지만 강제 메커니즘이 없어 코드가 원칙을 따라가지 못함.** 이게 단일 가장 큰 문제.

### #2 — Composition Root가 GameStart에서 멈춤
- BootLoader → Managers 씬 → GameStart는 잘 정리됨 ✓
- 그러나 **씬 진입 후 컴포넌트로 DI 확장이 안 됨**
- 결과: 모든 Entities/ 컴포넌트가 Awake/Start에서 `Manager.Instance` 직접 조회 → race condition + 레이어 위반 동시 발생
- 가장 전형적 사례: `Refrigerator.cs:9`
  ```csharp
  private void Awake() => capacity = StorageUpgradeManager.Instance?.GetCurrentData("refrigerator")?.value ?? 7;
  ```
  한 줄에 4가지 위반 (Awake .Instance / Entities Manager 접근 / 매직 스트링 / 매직 넘버)

### #3 — 이벤트 누수 5+ 건 — 람다 클로저 패턴
- CustomerManager L199/200, L146 — 람다로 lifecycle/ticket 캡처, 구독 해제 불가
- CustomerLifecycle L71/72 — orderTicket 이벤트 정리 없음
- 게임 세션 길수록 누적 (반복되는 고객 생성 루프)
- **즉시 처리 가능, 비용 낮음, 효과 큼** — 가장 가성비 좋은 첫 작업

### #4 — "Unified" 라는 이름의 분산 패턴
- `UnifiedShopManager`는 UI 오케스트레이션만 통합
- 실제 도메인 로직은 `StorageUpgradeManager` / `ToolUpgradeManager` / `FarmUpgradeManager` 3개 자체 Singleton에 분산
- 3개 모두 거의 동일 패턴 반복 (LoadTable, GetCurrentData, GetNextData, IsMax, TryUpgrade, Save/Apply)
- **베이스 클래스(`UpgradeManagerBase<TConfig>`) 추출로 중복 50%+ 제거 가능**

### #5 — God Class — MenuCardController (594라인, Entities/ Singleton)
- 7개 책임 혼재: Header, Image, RecipeLine, IngredientList, LayoutAdjust, Tab, 자체 생명주기
- AdjustSpacing 62라인 / InitSlot 52라인 / PopulateRecipeLine 51라인
- CustomerManager 분할 사례 (Spawner+TicketController+Lifecycle)와 동일한 패턴으로 분해 가능

---

## 2. Cross-Cutting 패턴 (여러 파일에 반복)

### 패턴 A: "Awake/Start에서 글로벌 매니저로 자기 의존성 채우기"
- 17개 메서드 (Awake 5, Start 8, OnEnable 4)
- 대표: Refrigerator/UpperShelf/LowerShelf, Farm, WaitingCustomer, ClockUI/MoneyUI/StaminaGauge, TooltipController, SettingsController
- **해결 방향**: 씬 진입 컨트롤러(MallSceneController, CookingSceneManager 등)가 자식 컴포넌트에 `Configure(...)` 주입
- **인터페이스 추출 후보**: `ISoundPlayer`, `ITimerProvider`, `IStorageUpgradeProvider`, `IProgressProvider`, `IOrderProvider`

### 패턴 B: "자체 Singleton (베이스 미상속)"
- 8 파일: MenuCardController, CropDataManager, FarmUpgradeManager, StorageUpgradeManager, ToolUpgradeManager, UnifiedShopManager, TimeManager, ValidationFeedbackUI
- 라이프사이클 정책 불일치 (DontDestroyOnLoad 정책, OnDestroy 처리)
- **해결**: 단순 마이그레이션 — 기존 패턴을 `SingletonMonoBehaviour<T>` 상속으로 교체
- 일부는 아예 Singleton 정당화 어려움 (MenuCardController는 Entities/ 컴포넌트, ValidationFeedbackUI는 UI 한 컴포넌트)

### 패턴 C: "[SerializeField] 있는데 OnValidate 없음"
- **68 파일** (전체의 36%)
- DEV 원칙 명시 필수 규칙 위반
- 인스펙터 누락이 Editor 로드 시점에 감지 안 됨 → PlayMode에서 NRE
- **해결**: snippet 표준화 + 필수 SerializeField 검출 자동 생성기 (또는 Editor 도구)

### 패턴 D: "GetComponent<X> 있는데 [RequireComponent(typeof(X))] 부재"
- **112 건**
- 대부분 Unity 기본 컴포넌트 (SpriteRenderer, PolygonCollider2D, RectTransform, Renderer)
- 일부는 자체 컴포넌트 (SpeechBubble, GaugeUI, BentoBehavior, CookingToolDescription) — 실제 NRE 위험
- **해결**: 일괄 검사 후 [RequireComponent] 추가 또는 SerializeField로 명시 노출

### 패턴 E: "View가 매니저 6개를 직접 호출"
- 대표: `ShopDetailPanel.cs` — StatsSystem/Inventory/Settlement/Tool/Storage/Farm/UnifiedShop 직접 .Instance 12회
- View 한 클래스가 도메인 전체에 의존
- **해결**: `IBuyService`, `IUpgradeService` 같은 도메인 서비스 추출 후 주입

### 패턴 F: "유사 매니저 분립"
- 사운드: SoundManager + UISoundManager + GlobalButtonSfxManager (3개)
- 업그레이드: StorageUpgrade + ToolUpgrade + FarmUpgrade (3개)
- **해결**: 통합 또는 베이스 클래스 추출

### 패턴 G: "동일 시퀀스 중복"
- `GameStart.ProcessContinue() vs NewGame()` — Init Phase 1/2/3 거의 동일 30라인 중복
- **해결**: `IGameInitPhase` 추상화 + Phase 리스트 주입

### 패턴 H: "piggyback 매니저 데이터"
- RecipeDataManager / UnlockedFoodManager 데이터가 PhaseData에 얹혀 저장됨
- SaveManager에서 `PrepareForSave` / `LoadFromProgress` 특수 호출로 처리
- **해결**: 두 매니저가 자기 SaveData를 갖도록 분리 또는 PhaseData를 분해

---

## 3. 핫스팟별 발견 요약

### MenuCardController.cs (594라인)
- Singleton 제거 + 7개 책임 분할 (Presenter + Renderer 다수 + LayoutAdjuster + TabManager)
- RecipeDataManager.Instance 3회 제거 → IRecipeDataProvider 주입
- OnValidate 추가, Find() 패턴 → SerializeField 명시
- 추정 작업: **5-7일**

### CustomerManager.cs + cooking/ 컴포넌트
- **이벤트 누수 5건 즉시 수정** (Week 1, 2-3시간)
- SessionStatisticsTracker / DeliveryOrderProcessor / GameEndChecker 분리
- CustomerLifecycle → IDisposable 구현
- Singleton → DI (ISoundService, IPhaseProvider)
- 추정 작업: **누수만 즉시 / 전체 4주**

### UnifiedShopManager + shop/
- ShopDetailPanel `.Instance` 12회 → IBuyService/IUpgradeService 주입
- 4개 자체 Singleton → `SingletonMonoBehaviour<T>` 베이스 통일
- `UpgradeManagerBase<TConfig>` 추출 (3개 중복 제거)
- 추정 작업: **1-2 phases (5-10시간)**

### Entities/ 25파일 .Instance 위반
- **Critical**: Refrigerator/UpperShelf/LowerShelf (Awake .Instance) — 2시간
- **High**: Farm (Start에서 4개 Manager) — 4-5시간
- **High**: WaitingCustomer (Start에서 TimeManager 콜백) — 2시간
- **Med**: Bento/CookingTool/Order Model들 — 12시간
- **Low**: UI/Dialogue/Mall 진입점 — 15시간
- 추정 총 작업: **~39시간 (~2주 full-time)**

---

## 4. 통합 우선순위 (cross-cutting)

5단계 가중치 (High/Critical/Med/Low/Optional)로 묶음:

### Critical (즉시 / 1주 이내)
1. **이벤트 누수 5건 수정** (CustomerManager + CustomerLifecycle)
   - 비용: 2-3시간 / 효과: 메모리 누수 차단 / 검증: PlayMode 5분 반복 테스트로 누수 확인
2. **Refrigerator/UpperShelf/LowerShelf Awake .Instance 제거**
   - 비용: 2시간 / 효과: 초기화 race 제거 / 검증: BaseStorage.SetCapacity 주입 후 동작 확인

### High (단기 / 2-4주)
3. **자체 Singleton 8개 → `SingletonMonoBehaviour<T>` 베이스 통일**
   - 단순 마이그레이션, 라이프사이클 일관성 확보
4. **OnValidate 누락 68파일 일괄 추가** (스니펫 + 일괄 적용)
   - DEV 원칙 v2.0 필수 규칙 회복
5. **[RequireComponent] 누락 112건 일괄 정리**
   - 자체 컴포넌트 우선, Unity 기본 컴포넌트는 일괄
6. **ShopDetailPanel → IBuyService/IUpgradeService 주입** (Entities → Manager 위반 제거 첫 사례)
7. **UpgradeManagerBase<TConfig> 추출** (3개 중복 제거 + DI 가능 상태)

### Med (중기 / 1-2개월)
8. **MenuCardController 분할** (Singleton 제거 + 7개 책임 분리)
9. **CustomerManager 추가 분리** (SessionStatisticsTracker, DeliveryOrderProcessor, GameEndChecker)
10. **씬별 Composition Root 확장** (MallSceneController, CookingSceneManager가 자식 컴포넌트에 의존성 주입 책임)
11. **ISaveable 인터페이스 + 자동 등록** (SaveManager 단순화, piggyback 매니저 정리)
12. **InitPhase 추상화** (GameStart ProcessContinue/NewGame 중복 제거)
13. **사운드 매니저 3개 → 1개 통합** 또는 명확한 책임 분리 문서화
14. **OrderManager 위치 정리** (Entities/Mall/delivery → Managers/ 이동)

### Low (장기 / 분기)
15. **어셈블리 분리** (Game.Schema / Game.Model / Game.Runtime) — 레이어 강제 메커니즘 확립
16. **PlayMode 테스트 인프라 구축**
17. **이벤트 구독 자동 정리 패턴** (EventSubscriptionCleaner 또는 IDisposable 일반화)
18. **Magic Number 207건 정리** — 스크립트로 일괄 추출 후 SerializeField화
19. **Debug.Log 101건 정리** — 프로덕션 로깅 정책 수립

### Optional / 보류
- 3개 UpgradeManager 완전 통합 (UpgradeService 단일화) — 수익성 낮음, 큰 작업
- EventBus 도입 — 큰 결정, 별도 평가 필요

---

## 5. 측정 기반 목표 (Phase 2 마스터 플랜 입력값)

| 지표 | 현재 | 목표 (1차) | 측정 방법 |
|---|---:|---:|---|
| Singleton 매니저 수 | 26 | ≤15 | `harness/checks/04_antipatterns.sh` singleton_decl + base 사용 합산 |
| `.Instance` 호출 | 318 | ≤180 (-43%) | `instance_access` |
| Awake/Start의 `.Instance` | 17 | 0 | `di_safety.kv awake_instance_hits` |
| Entities/ 안 `.Instance` | 25 파일 | ≤10 파일 | `di_safety.kv model_singleton_access` |
| 자체 Singleton (베이스 미사용) | 8 | 0 | `di_safety.kv self_rolled_singleton` |
| OnValidate 누락 | 68 파일 | ≤20 | `di_safety.kv serialize_no_validate` |
| [RequireComponent] 누락 | 112 | ≤30 | `di_safety.kv getcomponent_no_require` |
| 함수 41+ 라인 | 24 (3.3%) | ≤12 (1.6%) | `method_lengths.tsv` |
| 함수 61+ 라인 | 2 (0.3%) | 0 | 동일 |
| 들여쓰기 ≥6 | 12 파일 | ≤4 파일 | `indent_depths.tsv` |
| 이벤트 누수 후보 | 28 파일 | ≤10 | `event_leaks.tsv` |
| 테스트 커버리지 프록시 | 7.6% (16/211) | 25%+ | `test_coverage.kv` |
| 가장 큰 파일 | 594 | ≤350 | `file_lines.tsv` |
| 매직 넘버 | 207 | ≤100 | `magic_numbers.kv` |
| Debug.Log | 101 | ≤30 | `antipatterns.tsv` |

> 이 목표값은 1차. Phase 2 마스터 플랜에서 단계별 PR마다 부분 목표를 분해.

---

## 6. 권장 작업 흐름

```
[Critical 1주]
  이벤트 누수 5건 + Storage Awake .Instance 3개
  → 회귀 검증: 쿠킹 씬 5분 플레이, 손님 ≥10명 처리, 메모리 모니터
                                ↓
[High 2-4주]
  Singleton 베이스 통일 (8개) + OnValidate 일괄 (68) + RequireComponent 일괄 (112)
  ShopDetailPanel DI + UpgradeManagerBase 추출
  → 회귀 검증: 상점/창고/도구/농장 업그레이드 전체 플레이
                                ↓
[Med 1-2개월]
  MenuCardController 분할 + 씬 Composition Root 확장
  ISaveable + InitPhase 추상화 + 사운드 통합
  → 회귀 검증: 세이브/로드/씬 전환 전체 플레이
                                ↓
[Low 분기]
  어셈블리 분리 + PlayMode 테스트 + 매직 넘버/로그 정리
```

각 단계 끝에 `bash tmp/refactor-2026-05/harness/run_all.sh` 재실행 → `reports/history/` 비교로 정량 진척 확인.

---

## 7. 다음 단계 (Phase 2)

이 마스터 리뷰를 입력으로 **Phase 2: 마스터 플랜**을 작성. 포함할 것:
- 위 5단계 우선순위를 PR 단위로 쪼개기
- 각 PR의 가설 / 측정 기준 / 검증 방법 / 회귀 방지 체크리스트
- 단계 간 의존성 (예: Composition Root 확장이 Entities/ 정리의 전제)
- 시간 추정 및 마일스톤
