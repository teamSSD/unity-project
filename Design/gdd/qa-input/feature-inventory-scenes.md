# QA 인풋 — 씬별 기능 인벤토리

> **문서 목적**: Aftertaste 게임의 9개 씬에 실제로 존재하는 관찰 가능한 요소를 누락 없이 나열.
> 테스트 케이스 설계는 QA 몫이며, 이 문서는 그 재료임.
>
> **출처**: `Assets/Scenes/ForReal/*.unity`, 씬별 컨트롤러 스크립트, 기존 GDD (`Design/gdd/scenes/`, `Design/gdd/systems/`).

---

## Boot 씬

> 파일: [`Assets/Scenes/ForReal/Boot.unity`](../../../Assets/Scenes/ForReal/Boot.unity)
> 컨트롤러: [`BootLoader.cs`](../../../Assets/Scripts/Unity/Common/BootLoader.cs)

### 진입
- 게임 실행 시 최초로 로드되는 씬 (Build Settings 인덱스 0)
- Editor에서 GameStart를 직접 Play해도 `GameStart.cs`가 `ManagerBootstrap.EnsureAll()`로 매니저 보장 (Boot 우회 경로)

### UI 요소
- 없음 (씬은 렌더링 대상 없이 로직 홀더로만 존재; Camera/Light 없음)

### 인터랙티브 오브젝트
- 없음

### 플레이어 액션
- 없음 (씬 표시 시간 자체가 매우 짧음)

### 자동 동작
- Managers 씬 additive 로드 → SetActive
- `ManagerBootstrap.EnsureAll()` — 매니저 6종(`LoadingManager`, `HUDManager`, `UIManager`, `ShopUIAdapter`, `SettingsUIManager`, `RecipeBookManager`) 없으면 생성
- `FontPreWarmer.WarmAll()` — SUIT 폰트 5종에 게임 등장 문자 pre-warm (`Resources/font_prewarm_chars.txt` 소스)
- GameStart 씬 additive 로드 → SetCurrentScene(GameStart) → SetActive
- Boot 씬 자기 자신 UnloadSceneAsync (fire-and-forget)

### 탈출
- 위 자동 동작 완료 후 씬 언로드 → GameStart 씬으로 자동 이관

---

## Managers 씬

> 파일: [`Assets/Scenes/ForReal/Managers.unity`](../../../Assets/Scenes/ForReal/Managers.unity)

### 진입
- Boot 씬이 최초 additive 로드 (게임 세션 내내 상주, 언로드되지 않음)

### UI 요소
- **TutorialOverlayCanvas** — 튜토리얼 말풍선(bubble)/앵커 전용 오버레이 캔버스 (씬 전환에도 살아남음)
- **ConfirmModal** (동적 생성) — Yes/No 모달. 최초 호출 시 Canvas + Panel 코드로 생성 후 재사용. Escape 자동 No.
  [`ConfirmModal.cs`](../../../Assets/Scripts/Unity/Common/ConfirmModal.cs)
- **StatUI 프리팹 인스턴스** (`HUDManager` 자식) — 상단 HUD (money + stamina + clock). 씬 전환에도 유지.
- **ShopBook 인스턴스** (런타임, `ShopUIAdapter` 소유) — 실제 상점 UI. 4탭(Item / Tool / Storage / Farm) 오버레이.
- **레시피북 인스턴스** (런타임, `RecipeBookManager` 소유) — Tab 키로 열림, 메뉴/인벤토리 2 페이지 북마크.
- **Settings 창** (`SettingsUIManager`) — 효과음/음악 슬라이더, 해상도 셀렉터, 전체화면 셀렉터, 닫기/리셋 버튼.
  [`SettingsController.cs`](../../../Assets/Scripts/Unity/UI/SettingsController.cs)

### 인터랙티브 오브젝트
- (씬 자체엔 없음 — 매니저 GO들이 다른 씬의 오브젝트/UI를 소유·관리)

### 플레이어 액션
- 없음 (컨텐츠 씬 아님)

### 자동 동작
- **CatalogProvider** (`ExecutionOrder(-1000)`) — 모든 catalog SO 정적 접근점 초기화.
  대상: Food/Recipe/Ingredient/CookingTool 카탈로그, DeliveryNpc/DialogueConfig/NpcNormalDialogue 카탈로그, PrefabCatalog, foodShopConfig, CsvCatalog(deliveryQuest/cropData/farmUpgrade/storageUpgrade/toolUpgrade), CropSpriteCatalog, BGM 3종(bgmMall/bgmCooking/bgmNight).
  [`CatalogProvider.cs`](../../../Assets/Scripts/Unity/Common/CatalogProvider.cs)
- **GameSessionRoot** (`ExecutionOrder(-999)`) — `GameState` POCO 생성 + 17개 Service wiring (Stats/Progress/CropCatalog/FarmUpgrade/StorageUpgrade/ToolUpgrade/Inventory/Purchase/DeliveryQuest/NpcNormalDialogue/Order/QuestMenuCatalog/MenuSelection/UnlockedFood/RecipeLookup/Weather/Settlement/Tutorial).
  [`GameSessionRoot.cs`](../../../Assets/Scripts/Unity/Common/GameSessionRoot.cs)
- **SoundManager** — bgmSource / sfxSource / loopSfxSource 3종 AudioSource. 페이즈 변화 → BGM 자동 전환 (`bgmMall`/`bgmCooking`/`bgmNight`). 씬 전환 시 AudioListener 감지해 SFX 활성/비활성. 씬 로드 후 모든 Button에 클릭 SFX 자동 부착. UI SFX: `uiBookSfx`, `buttonClickSfx`.
  [`SoundManager.cs`](../../../Assets/Scripts/Unity/Common/SoundManager.cs)
- **TutorialController** — 스텝 정의(`TutorialStepCatalog`) 기반 파트 순차 표시. `TutorialTarget` 앵커에 bubble world position 추적.
  [`TutorialController.cs`](../../../Assets/Scripts/Unity/Tutorial/TutorialController.cs)
- **TextureDiagnostics** — `scanIntervalSec=3f` 주기 SpriteRenderer 스캔. null sprite/null material 로그, 씬 진입 시 개수 요약.
  [`TextureDiagnostics.cs`](../../../Assets/Scripts/Unity/Common/TextureDiagnostics.cs)

### 탈출
- 없음 (게임 종료까지 상주)

---

## GameStart 씬

> 파일: [`Assets/Scenes/ForReal/GameStart.unity`](../../../Assets/Scenes/ForReal/GameStart.unity)
> 컨트롤러: [`GameStart.cs`](../../../Assets/Scripts/Unity/Common/GameStart.cs)

### 진입
- Boot 씬이 Managers 로드 후 두 번째로 additive 로드
- Editor에서 직접 Play해도 진입 가능 (매니저 미존재 시 `ManagerBootstrap.EnsureAll()` 자동 호출)

### UI 요소
- **newGame** 버튼 (Image + Text (TMP))
- **Continue** 버튼 (Image + Text (TMP)) — 저장 데이터 유무에 따라 `interactable` 결정, 비활성 시 컬러 `(0.5, 0.5, 0.5, 0.5)`
- **Settings** 버튼 (Image + Text (TMP))
- 타이틀 등 정적 TextMeshProUGUI
- Canvas (ScreenSpaceOverlay)
- EventSystem + StandaloneInputModule

### 인터랙티브 오브젝트
- (레거시 placeholder GO 4종 — `ActionSelectionManager`, `ProgressSystem`, `RecipeDataManager`, `StatsSystem` — 실제 매니저는 Managers 씬 소속)

### 플레이어 액션
- `NewGame` 버튼 클릭 → 신규 게임 흐름
- `Continue` 버튼 클릭 → 저장 데이터 로드 흐름 (`SaveManager.HasSaveData()` false 시 비활성)
- `Settings` 버튼 클릭 → `SettingsUIManager.Open()` 오버레이

### 자동 동작
- Start 시 `UILockManager.Lock(Owner.GameStart)` — 유저 입력 잠금
- Continue 버튼 상태 자동 결정 (저장 데이터 유무)
- **NewGame 흐름**: `SceneLoader.LoadSceneWithInit(Mall, ...)` → `InitializeManagers()` + `ApplyNewGameDefaults()` (시각=05:00, 소지금=12000G, 스태미나=100, Day=0, 인벤토리 리셋, 언락 레시피 기본값, NPC 대화 상태 리셋, 밭 타일 배열 clear) → `GameRandom.InitSession/InitDay` → `Weather.UpdateWeather(0)` → `SaveManager.SaveAll()` → `HUDManager.Initialize()`
- **Continue 흐름**: `SceneLoader.LoadSceneWithInit(Mall, ...)` → `InitializeManagers()` + `Order.Clear()` → `SaveManager.LoadAll()` → 저장된 `immutableSeed` + 세션 신규 시드로 `GameRandom` 재초기화 → `GameRandom.InitDay(day)` → `Weather.UpdateWeather(day)` → `HUDManager.Initialize()`

### 탈출
- NewGame / Continue 클릭 → Mall 씬으로 자동 이관 (`UILockManager.Unlock` 후)
- Settings 클릭 → 씬 유지, 오버레이만 표시

---

## Mall 씬

> 파일: [`Assets/Scenes/ForReal/Mall.unity`](../../../Assets/Scenes/ForReal/Mall.unity)
> 컨트롤러: [`MallSceneController.cs`](../../../Assets/Scripts/Unity/Common/MallSceneController.cs)

### 진입
- GameStart의 NewGame/Continue 흐름 종료 시
- Cooking/CookingTutorial 씬 페이즈 종료 시 (Night → Settlement 예외)
- Shop 씬의 ExitToMall 트리거 사용 시
- Garden 씬의 ExitToMall 트리거 사용 시
- Settlement 씬에서 아무 키 입력 후

### UI 요소
- **상단 HUD** (`HUDManager`가 관리하는 StatUI 인스턴스) — Money, Stamina, Clock 표시
- **PhaseActionSelector** (런타임 인스턴스, prefab) — 화면 중앙 카드 3장(영업/휴식/상가 이동)
  [`PhaseActionSelector.cs`](../../../Assets/Scripts/Unity/UI/PhaseActionSelector.cs)
  - `phaseTypeText`: "{준비|아침|점심|저녁|밤} 페이즈 선택"
  - `workButton` ("영업" / "가게를 엽니다.")
  - `restButton` ("휴식" / "스태미너를 충전합니다.")
  - `shoppingButton` ("상가 이동" / "상가로 이동합니다.")
- **BentoSelection modal** (런타임 인스턴스, prefab) — 도시락 3슬롯 UI (Preparation 페이즈 전용)
  [`BentoSelectionController.cs`](../../../Assets/Scripts/Unity/UI/BentoSelectionController.cs)
  - `bentoSlots[3]` (도시락 1/2/3), 각 슬롯: 이름 변경, mainCategory + sideCategory 아이템 목록
  - `backButton`, `confirmButton`
- **goHomeButton** (레거시 상단 UI, `MallSceneController.goHomeButton`)
- **DialoguePanel** (`DialogueManager` 관리 prefab) — NPC 대화창
  [`DialogueManager.cs`](../../../Assets/Scripts/Unity/Mall/DialogueManager.cs)
- **InteractPromptUI** — 근접 상호작용 안내 (계단/상점/텃밭/집/NPC)
- **ConfirmModal** — GoHome, 페이즈 종료 확인 등
- **TutorialBubble** — 튜토리얼 활성 시 (WelcomeAtSpawn / PhaseSelectAfternoon / MallCorridor / MallStairs / MallStore / MallNPC / MallReturnToStore / Closing)

### 인터랙티브 오브젝트
- **NPCs 컨테이너** — 일반 NPC (Casual), 배달 NPC (`DeliveryNpcView`) 다수 배치
  - [`CasualNpcInteraction.cs`](../../../Assets/Scripts/Unity/Mall/CasualNpcInteraction.cs)
  - [`DeliveryNpcView.cs`](../../../Assets/Scripts/Unity/Mall/DeliveryNpcView.cs) → state에 따라 `DeliveryNpcDialogueInteraction` / `CasualNpcInteraction` 컴포넌트 동적 추가
- **Stores (Floor1~Floor5)** — 각 층 store1~store14, playerStore (Floor3), meat_store (Floor3, 정육점)
- **playerStore trigger** ([`GoHomeInteraction.cs`](../../../Assets/Scripts/Unity/Mall/GoHomeInteraction.cs)) — 플레이어 진입 후 Space
  - 자식: `TutorialAnchor_Door` (튜토리얼 앵커)
- **Stairs (upper/lower/visual 짝)** — 층간 이동
  [`StairsInteraction.cs`](../../../Assets/Scripts/Unity/Mall/StairsInteraction.cs)
  - 자식 Triangle × 4~5 — 스텝 콜라이더
  - `targetStair` 짝 계단 참조, `promptText = "press spacebar"`
- **FarmPath** ([`FarmPathInteraction.cs`](../../../Assets/Scripts/Unity/Mall/FarmPathInteraction.cs)) — Garden 씬 진입 지점
- **Shop 진입 트리거** (`SceneTransitionInteraction` — targetScene, spawnX 설정 가능)
- **Background (parallax layer 6종)** — ground 아래 layer 1~6
- **TutorialAnchor_Head** — 튜토리얼용 카메라 앵커 (별도 root)

### 플레이어 액션
- 좌우 이동 (A/D 또는 좌우 방향키, `PlayerMove`)
- **Space** — 근접 상호작용 (계단/텃밭길/상점/NPC/집)
- **Tab** — 레시피북 열기 (`RecipeBookManager.Update`가 전역 감지)
- **Escape** — 열린 UI(PhaseActionSelector/BentoSelection/RecipeBook/Settings) 닫기
- 마우스 클릭 — 상단 goHomeButton, PhaseActionSelector 버튼, BentoSelection 버튼
- PhaseActionSelector에서 액션 선택:
  - **영업(Work)** → Cooking 씬으로 이동
  - **휴식(Rest)** → 스태미나 100 회복 + `PassPhase` (Night 페이즈면 Settlement 씬으로)
  - **상가 이동(Shopping)** → UI만 닫음 (이미 Mall이므로)

### 자동 동작
- Start 시 `EventSystem.sendNavigationEvents = false` (Space/Enter로 버튼 실수 트리거 방지)
- `SceneLoader.MallReturnPosition` 있으면 플레이어 위치 override, `CameraFollow.SnapToPlayer()` 즉시 스냅
- Preparation 페이즈 진입 시 `MenuSelection.ClearAllMenus()`
- BentoSelection prefab Instantiate → `Inject(unlockedFood, menuSelection)` → `Close()`
- PhaseSelection prefab Instantiate → `Hide()` 후 `OnActionExecuted` 구독
- Mall 진입 시 페이즈 + 이전 씬 조합으로 UI 자동 결정:
  - Preparation/Morning: 자동 UI 없음
  - Shop/Garden 복귀: 유지 (다시 강요 X)
  - 그 외 (Cooking 복귀 등): `OpenActionSelection()` 자동 표시
- Preparation 첫 진입 시 `WelcomeAtSpawn` 튜토리얼 시도
- Afternoon 진입 시 `PhaseSelectAfternoon` 튜토리얼 시도 → Part 0 동안 Work/Rest/Shopping 전부 비활성, Part 1 진입("이번 점심에는" prefix) 시 Shopping만 활성 → Shopping 클릭 후 `MallCorridor` 자동 시작
- `DynamicBoundaryWalls` 카메라 뷰포트 좌우 경계 추적하며 벽 콜라이더 동적 배치
- Mall 체류 중 페이즈 전환(Rest 등) 시 `OpenActionSelection()` 자동 재표시 (Preparation/Morning 제외)
- BGM: `bgmMall` (SoundManager가 페이즈에 따라 자동 전환; 밤엔 `bgmNight`)
- 계단 상호작용 중 `UILockManager.Lock(Owner.Loading)` — `PlayerMove.enabled = false`, `Rigidbody2D.simulated = false`, 2단계 이동(현재 → 짝 계단)
- 배달 NPC state에 따라 `DeliveryNpcView.Init`이 sprite/state 적용 + 적합한 interaction 컴포넌트 부착

### 탈출
- **playerStore 접근 후 Space** (`GoHomeInteraction`) → `MallSceneController.GoHome()`
  - Preparation 페이즈: `OpenMenuSelection()` → 확인 시 `PassPhase()` + Cooking(또는 CookingTutorial) 자동 로드
  - Afternoon + Closing 튜토리얼 활성: Closing 표시 → 완료 시 `Complete()` + Settlement 씬 로드
  - 그 외: `ConfirmModal` 확인 → `PassPhase()` (Night이면 문구 "하루를 마치시겠습니까?")
- **FarmPath Space** → `SceneLoader.LoadScene(Garden)`, MallReturnPosition 저장
- **Shop trigger Space** (`SceneTransitionInteraction`) → `SceneLoader.LoadScene(Shop)`, MallReturnPosition 저장
- Escape는 열린 UI만 닫힘 (씬 유지)

---

## Shop 씬

> 파일: [`Assets/Scenes/ForReal/Shop.unity`](../../../Assets/Scenes/ForReal/Shop.unity)
> 컨트롤러: [`ShopSceneController.cs`](../../../Assets/Scripts/Unity/Shop/ShopSceneController.cs)

### 진입
- Mall의 `SceneTransitionInteraction` 트리거 Space
- Mall의 `UnifiedShopInteraction`으로 진입 시엔 씬 전환 없이 오버레이만 뜸 (참고: 현재 프로덕션 flow는 씬 전환 방식)

### UI 요소
- **ShopBook 인스턴스** (`ShopUIAdapter`가 오버레이로 표시, Managers 씬 상주 싱글턴)
  [`ShopUIAdapter.cs`](../../../Assets/Scripts/Unity/Shop/ShopUIAdapter.cs)
  - 헤더 라벨 (탭 이름)
  - 4 Bookmarks: Item / Tool / Storage / Farm (비선택 70px / 선택 105px × 45px 높이)
  - 리스트 컨테이너 + `ShopListRow` prefab 다수
  - `ShopDetailPanel` (선택 아이템 상세)
  - closeButton
- **InteractPromptUI** — "(press spacebar to open shop)" (`UnifiedShopInteraction` 근접 시)
- **Canvas** (씬 자체; ScreenSpaceOverlay)
- **HUD** (Managers에서 상주)

### 인터랙티브 오브젝트
- **ShopTrigger_Item** — `UnifiedShopInteraction` (targetTab = `ShopUIAdapter.Tab.Item` 기본값)
  [`UnifiedShopInteraction.cs`](../../../Assets/Scripts/Unity/Mall/UnifiedShopInteraction.cs)
- **ExitToMall** — `SceneTransitionInteraction` (targetScene = Mall)
- **PlayerSpawnPoint** — 진입 지점 Transform
- **Wall_ShopThreshold** — 진입 경계 벽 (뒤로 걸어나가기 방지)
- **Floor** — BoxCollider2D (플레이어 착지 판정)
- **Square, background** — 지오메트리/배경

### 플레이어 액션
- 좌우 이동
- **Space** at ShopTrigger_Item → `ShopUIAdapter.OpenShop(Item)`
- **Space** at ExitToMall → Mall 씬 복귀
- ShopBook 오버레이 내:
  - 4 Bookmark 버튼 클릭 → 탭 전환 (Item/Tool/Storage/Farm)
  - 각 탭 리스트에서 항목 클릭 → `ShopDetailPanel` 표시
  - 구매/업그레이드 확정 액션 (탭별 `PurchaseService` / `ToolUpgradeService` / `StorageUpgradeService` / `FarmUpgradeService`)
  - closeButton 클릭 또는 Escape → 오버레이 닫힘
- **Tab** — 레시피북 (Managers 씬 전역 핫키)

### 자동 동작
- Start 시 `entryPoint` (없으면 `ExitToMall`) X + Floor 상단 Y로 플레이어 위치 정렬 (플레이어 collider offset/size 반영)
- `CameraFollow.SnapToPlayer()` 즉시 카메라 정렬
- `DynamicBoundaryWalls` 벽 동적 배치
- ShopBook 오픈 시 `UILockManager.Lock(Owner.Shop)`, 닫을 때 Unlock

### 탈출
- **ExitToMall Space** → Mall 씬 (`SetMallReturnPosition`으로 X 저장)
- ShopBook 오버레이 닫기(Escape 또는 close 버튼)는 씬 유지, UI만 해제

---

## Cooking 씬

> 파일: [`Assets/Scenes/ForReal/Cooking.unity`](../../../Assets/Scenes/ForReal/Cooking.unity)
> 컨트롤러: [`CookingSceneManager.cs`](../../../Assets/Scripts/Unity/Cooking/CookingSceneManager.cs)

### 진입
- Mall Preparation → BentoSelection 확정 → `PassPhase()` → `SceneLoader.LoadScene(Cooking)` (튜토리얼 미활성 시)
- Mall Afternoon/Evening/Night PhaseActionSelector → **영업(Work)** 클릭 시

### UI 요소
- **Canvas** (Screen space)
  - `EarlyEndButton` (우상단, "영업 조기 종료")
    [`EarlyEndButton.cs`](../../../Assets/Scripts/Unity/Cooking/EarlyEndButton.cs)
  - Label
- **TopUICanvas** — 상단 UI (시계 등)
- **WorldCanvas** — World-space UI (손님 위 타이머, 영수증 스폰 위치)
- **HUD** (Managers 상주 StatUI) — Day/시각/Money/Stamina 표시
- **미니게임 UI** (6종 prefab 인스턴스, `MiniGameManager`가 관리)
  [`MiniGameManager.cs`](../../../Assets/Scripts/Unity/Cooking/MiniGameManager.cs)
  - BakeMinigamePrefab, BoilMinigamePrefab, MixMinigamePrefab, SauseMinigamePrefab, CutMinigamePrefab, GrillMinigamePrefab
  - MinigameResultPrefab
- **손님 머리 위 타이머 캔버스** (Waiting 손님, `GaugeUI`)
- **OrderTicket** (world canvas 상 스폰) — 일반 6슬롯 + 배달 6번째 슬롯 (`QuestSlotIndex=5`)
- **DeliveryTicket** (배달 퀘스트, `DeliveryTicketCoordinator`)
- **SpeechBubble** (손님 머리 위 등)

### 인터랙티브 오브젝트
- **요리 도구 5종 그룹** (인덕/팬/냄비/볼/도마/철판) — `CookingToolModel` + 관련 컴포넌트, 각 `MinigameId` 매핑 (T001~T005 → M001~M005)
- **테이블** (도구 그룹 배경)
- **냉장고** (`Refrigerator` : `BaseStorage`)
  - 냉장고 내부 자식
- **캐비넷(상)** (`UpperShelf` : `BaseStorage`)
- **캐비넷(하)** (`LowerShelf` : `BaseStorage`, capacity=99, xOffset=-2.23, yPosition=0.35, xInterval=0.755)
- **도시락 위치 1/2/3** (각 `BentoPositionModel`, blinkSpeed=2.5f)
- **도시락묶음** (`BentoSet`)
- **쓰레기통** — 버리기 트리거
- **배경 3종** — `bg_cuisine_morning` / `bg_cuisine_evening` / `bg_cuisine_night` (`CookingBackgroundController` 관리)
- **Customer**
  - Ordering (카운터, world position `(3.02, -0.26, 0)`)
    [`OrderingCustomer.cs`](../../../Assets/Scripts/Unity/Cooking/OrderingCustomer.cs)
  - Waiting (5명 슬롯, base `(-8, 0.78, 0)` + offset `(1.75, 0, 0)` × index)
    [`WaitingCustomer.cs`](../../../Assets/Scripts/Unity/Cooking/WaitingCustomer.cs)
  - Taking (퇴장, exit `(-9.89, -0.85, 0)`)
    [`TakingCustomer.cs`](../../../Assets/Scripts/Unity/Cooking/TakingCustomer.cs)
  - Scale: orderingTaking=0.45, waiting=0.297 (× CustomerData.displayScale)
- **Receipt / ReceiptLine** — 배달 영수증
  [`Receipt.cs`](../../../Assets/Scripts/Unity/Cooking/Receipt.cs)

### 플레이어 액션
- 재료 드래그 & 드롭 → 도구
- 도구 클릭 → 미니게임 시작 (도구별 매핑된 미니게임 prefab 실행)
  - Fire (Space 유지)
  - Sauce (Up/Down 방향키 턴제)
  - Mix (Space 연타)
  - Griddle (방향키 4방향)
  - Click (Space)
  - Slice (드래그)
- 완성 요리 드래그 → 도시락 슬롯
- 도시락 완성 → 영수증 부착
- 손님 클릭 → 주문 접수
- 손님에게 완성 도시락 서빙
- 영수증 호버 시 내려오는 인터랙션
- **EarlyEndButton** 클릭 → `ConfirmModal` ("영업 조기 종료" / "다음 페이즈로 넘어갑니다.") → 확인 시 `CustomerManager.EndEarly()`
- **Tab** — 레시피북 (Managers 씬 전역 핫키)
- **Escape** — 열린 UI 닫기 (미니게임 UI, ConfirmModal 등)

### 자동 동작
- Start 시 `MiniGameManager`, `RecipeLookup`, `Inventory` 참조 획득
- 각 요리도구에 `CookingToolModel.Inject(playMinigameUsecase, searchRecipeUsecase)`
- Storage 3종에 `InjectStorageCapacity(storage, DefaultCapacity)`
- FillStorage — 인벤토리 재료를 저장소별 카테고리로 배치
- `TryStartCookingTutorial()` — CookingIntro 스텝 활성 시 표시 (실 씬에서도 발생 가능)
- `CustomerSpawner` 자동 손님 스폰 (`AutoSpawnEnabled` 상태에 따름)
- 손님 인내심 게이지 자동 감소 (`defaultCustomerWaitTime = 90f`, 영업 종료 후 `closedLocalTimerScale = 2f`)
- 시간 자동 진행 (`TimeManager`, `gameTimeScale = 120f`, startHour=11 / endHour=15)
- BGM: `bgmCooking` (SoundManager)
- `CookingBackgroundController`가 `Progress.OnPhaseChanged` 구독 → 세 배경 GO 토글
- OrderTicket 스폰 애니메이션: `-2y`에서 위로 올라옴
- `TimeManager.OnTimeEnd` → 페이즈 종료
- `CustomerManager.OnGameEnd` → `SubSceneController.ReturnToIdle()` → `Progress.PassPhase()`
- 도어 벨 SFX (`CustomerManager.doorSfx`) — 손님 등장 시

### 탈출
- `TimeManager.OnTimeEnd` 시각 도달 (Morning 12:00, Afternoon/Evening/Night 각 종료 시각)
- EarlyEndButton 확인 → 즉시 페이즈 종료
- Night 페이즈 종료 시 → Settlement 씬 자동 로드 (return-scene skip)
- 그 외 페이즈 종료 → Mall 씬 (`SubSceneController.returnScene = Mall`)
- Escape는 열린 UI만 닫힘

---

## CookingTutorial 씬

> 파일: [`Assets/Scenes/ForReal/CookingTutorial.unity`](../../../Assets/Scenes/ForReal/CookingTutorial.unity)
> 추가 컨트롤러: [`TutorialCookingController.cs`](../../../Assets/Scripts/Unity/Tutorial/TutorialCookingController.cs)

### 진입
- Mall Preparation → BentoSelection 확정 시 `Tutorial.IsActive`이면 `SceneLoader.LoadScene(CookingTutorial)`

### UI 요소
- Cooking 씬과 동일한 UI 구성 (Canvas, TopUICanvas, WorldCanvas, EarlyEndButton, 미니게임 UI 등)
- **TutorialBubble** — 파트별 안내 말풍선
- **EarlyEndButton** — "오늘은 손님이…" 파트 도달 전까지 클릭 불가

### 인터랙티브 오브젝트
- Cooking 씬과 동일 (요리도구 5종, Refrigerator/UpperShelf/LowerShelf, 도시락 위치 3, 도시락묶음, 쓰레기통, 배경 3종)
- **TutorialTarget** 컴포넌트 각 앵커에 부착 (조리도구/도시락 슬롯/customer/placed-bento/receipt 등)
- Ordering 손님 1명만 mock 스폰 (`TutorialSpawnOne()`)

### 플레이어 액션
- Cooking 씬과 동일한 재료/도구/도시락 조작 (mock 흐름 내에서)
- **Space** — TutorialBubble dismiss (파트 진행)
  - MenuCard 활성 중엔 Space dismiss 무시
- 마우스 클릭도 dismiss로 동작 (Space와 등가)
- 미니게임 내부 입력은 그대로 (Space/방향키)

### 자동 동작
- Start 시 mock 조건 걸기:
  - 재료 무한 refill (각 `BaseStorage.OnFoodDestroyedForRefill += HandleFoodDestroyed`, 다음 프레임 `CookingSceneManager.TryTutorialRefill`)
  - **시간 정지**: `TimeManager.PauseTime()` (아침 자동 종료 방지)
  - **손님 자동 스폰 억제**: `customerManager.enabled = false`
  - **상호작용 차단**: `UILockManager.Lock(Owner.CookingTutorial)`
  - **EarlyEndButton 락**: 스킵 파트 전까지 클릭 불가
- 카메라 제어 (`LateUpdate` SmoothDamp):
  - 파트별 `TutorialTarget` 위치로 이동
  - `worldCollider.bounds`로 클램프
  - `part.freeCamera=true` 파트: 튜토리얼 카메라 off, `HorizontalCameraMove` 활성
  - Force camera 옵션: `forceCameraPosition = (0, 0, -10)`, `forceCameraOrthoSize`
- HandlePartShown 파트별 mock 흐름 라우팅 (메시지 prefix 기반):
  - "손님이 오면" → `TutorialSpawnOne()`, target "customer" 등록
  - "도시락을 원하는" → `BentoModel.OnBentoPlacedForTutorial` 구독
  - "완성한 요리" → 방금 놓은 도시락에 target "placed-bento", `BentoModel.OnFoodAddedForTutorial` 구독
  - "영수증" → ticket에 target "receipt", Taking 손님 대기
  - "시간 안에" → Taking 손님 target 등록 (dynamic follow)
  - "오늘은 손님이" → EarlyEndButton 활성화
- `_activeLifecycle.OnTutorialOrderPlaced` → Waiting timer 정지 (`StopTimer()`)
- 튜토리얼 완료 시 `ReleaseTutorialLocks()`:
  - `UILockManager.Unlock(CookingTutorial)`
  - `HorizontalCameraMove.enabled = true`
  - `TimeManager.ResumeTime()`
  - `CustomerManager.enabled = true` + `EndEarly()` → 즉시 다음 페이즈

### 탈출
- 튜토리얼 완료 → `EndEarly()` → 다음 페이즈로 → Mall 복귀
- Skip(EarlyEndButton)로 조기 종료 시:
  - `Tutorial.MarkShown(CookingIntro)`
  - `Inventory.ResetToDefault()` (refill 흔적 제거)
  - `SaveManager.SaveAll()`
  - 다음 페이즈 → Mall
- `SubSceneController.returnScene = Mall`

---

## Garden 씬

> 파일: [`Assets/Scenes/ForReal/Garden.unity`](../../../Assets/Scenes/ForReal/Garden.unity)
> 컨트롤러: [`SubSceneController.cs`](../../../Assets/Scripts/Unity/Common/SubSceneController.cs) (returnScene=Mall)

### 진입
- Mall의 `FarmPathInteraction` Space (MallReturnPosition 저장 후 로드)

### UI 요소
- **Canvas** (Screen space)
- **SignPanel** — 간판 UI (텃밭 상태 표시 등)
- **각 FarmTile UI** (per-tile world canvas):
  [`Farm.cs`](../../../Assets/Scripts/Unity/Garden/Farm.cs)
  - `actionPrompt` (TextMeshProUGUI) — 근접 시 프롬프트
    - 잠김: "잠겨 있음"
    - 수확 가능: "(스페이스바로 수확)"
    - 빈 타일: "(스페이스바로 심기)"
    - 성장 중: "성장 중..."
  - `growthGauge` (`GaugeUI`) — 성장 게이지, `cropSpriteRenderer.bounds.max.y + uiOffsetY`
  - `cropNameLabel` (TextMeshProUGUI) — 작물 이름, `cropSpriteRenderer.bounds.min.y - nameOffsetY`
- **InteractPromptUI** — ExitToMall 근접 시 안내
- **HUD** (Managers 상주 StatUI)

### 인터랙티브 오브젝트
- **FarmTile 인스턴스 다수** (약 40칸, Prefab)
  - `farmIndex`가 `FarmUpgrade.tile.value` 이상이면 `IsLocked=true`
  - 각 타일: `cropSpriteRenderer`, `growthGauge`, `cropNameLabel`
- **ExitToMall** — `SceneTransitionInteraction`
- **background**, **ground**, **Player** (SpawnPoint)

### 플레이어 액션
- 좌우 이동
- **Space** at 각 FarmTile 트리거 내:
  - 잠김: 무동작
  - 수확 가능: `FarmTile.Harvest(out id, out crops, harvestCount)` → 인벤토리에 적재, 다음 crop 자동 심기 (`harvestCount` = `FarmUpgrade.harvestCount`, 기본 5)
  - 성장 중: 프롬프트만 표시
  - 빈 타일: (원래 심기 가능하지만 Start에서 자동 심기 처리)
- **Space** at ExitToMall → Mall 복귀
- **Tab** — 레시피북

### 자동 동작
- Start 시 각 Farm 타일:
  - `GardenPersistent.tiles[farmIndex]`에 저장 데이터 있으면 `FarmTile.ApplySaveData(...)` 복원
  - 잠기지 않고 비어있으면 `CropCatalog.GetRandomCropByWeight()`로 자동 심기 (가중치 랜덤)
- `Progress.OnPhaseChanged += OnTimePassed` — 페이즈 전환마다 UI 갱신, 성장 단계 진행
- `AdjustUIPosition` — 매 프레임 UI 위치 재조정
- OnDestroy 시 각 타일 상태 → `GardenPersistent.tiles[farmIndex] = tile.GetSaveData()` (씬 나가기 전 저장)
- `CameraFollow` + `DynamicBoundaryWalls` 카메라 경계

### 탈출
- **ExitToMall Space** → Mall 씬 (`SetMallReturnPosition`으로 X 복귀)
- Garden에서는 페이즈 자동 종료 없음 (`SubSceneController.ReturnToIdle()` 미사용)

---

## Settlement 씬

> 파일: [`Assets/Scenes/ForReal/Settlement.unity`](../../../Assets/Scenes/ForReal/Settlement.unity)
> 컨트롤러: [`SettlementController.cs`](../../../Assets/Scripts/Unity/Mall/SettlementController.cs)

### 진입
- Cooking(또는 CookingTutorial) 씬 Night 페이즈 종료 → `Progress.PassPhase()` 결과로 자동 로드
- Mall Afternoon 튜토리얼 Closing 스텝 완료 시 → `SceneLoader.LoadScene(Settlement)`

### UI 요소
- **Canvas** (ScreenSpaceOverlay 예상)
  - **배경** — Settlement 배경
  - **테이블** (정산 테이블 UI 컨테이너)
    - `dayText` — "{Day}일차 정산"
    - `incomeContainer` — 수입 라인 부모
      - `SettlementLineItemUI` 인스턴스 다수 (`Set(label, amount, isExpense=false)`)
      - `incomeTotalText` — "+{totalIncome:N0}G"
    - `expenseContainer` — 지출 라인 부모
      - `SettlementLineItemUI` 인스턴스 다수 (`Set(label, amount, isExpense=true)`)
      - "관리비" 라인 (`SettlementService.ManagementFee`) 추가됨
      - `expenseTotalText` — "-{totalExpense:N0}G"
    - `balanceText` — "{finalMoney:N0}G  ({+/-}{netChange:N0})"
  - **saveStatusText** — 처음엔 빈 문자열 → "저장 중..." → "아무 키나 눌러서 계속"

### 인터랙티브 오브젝트
- 없음 (읽기 전용 요약 화면)

### 플레이어 액션
- **아무 키 입력** (`Input.anyKeyDown`) — `waitingForInput=true` 상태에서만 반응 → Mall 씬 로드

### 자동 동작
- Start 시 `BuildUI()`:
  1. Day 헤더 세팅
  2. `Settlement.GetIncomeEntries()` 순회 → 수입 라인 스폰
  3. `Settlement.GetExpenseEntries()` 순회 → 지출 라인 스폰 + 관리비 라인 추가
  4. `finalMoney = Stats.GetMoney() - ManagementFee` → balance 표시
- `SaveRoutineAsync()`:
  1. `saveStatusText = "저장 중..."`
  2. `await UniTask.Yield()` (1프레임 양보)
  3. `GameSessionRoot.Instance.Progress.PassDay()` — Day 증가 + Stats 리셋 + ManagementFee 차감 + `SaveManager.SaveAll()` + Weather/시드 재초기화
  4. `saveStatusText = "아무 키나 눌러서 계속"`
  5. `waitingForInput = true`
- BGM은 `SoundManager`가 페이즈에 맞춰 처리 (진입 시점 밤 페이즈 기준)

### 탈출
- `waitingForInput` 상태에서 아무 키 → `SceneLoader.LoadScene(Mall)`
  - Mall 진입 시 페이즈는 Preparation (새 하루 시작)

---

## 이 문서 원칙

- **QA 인풋 문서**: "이런 요소가 존재한다"만 기록. 기능/UI/오브젝트/자동 동작의 관찰 가능한 카탈로그.
- **테스트 케이스는 QA 영역**: 절차, 기대 결과, 우선순위, 통과 기준은 이 문서에 없음. QA가 위 재료로 직접 설계.
- **값·수치 상세는 GDD 참조**:
  - 시스템 상세: `Design/gdd/systems/*.md`
  - 씬 아키텍처: `Design/gdd/scenes/scene-*.md`
- **버전 관리**: 씬/컨트롤러 코드가 변경되면 이 문서도 갱신 필요. 원천은 `Assets/Scenes/ForReal/*.unity` 실 파일과 씬 컨트롤러 스크립트.
