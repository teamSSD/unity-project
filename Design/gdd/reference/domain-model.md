# Aftertaste — Domain Model (POCO Service 계층)

ADR-001 Option B에 따라 순수 POCO 서비스 + GameSessionRoot(Composition Root) 구조. `GameSessionRoot`는 `GameSessionStore`를 만들고 18개 서비스를 wiring하며, Store가 `GameState`의 소유권을 가진다.

## 1. Composition Root

`Assets/Scripts/Unity/Common/GameSessionRoot.cs`

| 항목 | 값 |
|---|---|
| 배치 | Managers 씬에 단 1개 (SingletonMonoBehaviour) |
| `[DefaultExecutionOrder]` | **-999** (CatalogProvider -1000 다음) |
| 노출 | `Store`, 호환용 `State`, `Stats`, `Progress`, `CropCatalog`, `FarmUpgrade`, `StorageUpgrade`, `ToolUpgrade`, `Purchase`, `DeliveryQuest`, `NpcNormalDialogue`, `Order`, `QuestMenus`, `Inventory`, `MenuSelection`, `UnlockedFood`, `RecipeLookup`, `Weather`, `Settlement`, `Tutorial` |
| Wiring 분리 | `WireCatalogAndUpgrades` / `WireInventoryAndPurchase` / `WireMallDomain` / `WireCookingDomain` |

## 2. GameState 트리

`Assets/Scripts/Schema/State/`

| Class | 파일 | 필드 |
|---|---|---|
| **GameState** | `GameState.cs` | `GardenState garden`, `ShopState shop`, `MallState mall`, `InventoryState inventory`, `MenuSelectionState menuSelection`, `UnlockedFoodState unlockedFood`, `BasicStats stats`, `PhaseData phase`, `TutorialState tutorial` |
| **InventoryState** | `Common/InventoryState.cs` | `FoodData → List<InventoryBatch>` 런타임 재고. `GameSessionStore` 소유 |
| **MenuSelectionState** | `Cooking/CookingSessionState.cs` | 도시락 3슬롯의 `MenuSelection[]`. `GameSessionStore` 소유 |
| **UnlockedFoodState** | `Cooking/CookingSessionState.cs` | 해금된 레시피 ID의 `HashSet<string>`. `GameSessionStore` 소유 |
| **BasicStats** | `Common/BasicStats.cs` | `int stamina/time/money`, `int immutableSeed / sessionSeed` |
| **PhaseData** | `Common/PhaseData.cs` | `int Day=1`, `PhaseType Phase=Preparation`, `List<string> UnlockedRecipes` (legacy), `List<string> SelectedMenus` (legacy) |
| **TutorialState** | `Common/TutorialState.cs` | `List<int> shownSteps`, `bool completed` |
| **GardenState** | `Garden/GardenState.cs` | `GardenPersistent persistent` |
| **GardenPersistent** | `Garden/GardenPersistent.cs` | `List<string> upgradeTypes`, `List<int> upgradeLevels` (parallel), `FarmTileSaveData[] tiles` (TileCount=8) |
| **ShopState** | `Shop/ShopState.cs` | `ShopPersistent persistent` |
| **ShopPersistent** | `Shop/ShopPersistent.cs` | `List<string> storageTypes / storageLevels` (parallel), `List<string> toolIds / toolLevels` (parallel) |
| **MallState** | `Mall/MallState.cs` | `MallPersistent persistent` |
| **MallPersistent** | `Mall/MallPersistent.cs` | `List<string> questGroupIds`, `List<int> questStages`, `List<string> normalCycleNpcIds`, `List<int> normalCycleIndices` |
| **InventorySaveData** | `Common/InventorySaveData.cs` | legacy `foodIds/amounts` + 신규 `List<InventoryItemEntry>` (배치=quantity+daysRemaining) |
| **InventoryBatch** | `Common/InventoryBatch.cs` | 런타임: `int quantity`, `int daysRemaining` |
| **MenuSchema** | `Cooking/MenuSchema.cs` | `string name`, `int orderNumber`, `List<FoodData> mainMenus / sideMenus`, `FoodData mainMenu` |
| **FoodSchema** | `Cooking/FoodSchema.cs` | `FoodData foodData`, `int Price` |
| **CookingToolSchema** | `Cooking/CookingToolSchema.cs` | `CookingToolData`, `List<FoodSchema> Ingredients` (max 7), `FoodSchema result`, `bool locked` + `Cook(food, recipe, score, chainDepth)` 로직 |
| **MenuSelection** | `Cooking/MenuSelection.cs` | 런타임 3슬롯 |
| **RecipeBookSaveData** | `Cooking/RecipeBookSaveData.cs` | MenuSelection 저장 형태 |
| **UnlockedRecipesSaveData** | `Cooking/UnlockedRecipesSaveData.cs` | 해금 레시피 저장 |
| **FarmTileSaveData** | `Garden/FarmTileSaveData.cs` | `string cropId`, `int plantedPhase` |
| **ItemShopSlotInfo** | `Shop/ItemShopSlotInfo.cs` | `FoodData item`, `int stock` (-1=∞), `ProductType type` |
| **DeliveryOrderData** | `Mall/DeliveryOrderData.cs` | 배달 주문 데이터 |

Namespace 노트: `TutorialState`만 `Game.Schema.State`. 나머지 State POCO는 다수 전역 namespace (레거시 잔재).

## 3. Domain 서비스 (18종)

### 3-1. 글로벌 통계 / 진행

#### StatsService
- 경로: `Assets/Scripts/Domain/Common/StatsService.cs`
- 책임: `BasicStats` 라이브 참조 (Func closure). 스탯 조회/설정, side-effect 없음.
- 주 API: `SetTime`, `AddMinutes`, `AddHours`, `SetStamina`, `AddStamina`, `SetMoney`, `AddMoney`, `TrySpend`
- 이벤트: `OnTimeChanged(int hour,int min)`, `OnStaminaChanged(int)`, `OnMoneyChanged(int)`, `OnStaminaExhausted()`
- 참조 State: `BasicStats` (Func 라이브)

#### ProgressService
- 경로: `Assets/Scripts/Unity/Common/ProgressService.cs` (**Game.Unity 배치** — Scene/Save/Weather side-effect)
- 책임: Day/Phase 진행. `TimePhaseProvider` 구현.
- 주 API: `PassPhase`, `PassDay`, `SetPhaseTime`, `GetPhaseStartMinutes`, `GetPhaseEndMinutes`, `CumulativePhaseIndex`
- 이벤트: `OnPhaseChanged(PhaseType)`
- 참조 State: `PhaseData` + private `_cumulativePhaseIndex`
- 상수: `TotalPhaseCount=5` (Preparation/Morning/Afternoon/Evening/Night)
- 의존: GameSessionRoot(Stats/Inventory/Weather/Settlement), SceneLoader, SaveManager, GameRandom

### 3-2. 카탈로그

#### CropCatalogService
- 경로: `Assets/Scripts/Domain/Garden/CropCatalogService.cs`
- 책임: Crop 마스터 조회, 가중치 랜덤 픽 (`GameRandom.WeightedPick`)
- 주 API: `GetCrop(id)`, `PickRandomCrop()`, `All`
- 상태 없음 (in-memory `List<CropData>`)

### 3-3. 업그레이드 (동일 패턴)

세 서비스 모두 CSV → `IEnumerable<UpgradeData>` 주입, `IMoneyService` + `IExpenseLog` 사용.

#### FarmUpgradeService
- 경로: `Assets/Scripts/Domain/Garden/FarmUpgradeService.cs`
- State: `GardenPersistent.upgradeTypes / upgradeLevels` (parallel List)
- 주 API: `GetLevel(type)`, `GetCost(type, level)`, `GetValue(type, level)`, `TryUpgrade(type)`
- 타입: `tile`, `timeReduction`, `harvestCount`

#### StorageUpgradeService
- 경로: `Assets/Scripts/Domain/Shop/StorageUpgradeService.cs`
- State: `ShopPersistent.storageTypes / storageLevels`
- 타입: `refrigerator`, `upperShelf`, `lowerShelf`
- `GetCapacity(type)` — InventoryService 용량 검증

#### ToolUpgradeService
- 경로: `Assets/Scripts/Domain/Shop/ToolUpgradeService.cs`
- State: `ShopPersistent.toolIds / toolLevels`
- toolId: T001~T005 (Bake/Boil/Sauce·Mix/Slice/Griddle)

### 3-4. 상점

#### PurchaseService
- 경로: `Assets/Scripts/Domain/Shop/PurchaseService.cs`
- 책임: 재료 상점 구매, phase 캐시 (`day<<8 | phase`), Special 재고 트래킹
- 주 API: `GetPhaseItems(day, phase)`, `TryBuy(item)`, `GetSpecialStock(itemId)`
- 상수: `specialPickCount=4` (페이즈당 랜덤 픽)
- 의존: `ShopConfigSO`, `InventoryService`, `IMoneyService`, `IExpenseLog`, `GameRandom.PhaseRandom`

### 3-5. Mall

#### DeliveryQuestService
- 경로: `Assets/Scripts/Domain/Mall/DeliveryQuestService.cs`
- 책임: NPC 배달 퀘스트 stage set/get (분기는 호출자 담당)
- State: `MallPersistent.questGroupIds / questStages`
- API: `GetStage(groupId)`, `SetStage(groupId, stage)`

#### NpcNormalDialogueService
- 경로: `Assets/Scripts/Domain/Mall/NpcNormalDialogueService.cs`
- 책임: NPC 일반 대사 사이클 index (mod sectionCount)
- State: `MallPersistent.normalCycleNpcIds / normalCycleIndices`

#### OrderService
- 경로: `Assets/Scripts/Domain/Mall/OrderService.cs`
- 책임: 배달 주문 상태. `IOrderReader + IOrderCommand` 구현
- API: `AddOrder`, `RemoveOrder`, `ConsumeBento` (`IMoneyService.Add` 트리거)
- 상태: private `List<DeliveryOrderData>`

#### QuestMenuCatalog
- 경로: `Assets/Scripts/Domain/Mall/QuestMenuCatalog.cs`
- 책임: groupId → MenuSchema 매핑
- 로드: `GameSessionRoot.ParseQuestMenus()` (line 108-141) — deliveryQuest.csv 1회 파싱

### 3-6. 인벤토리 / 요리 도메인

#### InventoryService
- 경로: `Assets/Scripts/Domain/Common/InventoryService.cs`
- 책임: Store의 `InventoryState`를 변경하는 배치 기반 인벤토리 규칙 (batch = quantity + daysRemaining), `LoadInventoryUsecase` 구현
- State: 생성자로 주입받은 `GameSessionStore.State.inventory`; 서비스 내부에 별도 재고 컬렉션을 만들지 않음
- 주 API: `Add`, `Remove`, `Count`, `AdvanceDay` (일별 유통기한 감소), `CanAcceptType`, `Save/Load`
- 저장: `InventorySaveData` (legacy `foodIds/amounts` fallback 포함)
- 의존: `StorageUpgradeService` (capacity 조회), `IEnumerable<FoodData>` 카탈로그
- 상수: `StartingIngredients` 9종 × 3개 (line 19-24)

#### MenuSelectionService
- 경로: `Assets/Scripts/Domain/Cooking/MenuSelectionService.cs`
- 책임: Store의 `MenuSelectionState`를 변경하는 도시락 3슬롯 규칙
- State: 생성자로 주입받은 `GameSessionStore.State.menuSelection`; 서비스 내부에 별도 슬롯 배열을 만들지 않음
- 주 API: `SetMenu(slot, food)`, `ClearMenu(slot)`, `GetMenu(slot)`, `Save/Load`
- 저장: `RecipeBookSaveData` + `PhaseData.SelectedMenus` legacy fallback

#### UnlockedFoodService
- 경로: `Assets/Scripts/Domain/Cooking/UnlockedFoodService.cs`
- 책임: Store의 `UnlockedFoodState`를 변경하는 레시피 해금 Set. `IUnlockedFoodProvider` 구현.
- State: 생성자로 주입받은 `GameSessionStore.State.unlockedFood`; 서비스 내부에 별도 해금 Set을 만들지 않음
- 기본 해금: I044 / I060 / I046 / I062
- 주 API: `IsUnlocked`, `Unlock`, `Save/Load`
- 저장: `UnlockedRecipesSaveData`

#### RecipeLookupService
- 경로: `Assets/Scripts/Domain/Cooking/RecipeLookupService.cs`
- 책임: (toolId + 정렬된 재료ID) → RecipeData. `SearchRecipeUsecase` 구현
- Static: minigameId ↔ toolId 매핑 (M001→T001 등)
- 상태 없음

### 3-7. 유틸

#### WeatherService
- 경로: `Assets/Scripts/Domain/Common/WeatherService.cs`
- 책임: 일별 날씨 결정 (`BadWeatherChance=0.4f`)
- 사전 조건: `GameRandom.InitDay` 호출 후에만 결정
- API: `IsBadWeather`, `UpdateWeather(day)`
- 의존: `GameRandom.Immutable`

#### SettlementService
- 경로: `Assets/Scripts/Domain/Mall/SettlementService.cs`
- 책임: 일별 income/expense 누적, DayStartMoney 스냅샷
- 상수: `ManagementFee = 1000`
- API: `AddIncome(cat, amt)`, `AddExpense(cat, amt)`, `GetIncome/GetExpense`, `Reset`, `DayStartMoney`

#### TutorialService
- 경로: `Assets/Scripts/Domain/Common/TutorialService.cs`
- 책임: 튜토리얼 shownSteps Set + completed flag
- API: `MarkShown(int)`, `HasShown`, `Complete()`, `IsCompleted`
- 이벤트: `OnStepShown(int)`, `OnCompleted()` ⚠️ **구독자 0건 (dead events)**
- 참조 State: `TutorialState`

## 4. Adapter / Provider

### StatsMoneyAdapter (`Assets/Scripts/Unity/Common/StatsMoneyAdapter.cs`)
- `IMoneyService` 구현
- `GameSessionRoot.Instance.Stats` 위임: `Current`, `TrySpend`, `Add`
- `GameSessionRoot.WireServices` line 56에서 생성

### SettlementExpenseAdapter (`Assets/Scripts/Unity/Common/SettlementExpenseAdapter.cs`)
- `IExpenseLog` 구현
- `SettlementService.AddExpense(cat, amt)` 위임
- Line 57에서 생성

### CatalogProvider (`Assets/Scripts/Unity/Common/CatalogProvider.cs`)
- `[DefaultExecutionOrder(-1000)]` SingletonMonoBehaviour (GameSessionRoot보다 먼저 Awake)
- 노출 카탈로그: `Food`, `Recipe`, `Ingredient`, `CookingTool`, `DeliveryNpc`, `DialogueConfig`, `NpcNormalDialogue`, `Prefabs`, `FoodShopConfig`, `Csvs`, `CropSprites`
- BGM 클립: `BgmMall`, `BgmCooking`, `BgmNight`
- OnValidate에서 null 경고

## 5. 인터페이스 위치 (Schema/Interfaces/)

### Common
- `IMoneyService.cs` — `Current`, `TrySpend`, `Add`
- `IExpenseLog.cs` — `Add(category, amount)`
- `IUnlockedFoodProvider.cs`
- `INpcInteraction.cs`
- `IBentoValidator.cs`
- `IOrderReader.cs`, `IOrderCommand.cs`
- `CsvModelConverter.cs`

### Cooking
- `LoadInventoryUsecase.cs`, `SearchRecipeUsecase.cs`, `PlayMinigameUsecase.cs`, `ISelectMenu.cs`

### Garden
- `TimePhaseProvider.cs` (ProgressService가 구현)

## 6. 저장 / 로드 계약

Stats / Progress / Tutorial / Inventory / MenuSelection / UnlockedFood — 모두 `GetSaveData()` / `ApplySaveData(...)` 페어. Order는 `GetOrders` / `SetOrders`. FarmUpgrade/StorageUpgrade/ToolUpgrade/DeliveryQuest/NpcNormalDialogue는 parallel List 직접 저장 (JSON 호환).

SaveManager (`Assets/Scripts/Unity/Common/SaveManager.cs`)가 `gamedata.json` 단일 파일로 통합. `MigrateLegacyIfNeeded()` 5개 legacy JSON → 신규 형식 마이그레이션.

## 7. 관찰

- **ProgressService만 Game.Unity에 위치** — Scene/Save/Sound side-effect 밀집. Domain 순수성 예외.
- **Parallel List 저장 패턴** — Farm/Storage/Tool/Quest/NpcNormal 모두 `List<string> + List<int>` 페어.
- **PassDay 트리거 chain** — Progress.PassDay가 Stats.SetStamina(100) + Inventory.AdvanceDay + Weather.UpdateWeather + Settlement.Reset 연쇄.
- **캐시 트리거** — PurchaseService는 `(day, phase)` key, TutorialService는 shownSteps.Count 미스매치 시 rebuild.
- **정적 상수 위치** — `SettlementService.ManagementFee=1000`, `WeatherService.BadWeatherChance=0.4f`, `ProgressService.TotalPhaseCount=5`, `GardenPersistent.TileCount=8`.
