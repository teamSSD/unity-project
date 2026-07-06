# Code↔View Contention 진단 — 2026-07-06

_원천: 4개 병렬 감사 (inspector-contention / dependency-fanout / SRP-smell / consistency). refactor/2026-05 브랜치, Phase 5 종료(2026-06-04) 후 126 .cs 파일 기능 개발이 누적된 시점._
_렌즈: 일관성 / 코드 스멜 / SRP / **코드↔뷰 경합(최우선)**._

> **한 줄 진단**: 2026-05 리팩터는 *상태(state)*를 매니저 밖 POCO Service로 빼는 데 성공했지만, *접근(access)*과 *뷰 구성(view construction)*은 여전히 코드-전역에 남아 있다. 즉 "코드↔뷰 경합"은 게이트가 못 잡은 **미완의 절반**이다.

---

## 근본 원인 5개 (독립 감사 교차검증됨)

### A. 상태는 뺐지만 접근은 전역 static — DI 섬(island) 하나만 존재
- **정량**: 정적 `GameSessionRoot.Instance.<Service>` 호출 **167개소**. DI 컨테이너 없음.
- **정확한 패턴이 이미 코드에 있음**: `BentoSelectionController.Inject(IUnlockedFoodProvider, MenuSelectionService)` (UI/BentoSelectionController.cs:28, MallSceneController.cs:56에서 push) — 유일하게 인터페이스 의존 + push-injection. Cooking 서브시스템(FoodModel/CookingToolModel/CustomerSpawner)도 `Inject()` 채택. 나머지 ~40 뷰는 전부 `.Instance`.
- **같은 파일 내 혼용**: CookingToolModel.cs:29 `Inject(...)` + :182 `SoundManager.Instance`; BentoSelectionController.cs:28 inject + :158 `.Instance`.
- **의미**: "새 의존성을 Inject 파라미터로 받을지 `.Instance`로 잡을지" 규칙이 없어 이미 만들다 만 DI 이음새(seam)가 조용히 우회된다. 뷰가 테스트 불가능해지는 지점이 정확히 `.Instance` 지점.
- **→ 사용자의 "뷰 의존성 과다" 우려의 근본 원인.**

### B. UI를 코드로 통째 생성 — 인스펙터로 될 일을 코드까지 끌어옴 (경합의 핵심)
코드에서 Canvas/CanvasScaler/버튼/레이아웃을 런타임 construction하는 파일들. 전부 프리팹 후보:
- `Shop/ShopUIAdapter.cs:44-68` — Canvas 코드 생성 + `book.Find("Page/ShopPage/Page_L/Scroll/Viewport/Content")` 깊은 계층 문자열 + enum `ToString()`이 GameObject 이름과 매칭되길 기대(`$"...BookMark_{(Tab)i}"`).
- `Mall/DialogueManager.cs:57-86` + `DialogueManager.Choices.cs:25-46` — DialogueCanvas 전체 + 선택지 버튼 행(▶ 글리프/HLG/spacing/padding/font)을 매번 코드로 build.
- `Common/ConfirmModal.cs:78-164` `BuildUI` — 모달 전체 + ~12개 하드코딩 tunable(sortingOrder=300, refRes 1920×1080, dim 0.55, width 600, padding, font 36/26…).
- `UI/InteractPromptUI.cs:27-49`, `UI/MenuCardOverlay.cs:65-96 CreateCloseButton`, `Common/LoadingManager BuildLoadingUI` — 동일 패턴.
- **`transform.Find("A/B/C")` 계층 문자열 배선**: MenuCardController.cs:44-58 (**9개**), ShopUIAdapter, DialogueManager, RecipeBookManager.cs:72-83, MenuSlot.cs:30, InventoryPageController(배치행 자식). 프리팹 rename/reparent 시 컴파일 에러 없이 런타임 null.
- **의미**: 디자이너가 색/폰트/위치/레이아웃을 인스펙터에서 못 만진다. 재컴파일 없이는 어떤 시각 튜닝도 불가. `new Vector2(1920,1080)` refResolution이 코드-생성 Canvas 5곳에 중복.

### C. 하드코딩 튜닝값·팔레트가 팀 자체 규칙(인스펙터 우선) 위반
- **UIColors 우회 팔레트** (직접 `new Color(...)`): SettlementLineItemUI.cs:9-11 (수입 초록/지출 빨강 `static readonly`), DeliveryNpcView.cs:74-84 (상태 tint), MenuCardController.cs:25-28 (탭 4색), ValidationFeedbackUI.cs:154-165 (S~F 등급색), NumericStatsViewer.cs:55 (#FFD700).
- **임계치가 max와 분리**: StaminaGauge(LinearGauge).cs:42/50/58 — 경고 임계 `40`/`15`가 `maxValue=100`과 decouple. max 바꾸면 조용히 깨짐.
- **비직렬화 월드 레이아웃**: CustomerSpawner.cs:32-44 (5개 Vector3 위치가 `private` 비직렬화), OrderTicketController.cs:20-23, Receipt.cs:9-10 — 수작업 `base + offset*index` 루프(VLG로 될 일). `slotOffset 1.75`가 CustomerSpawner↔OrderTicketController 간 중복(티켓이 손님 위에 정렬돼야 해서 cross-class 결합).
- **sibling 가족 내 규칙 split**: MiniGameAbstract 계열 — FireMiniGame/GriddleMinigame는 `[SerializeField] private`, 반면 SliceMiniGame.cs:12-32은 `public` 15개, MixMiniGame 6개, SauceMiniGame 4개. Behavior 계열 `speed` — CookingToolBehavior `[SerializeField]` vs FoodBehavior/OrderTicketBehavior `public`.
- **숨은 밸런스 2차 원천**: Farm.cs:23 `?? 3`, :87 `?? 5`, FarmTile.cs:25 `?? 0f` — FarmUpgrade 데이터와 별개의 shadow 기본값(drift 위험).

### D. 비즈니스 규칙이 View("Model" MonoBehaviour) 안에 산다
- **네이밍이 거짓말**: `FoodModel/BentoModel/CookingToolModel/OrderTicketModel`은 전부 `: MonoBehaviour` 프리젠터. 실제 데이터 POCO는 `*Schema`. → "Model"=MonoBehaviour, "Schema"=data (MVC 관례의 반대).
- **규칙이 뷰에 박힘**:
  - FoodModel.cs — 80줄 convex-hull 수학 라이브러리(L92-171) + 드롭/소비 규칙 + 툴팁 포맷 + 콜라이더 저작 = 4책임.
  - OrderTicketModel.cs:159 `ValidateExactMatch`(주문 정확도 규칙, 이미 존재하는 MenuValidator가 할 일) + :172 배달 분리 로직이 드래그-애니메이션 뷰에 매몰.
  - PhaseActionSelector.cs:50-51 — `Stats.SetStamina(100); Progress.PassPhase();`를 버튼 핸들러에서 직접 실행 (뷰엔 이미 `OnActionExecuted` 이벤트가 있는데 안 씀).
  - Farm.cs — 5개 서비스 만지며 잠금규칙/수확량/save-restore를 뷰에서 결정.
  - ShopDetailPanel.cs:124/148-150 — 표시 데이터는 push받는데(좋음) 버튼 클릭 시 4개 서비스로 되돌아가 도메인 mutate.
- **메타 발견 (가장 큰 체계적 스멜)**: "객체 A를 대상 B에 드롭" 배치 규칙이 draggable View마다 재구현 — FoodModel(×2: AddToCookingTool/AddToBento), CookingToolModel(×2), OrderTicketModel. MEMORY.md의 `BaseStorage` 리팩터가 storage에 대해 잡은 중복 클래스와 **동일**한데, 드래그-드롭 계층엔 확장 안 됨. FoodModel.cs:204 vs :231의 `reflected` 플래그 **극성 반전**은 복붙 중 생긴 잠재 버그 표면.

### E. 네이밍/배치 일관성 붕괴 (내비게이션 함정)
- **파일명 ≠ 클래스명**: MoneyUI.cs → `SmoothMoneyText`, StaminaGauge.cs → `LinearGauge`. IDE "go to file" 실패, MCP 스크립트 lookup mismatch. (133/137은 일치 — 이 2개만 예외.)
- **Manager vs Controller가 역할과 무관**: 싱글톤인데 "Controller"(MenuCardController), 비싱글톤인데 "Manager"(CustomerManager, StatManager=25줄 이벤트 forwarder). "Manager"가 싱글톤을 함의하지만 아님.
- **밝은 점**: `*Adapter`(StatsAudioAdapter/SaveAdapters)는 일관되게 "A→B 브릿지"로 사용됨.

---

## 교차검증된 실제 버그 (감사 부산물)

| 버그 | 위치 | 확인 |
|---|---|---|
| Waiting 풀 크기 5 vs 3 모순 (reset 후 5→3 축소, GetWaitingQueueSize 음수 가능) | CustomerSpawner.cs:32/41/189 | SRP + Inspector 두 에이전트 독립 확인 |
| `Awake()`가 `[Range] minAlpha` 직렬화값을 매 실행 clobber (인스펙터 필드가 거짓말) | VisibleStateUtil.cs:18 | Inspector |
| `reflected` 극성 반전 (복붙 divergence) | FoodModel.cs:204 vs :231 | SRP |
| 돈 지급 split-brain (Lifecycle에서 AddMoney + Manager에서 재기록) | CustomerLifecycle.cs:107 & CustomerManager | SRP |
| 숨은 밸런스 기본값이 FarmUpgrade와 2중 원천 | Farm.cs:23/87 | Inspector |

---

## diagnosis_status.md "9 UI 매니저 wontfix" 재평가

의존성 감사 결론: wontfix는 **leaf 프리젠터엔 유효**(HUDManager/UIManager/LoadingManager/SettingsUIManager/ValidationFeedbackUI + Money/Clock/Stamina 단일값 readout — 0~1 의존, 비즈니스 결정 없음). ValidationFeedbackUI/HUDManager는 나머지가 모방해야 할 **모범 사례**.
하지만 **ShopUIAdapter(8), ShopDetailPanel(5), Farm(7)은 wontfix가 틀림** — 팬아웃이 실질적으로 축소 가능. 이 3개는 canonical 상태에서 partial로 되돌려야 함.

---

## 리팩터링 플랜 (leverage 순 4 wave)

> Unity 규칙: `.unity`/`.prefab` 직접 편집 금지 → 프리팹화 작업은 Unity MCP/에디터 스크립트로. "부분 적용 거부"(consistency_priority) 원칙에 따라 각 카테고리는 **전량 sweep**으로.

### Wave 0 — 버그 + 저비용 chore (반나절, 고신뢰)
1. CustomerSpawner 풀 크기 단일화 → `[SerializeField] int maxWaitingCustomers` 하나로 (5/3 모순 해소).
2. VisibleStateUtil.cs:18 삭제 (필드 이니셜라이저로).
3. FoodModel reflected 극성 검증 후 통일.
4. CustomerLifecycle 돈 지급 1곳으로 단일화.
5. 파일명=클래스명 rename (MoneyUI→SmoothMoneyText 또는 반대; StaminaGauge/LinearGauge).
6. OnValidate 누락 4개 추가 (BentoPositionModel/ShopSceneController/CatalogProvider/GoHomeInteraction).
7. `public` tunable → `[SerializeField] private` 전량 (SliceMiniGame 15, MixMiniGame, SauceMiniGame, FoodBehavior, OrderTicketBehavior).

### Wave 1 — 코드↔뷰 경합: 인스펙터 탈출 (사용자 최우선)
8. **UIColors 우회 팔레트 전량 회수** → `[SerializeField] Color` 또는 UIColors 경유 (5개 파일). UIColors를 ScriptableObject로 승격 검토.
9. **코드-생성 UI 프리팹화** (Unity 작업): ConfirmModal / InteractPromptUI / MenuCardOverlay 닫기버튼 / DialogueManager Canvas+선택지행 / ShopUIAdapter Canvas / LoadingManager. `BuildUI`류 삭제, 프리팹이 config 보유.
10. **`transform.Find` 계층 문자열 → `[SerializeField]` 배선** 전량 (MenuCardController 9개, ShopUIAdapter, DialogueManager, RecipeBookManager, MenuSlot).
11. 임계치/월드좌표/스페이싱 직렬화: StaminaGauge 40/15, CustomerSpawner/OrderTicketController/Receipt 레이아웃 → SerializeField 또는 씬 앵커 Transform/VLG.

### Wave 2 — 코드↔뷰 경합: 의존성 축소
12. **`Inject()` 패턴 전 UI 확대** — 각 씬 컨트롤러(Mall/Shop/Cooking)가 뷰에 서비스 push. `.Instance`는 예외로. (근본원인 A 해소 = 사용자 "의존성 과다" 직접 해결.)
13. **Shop 파사드 도입** `IShopFacade`(GetTabRows/GetDetail/TryBuy/TryUpgrade + ShopChanged 이벤트) → ShopUIAdapter(8)+ShopDetailPanel(5) 팬아웃 각 ~1로. 수동 Notify 콜백 쌍 → 이벤트로.
14. **비즈니스 규칙 뷰 밖으로**: PhaseActionSelector는 이미 있는 `OnActionExecuted` 이벤트만 쏘고 stamina/phase 규칙은 서비스가. ShopDetailPanel은 `OnBuyRequested/OnUpgradeRequested` 이벤트로. Farm → `FarmPlotService`(잠금/수확/영속) POCO.

### Wave 3 — SRP 분해 + 네이밍 정규화 (churn 큼, 신중히)
15. **`IngredientPlacementService` 추출** — 5개 draggable 드롭 규칙 통일 (BaseStorage 정신을 드래그-드롭 계층까지). 최대 SRP 승리이자 reflected 버그 클래스 제거.
16. FoodModel 분해: `PolygonMath`(순수 static, 유닛테스트 가능) + collider expander + placement service.
17. OrderTicketModel `ValidateExactMatch` → MenuValidator로 이관, `*Model`→`*View`/`*Presenter` rename.
18. TimeManager → `GameClock` + `LocalTimerService` 분리. SoundManager → `BgmDirector` + `AudioPlayer`.
19. Manager/Controller 명명 규약 확정(Manager=싱글톤, Controller=씬 스코프)으로 정렬.
20. `Debug.Log` → `GameLog` 헬퍼(`[Conditional]` 컴파일 아웃)로 전량 (65 hits → 게이트 24 달성).

---

## 다음 액션 후보
- Wave 0을 즉시 착수(버그 + chore, 고ROI 저위험)하고 게이트 재스냅샷.
- diagnosis_status.md 갱신: ShopUIAdapter/ShopDetailPanel/Farm wontfix→partial, 신규 항목 A~E 라벨.
- Wave 1(경합-인스펙터)을 이번 phase 주제로 선언.
