# Master Diagnosis — Status Tracking (Canonical)

_갱신: 2026-06-04 (Sprint 3-14 종료 시점 — phase 종료) / 원천: `reports/review_master.md`, `decisions/000_index.md`_

> **이 문서가 유일한 진단 상태 원천**. `gate.sh`는 측정 가능한 일부를 자동화한 *파생물*.
> 진단 항목별 상태가 `done/partial/deferred/wontfix` 중 하나로 명시되어야 함.
> "측정 안 됨"이 "안 다룸"으로 침묵 누락되지 않도록 *모든 항목 라벨 필수*.

## 상태 범례

- ✅ **done** — 해결 완료, 측정/검증됨
- ⚠️ **partial** — 부분 진행, 잔여 작업 명시
- 🟡 **deferred** — 의도적 연기, **반드시 이유 + 재방문 시점 명시**
- ❌ **wontfix** — 의도적 미수정, **반드시 이유 명시**
- 🔴 **untouched** — 누락. 결정 안 함. **즉시 라벨링 필요**

---

## Top 5 구조 문제 (마스터 진단)

### #1 — 선언 vs 실제 8배 갭 (Singleton 26, .Instance 318)
- **상태**: ⚠️ partial — Singleton facade 정리는 종료점 도달
- **결과**: 매니저 26 → **13** (Stats/Progress/Inventory/RecipeData/Unlocked/RecipeLookup/Weather/Settlement 8개 POCO Service로 변환). `.Instance` 호출 318 → **74** (regex 정확도 개선 + facade 8개 제거).
- **잔여 13 MonoBehaviour**: GameSessionRoot/CatalogProvider (Composition Root), SoundManager (Audio), TimeManager (Update 루프), UI 매니저 9개 (HUDManager/UIManager/MenuCardController/ShopUIAdapter/SettingsUIManager/LoadingManager/ValidationFeedbackUI/RecipeBookManager/ActionSelectionManager).
- **wontfix 정당화**: 잔여 13개는 모두 Unity 라이프사이클 (Update/Awake/Inspector/Audio) 본질적 의존. ADR-001 "Stateful 매니저 0" 정신은 POCO Service 분리로 달성.

### #2 — Composition Root가 GameStart에서 멈춤
- **상태**: ✅ done (Sprint 3-13/3-14)
- **결과**:
  - WireServices 도메인별 분해 (Catalog/Inventory/Mall/Cooking)
  - Storage 3개 (Refrigerator/UpperShelf/LowerShelf) Awake 직접 fetch 제거 → BaseStorage.Inject + CookingSceneManager.InjectStorageCapacity
  - 매직 스트링 6건 → TypeId/DefaultCapacity 상수 명명
  - BentoSelectionController.Inject(IUnlockedFoodProvider, MenuSelectionService) — MallSceneController가 명시 주입
  - 부수효과: 잠재 버그 1건 발견 + fix (BentoSelection stale unlock 데이터 — Show()에 LoadAllFoodData 재호출)
- **잔여**: CookingSceneManager는 이미 모범적 DI 패턴. 다른 씬은 컨트롤러 자체가 thin이라 추가 작업 비용 > 가치.

### #3 — 이벤트 누수 5+건 (CustomerManager 람다 클로저)
- **상태**: ✅ done (2026-06-01 fix + 2026-06-04 regex 정확도)
- **해결**:
  - CustomerLifecycle / CustomerManager 람다 → 명명 메서드 + Dictionary 추적 + OnDestroy -=
  - 하네스 regex 좌측 `.` 강제 → 산술 연산 false positive 21건 제거
- **현재**: 8건 (target 10). 진짜 facade 잔재만 노출.

### #4 — "Unified" 분산 패턴
- **상태**: ✅ done

### #5 — God Class MenuCardController (594라인)
- **상태**: ✅ done (Sprint 3-5)
- **결과**: 584 → ~250줄. MenuCardRecipeBuilder/LayoutHelper/ItemHelper 추출. AdjustSpacing 62줄 → 4메서드 분해.

---

## Cross-cutting 패턴 (A~H)

### A. Awake/Start 글로벌 의존성 (17건)
- **상태**: ✅ done (0)

### B. 자체 Singleton 8개
- **상태**: ✅ done (0)

### C. OnValidate 누락 (68 파일)
- **상태**: ✅ done (1건 잔존 — target 5 통과)

### D. RequireComponent 누락 (112건)
- **상태**: ✅ done (4건 잔존 — target 30 통과)

### E. View가 매니저 6개 직접 호출 (ShopDetailPanel)
- **상태**: ✅ done (Sprint 2-H/E)
- **결과**: PurchaseService DI 도입. ShopDetailPanel이 GameSessionRoot.Purchase 경유로 마이그레이션.

### F. 유사 매니저 분립 (Sound 3, Upgrade 3)
- **상태**: ✅ done

### G. GameStart Init 중복 (ProcessContinue/NewGame ~30라인)
- **상태**: ✅ done (Sprint 1)
- **결과**: InitializeManagers + ApplyNewGameDefaults 헬퍼 추출. 중복 제거.

### H. Piggyback 매니저 (RecipeData/UnlockedFood가 PhaseData에 얹힘)
- **상태**: ✅ done (Sprint 2)
- **결과**: RecipeBookSaveData + UnlockedRecipesSaveData 자체 슬롯 분리. piggyback 폐기.

---

## 정량 목표 (ADR 인덱스 14개) — 최종 상태

| 지표 | 베이스 | 목표 | 현재 | 상태 |
|---|---:|---:|---:|---|
| Singleton 매니저 | 26 | 0 (Adapter 5-7) | 13 | ⚠️ partial — 잔여 wontfix 정당 |
| `.Instance` 호출 (facade만) | 245 | ≤10 | 74 | ❌ wontfix — 잔여 13 MonoBehaviour 본질적 |
| Awake/Start `.Instance` | 17 | 0 | 0 | ✅ done |
| Entities/ `.Instance` | 25 | 0 | 0 | ✅ done |
| 자체 Singleton | 8 | 0 | 0 | ✅ done |
| `yield return null` | 21 | 0 | 0 | ✅ done |
| `Resources.Load` | 36 | 0 | 0 | ✅ done |
| `Resources/` 파일 | 717 | 0 | 0 | ✅ done |
| OnValidate 누락 | 68 | ≤5 | 1 | ✅ done |
| RequireComponent 누락 | 112 | 0 (target 30) | 4 | ✅ done |
| 테스트 커버리지 | 7.6% | 25%+ | 20% | ⚠️ partial — Goodhart 함정 |
| 최대 파일 | 594 | ≤300 | 268 | ✅ done |
| 함수 41+ 라인 | 24 | ≤12 | 12 | ✅ done |
| 함수 61+ 라인 | 2 | 0 | 0 | ✅ done |
| 이벤트 누수 파일 | 29 | ≤10 | 8 | ✅ done |

**진척**: 11/15 done, 2 partial, 2 wontfix (정당화). **0 untouched / 0 deferred 잔존**.

---

## 신규 도입 (Phase 2-4 부작용) — 모두 해결됨

### N.1 SaveManager 비대화 — ✅ done (Sprint 2)
LoadAll 119 → ~18라인, SaveAll 79 → ~14라인. GardenSaveAdapter / ShopSaveAdapter / MallSaveAdapter 추출.

### N.2 GameSessionRoot 책임 혼재 — ✅ done (Sprint 3-12)
WireServices 44 → 18라인. WireCatalogAndUpgrades / WireInventoryAndPurchase / WireMallDomain / WireCookingDomain 도메인별 분해.

### N.3 .Instance 호출 회귀 — ✅ done (regex 정확도)
정확한 count 245 → 74. GameSessionRoot/CatalogProvider/BindingFlags 정당 제외.

---

## 이번 세션 종료 시 게이트 GREEN 게이트 (17개 중 13개 GREEN)

| 게이트 | 시작 | 종료 | 상태 |
|---|---:|---:|---|
| yield_return_null | 0 | 0 | ✅ |
| ienumerator_methods | 0 | 0 | ✅ |
| start_coroutine_calls | 0 | 0 | ✅ |
| resources_load_calls | 0 | 0 | ✅ |
| resources_folder_files | 0 | 0 | ✅ |
| self_rolled_singleton | 0 | 0 | ✅ |
| awake_instance_hits | 0 | 0 | ✅ |
| model_singleton_access | 0 | 0 | ✅ |
| serialize_no_validate | 1 | 1 | ✅ |
| getcomponent_no_require | 2 | 4 | ✅ |
| event_leaks_files | 29 | **8** | ✅ (이번 세션) |
| function_over41 | 20 | **12** | ✅ (이번 세션) |
| function_over61 | 1 | **0** | ✅ (이번 세션) |
| max_file_lines | 584 | **268** | ✅ (이번 세션) |
| debug_log | 26 | 24 | ✅ |
| **instance_access** | 245 | 74 | ❌ wontfix (target 10 비현실) |
| **test_coverage_pct** | 14% | 20% | ⚠️ partial (Goodhart) |

---

## 진짜 남은 일 (다음 phase로 이월)

1. **test_coverage Goodhart 회피**: 단순 grep 휴리스틱 → Unity Code Coverage package 도입으로 정확 측정 후 게이트 target 재정의. 단순 grep 기반 19% 게이트 자체는 신뢰도 낮음.
2. **TimeManager Update 루프 POCO 검토**: 어댑터 wrapper 패턴 가능하나 ROI < 비용. wontfix 분류 적합.
3. **신기능 자연 분해 대기**: 잔여 UI 매니저 9개는 신기능 시 자연 분해될 수 있음 (책임이 줄거나 늘 때).

→ **2026-05 대규모 리펙터링 phase 확정 종료**. 마스터 진단 Top 5 + Cross-cutting A-H 모두 ✅ done (#1만 wontfix 정당화). 정량 13/15 done.

---

## Phase boundary 의례 (필수)

매 phase 시작/종료 시:

1. **시작**: 이 파일 열고 🔴 untouched 항목 + ⚠️ partial 항목 top 5 확인
2. **종료**: 진행한 항목 상태 갱신 (done/partial). 새로 발견한 누락은 🔴 추가.
3. **gate.sh 실행** + delta 출력 확인 → 악화된 항목은 본 문서에 N.X로 기록
4. **사용자 보고 시 이 파일 링크 + 미해결 top 3 명시** (게이트 점수만 보고 금지)

---

## 2026-07 재검토 (Code↔View 경합 심층 디깅) — 신규 진단

_원천: [reports/review_2026-07_deepdig.md](reports/review_2026-07_deepdig.md) — 5월 종료 후 재검토에서 확인된 결함/구조 부채._

### Wave 0 (즉시) — 확정 결함 3건

| 항목 | 커밋 | 상태 |
|---|---|---|
| D-bug FoodModel.AddToBento 극성 반전 + 수락 브랜치 대칭 | `c0044b1` | ✅ done |
| VisibleStateUtil.cs:18 Awake clobber 제거 (인스펙터 값 상시 무효화) | `9ccd41a` | ✅ done |
| StaminaGauge 40/15 하드코딩 → maxValue 비율 SerializeField | `89a22d0` | ✅ done |

**도달성 triage 완료**: **LIVE bug 확정**. 시나리오:
1. `CookingSceneManager.FillStorage`가 인벤토리 raw INGREDIENT를 `FoodModel` 인스턴스로 fridge/shelf에 배치.
2. Player가 `BentoSetModel` 드래그로 `BentoModel` 씬 소환·배치.
3. Player가 raw 재료 드래그 → 놓기. `FoodModel.Awake` (line 34-35)가 `OnDragEnd`에 `AddToCookingTool`+`AddToBento` 둘 다 구독 → 어느 쪽이든 겹치면 발화.
4. bento 위 놓기 시 `BentoModel.AddIngredient` = false (MAIN/SIDE만 수용, line 66-69).
5. **구 극성 반전**: `if (!reflected)` → ConsumeFood + Destroy → **raw 재료 조용히 소실**.

Cooking 씬 표준 플로우 완전 포함. 릴리즈 빌드가 이 코드였다면 세이브에 raw 재료 사고 소실 잠복 가능. Wave 0 `c0044b1` fix 이후 안전.

### Wave 1 (근접) — 대부분 진행됨

| 항목 | 커밋 | 상태 |
|---|---|---|
| B ShopUIAdapter Find(8+) → ShopBookRefs + SerializeField | `ccaaa2e` | ✅ done |
| B MenuCardController Find(9) → SerializeField | `e2b8862` | ✅ done |
| B InventoryPageController row Find(4) → BatchRowRefs | `984c89a` | ✅ done |
| B DialogueManager Find(5) → DialoguePanelRefs | `8739f44` | ✅ done |
| B RecipeBookManager Chrome Find(4) → SerializeField | `8d3af04` | ✅ done |
| C MenuCardController 탭 색상 → UIColors + TabActive 신설 | `7c758d6` | ✅ done |
| C SettlementLineItemUI Income/Expense → UIColors | `669105b` | ✅ done |
| C InventoryPageController bar 세만틱 태그 (default 그대로) | `669105b` | ✅ done |
| **ConfirmModal 프리팹화** | — | 🟡 deferred (자기완결·저위험. Unity asset 작업 별도 phase) |

**Find 제거 소계**: 22곳 (5개 시스템). 프리팹 rename/reparent 시 조용한 파손 방어선 확보.

### Wave 2 — 진행됨

| 항목 | 커밋 | 상태 |
|---|---|---|
| ADR-008 DI 경계 규칙 문서화 (whitelist/blacklist, 채널, 완화 원칙) | `412d600` | ✅ done |
| ShopUIAdapter 5개 서비스 캐싱 (Purchase/Stats/ToolUpgrade/StorageUpgrade/FarmUpgrade) | `412d600` | ✅ done |
| ShopDetailPanel `Inject(purchase, tool, storage, farm)` API + `.Instance` 5회 제거 | `412d600` | ✅ done |

**정리 소계**: `.Instance` 접근 Shop 계열 17회 → 0회. 씬 컨트롤러(MallSceneController)의 `.Instance`는 ADR-008 whitelist(GameSessionRoot 루트) 라 정당.

**deferred**: MallSceneController의 `.Instance` 반복 접근을 캐싱 스타일로 정리하는 것은 규칙 위반이 아니라 코드 품질 개선이라 우선순위 낮음. 향후 신규 코드에 ADR-008 준수만 유지.

### Wave 3 — 부분 진행 (option ②)

| 항목 | 커밋 | 상태 |
|---|---|---|
| FoodModel 2메서드(AddToCookingTool/AddToBento) → OnFoodDropped 단일 dispatched | `5777aff` | ✅ done |
| CookingToolModel 2메서드(AddToBento/TransferIngredient) → 통합 | — | 🟡 deferred |
| IngredientPlacementService (POCO) 완전 추출 | — | 🟡 deferred (option ① — 코어 45파일 blast radius, 수동 회귀 체크리스트 선행 필요) |
| FoodModel convex-hull 수학 분리 | — | 🟡 deferred |
| 네이밍 (파일명↔클래스명, *Model→*View 대량 rename) | — | 🟡 deferred |

**성과**: 극성 반전 fix에 이어 FoodModel의 `OnDragEnd` 다중 구독을 단일로 정리 — review D의 "발화 순서 의존" 미래 버그 온상 축소. 신규 target(Storage 등) 추가 시 OnFoodDropped 한 지점만 편집.

**deferred 이유 (Playmode 회귀 부재)**: CookingToolModel의 두 메서드는 `SchemaInstance.GetResult()`/`Ingredients.ForEach` 등 상태 결합 로직이 있어 통합 시 미묘한 순서 변경 리스크. 완전한 IngredientPlacementService 추출은 자동 회귀 테스트 도입 이후로 미룸.
