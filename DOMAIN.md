# Game Domain Glossary

게임 코드에 등장하는 도메인 용어 정의 + 식별자 매핑.

작성 원칙: 한국어 정의 + 영어 식별자 (코드 클래스/필드명).
업데이트 정책: 새 개념 도입 시 즉시 갱신. 신규 식별자가 동의어를 만들지 않도록 우선 이 문서 참조.

---

## 식별자 접미사 컨벤션

| 접미사 | 역할 | 위치 (asmdef) | 예시 |
|---|---|---|---|
| `Data` | Config(마스터) ScriptableObject | `Game.Schema` | FoodData, RecipeData, CookingToolData |
| `Schema` | State 메모리 모델 (런타임 인스턴스 상태) | `Game.Schema` | FoodSchema, MenuSchema, CookingToolSchema |
| `SaveData` | JSON 직렬화 전용 구조 | `Game.Schema` 또는 `Game.Unity` | InventorySaveData, OrderSaveData |
| `Model` | MonoBehaviour (뷰 + 로직) | `Game.Unity` | BentoModel, OrderTicketModel, CookingToolModel |
| `Behavior` | MonoBehaviour (순수 뷰) | `Game.Unity` | BentoBehavior, FoodBehavior |
| `Manager` | MonoBehaviour 컨트롤러 (대다수) — 씬 수명 또는 전역 Singleton 혼재 | `Game.Unity` | InventoryManager, CustomerManager, FarmUpgradeManager |
| `System` | 핵심 게임 상태 (Stats/Progress/Weather) 한정 | `Game.Unity` | StatsSystem, ProgressSystem, WeatherSystem |
| `CatalogSO` | 자산 목록 SerializeField (Resources.Load 대체) | `Game.Schema` | FoodCatalogSO, PrefabCatalogSO |

---

## Cooking Domain (조리)

### Food
가게에서 다루는 모든 식재료/조리 중간물/완성품의 단위.
- **타입** (`FoodType` enum): `GARBAGE` / `INGREDIENT` / `PROCESSING` / `MAIN` / `SIDE`
- **관계**: Recipe의 입력/출력으로 등장. Bento 슬롯에 배치.
- **코드**: `FoodData` (마스터), `FoodSchema` (런타임 + Price), `FoodBehavior`/`FoodModel` (Unity)
- **예시**: 양배추(`INGREDIENT`) → SliceMiniGame → 슬라이스 양배추(`PROCESSING`) → 양배추말이(`MAIN`)

### Ingredient
`Food` 중 `FoodType.INGREDIENT` 타입의 식재료. **Food의 부분집합** (별도 도메인 아님).
- 단, `IngredientData`는 Food와 1:1 보조 메타데이터 (예: 가게 매대 표시 정보).
- **코드**: `IngredientData`, `IngredientCatalogSO`

### Recipe
입력 Food들 + MiniGame → 출력 Food 1개의 변환 규칙.
- **관계**: 입력은 `RecipeIngredient` 리스트 (Food + weight), 출력은 단일 Food
- **체인**: DFS로 최종 요리에 도달하는 sub-recipe 순서 계산 (RecipeChain)
- **코드**: `RecipeData`, `RecipeCatalogSO`, `RecipeDataManager` (검색)

### MiniGame
도구별 조리 액션. Recipe 실행의 인터랙티브 컴포넌트.
- **유형**: M001~M005 (Tool과 1:1 매핑)
- **코드**: `MiniGameAbstract` 기반, `RecipeDataManager.GetToolIdForMinigame(minigameId)`

### CookingTool
MiniGame을 통해 Food를 변환하는 도구.

| ID | 도구 | toolVariants 인덱스 |
|---|---|---|
| T001 | Pan (팬) | 0 |
| T002 | Pot (냄비) | 1 |
| T003 | Bowl (볼/믹서) | 2 |
| T004 | Knife (칼/커터) | 3 |
| T005 | Plate (판/접시) | 4 |

- **코드**: `CookingToolData`, `CookingToolSchema`, `CookingToolModel`, `CookingToolCatalogSO`

### Bento
손님에게 제공하는 최종 도시락 단위. **Main 1 + Side 3 슬롯** 구성.
- 각 슬롯은 `FoodType.MAIN` 또는 `FoodType.SIDE` Food 배치
- **코드**: `BentoModel` (Unity 뷰+로직), `FoodData.GetBentoImage(slotIndex)`

### Menu
한 페이즈(영업 회차)에 제공할 Bento(s) 또는 단품 라인업.
- **관계**: 메뉴 선택 액션에서 사장이 결정 → 영업 중 Customer가 그 중 주문
- **Recipe와의 차이**: Recipe는 변환 규칙(레시피북), Menu는 영업 라인업(오늘의 메뉴)
- **코드**: `MenuSchema` (런타임 메뉴 인스턴스), `MenuSelection` 씬/UI

### Storage
조리 도중 식재료를 보관하는 위치.
- **유형**: `Refrigerator` / `UpperShelf` / `LowerShelf` (모두 `BaseStorage` 상속)
- **업그레이드**: `StorageUpgradeManager` (수용량 단계적 상승)

---

## Customer / Order Domain

### Customer
**Cooking 씬**에 방문해 주문하는 손님. (Mall 씬의 NPC와 도메인적으로 분리)
- **상태 머신**: `OrderingCustomer` → `WaitingCustomer` → `TakingCustomer` (또는 `ExitingCustomer`)
- **코드**: `CustomerData` (마스터), `CustomerManager` (생명주기), `CustomerSpawner` / `CustomerLifecycle` (분할 컴포넌트)
- **NPC와의 구분**: Customer = Cooking 씬 전용. NPC = Mall 씬의 배달/대화 상대.

### NPC
**Mall 씬**의 배달 NPC, 일반 대화 NPC.
- **코드**: `DeliveryNpcData`, `NPCDialogue`, `DeliveryNpcDialogueInteraction`, `DeliveryNpcCatalogSO`

### Order
Customer 또는 NPC가 요청한 도시락/단품 사양.
- **유형**:
  - **가게 내 주문**: Customer가 발주, Bento로 응답
  - **배달 주문**: NPC가 발주 → `DeliveryOrderData` (상태: Ordered → Cooking → Cooked → Failed → Delivered)
- **코드**: `OrderManager`, `OrderTicketController`, `DeliveryOrderData`

### DeliveryQuest
**NPC별 장기 진행도** 추적. (개별 Order보다 상위 개념)
- **코드**: `DeliveryQuestSaveData` (NPC별 진행 카운트, 누적 결과)
- **Order와의 차이**: Order는 한 번의 요청, Quest는 NPC와의 누적 관계.

---

## Progress / Stats Domain

### Day
게임 진행의 큰 단위. 여러 Phase로 구성.
- **코드**: `PhaseData.day`, `ProgressSystem`

### Phase
하루의 세부 진행 단위.
- **유형** (`PhaseType` enum): `Preparation`, `Morning`, `Afternoon`, `Evening`, `Night`
- **코드**: `PhaseData.Phase`, `ProgressSystem.AdvancePhase()`

### Action
Phase 중 플레이어가 수행하는 활동 선택지.
- **유형** (`ActionType` enum): `Work`, `Rest`, `Shopping` 등
- **코드**: `ActionSelectionManager`

### Stats
캐릭터의 수치 상태.
- **항목**: `stamina`, `day`, `time`, `money`
- **코드**: `BasicStats`, `StatsSystem`

### Settlement
페이즈/Day 종료 시 정산 (매출/비용/순이익).
- **코드**: `SettlementManager`

---

## Shop / Upgrade Domain

### Shop
상점 UI 통합. 단일 책임 매니저는 분리되어 있고 UI만 통합.
- **코드**: `UnifiedShopManager` (UI 통합), `ShopBook` (UI 메타포)
- **판매 항목**: 재료(`IngredientData`) + 업그레이드(`UpgradeData` 계열)

### Upgrade
도구/저장소/농장의 단계적 강화.
- **유형**:
  - `ToolUpgradeManager` → 도구 미니게임 강화
  - `StorageUpgradeManager` → 저장 용량 확장
  - `FarmUpgradeManager` → 농장 슬롯/속도 강화
- **공통 패턴**: `<X>UpgradeManager` + `<X>UpgradeSaveData`

### RecipeBook
플레이어가 열람하는 레시피 인덱스 UI (영업 가능 메뉴 표시).
- **코드**: `RecipeBook` 프리팹/매니저
- **ShopBook과의 차이**: ShopBook = 구매 UI, RecipeBook = 정보 조회 UI

### Farm / Crop / Tile
- **Farm**: 농장 전체 (여러 `FarmTile` 격자)
- **FarmTile**: 단일 농장 슬롯 (Crop 1개 재배 가능)
- **Crop**: 재배 작물 정의 (`CropData`)
- **코드**: `Farm`, `FarmTile`, `CropDataManager`, `FarmUpgradeManager`

---

## Validation / Feedback

### Validation
손님이 받은 Bento를 검수한 결과 점수.
- **항목**: 메뉴 일치, 도시락 구성, 가격 등
- **코드**: `OrderValidation`, `DeliveryValidationResult`

---

## Save / Data Domain

### GameSaveData (직렬화 루트)

```
GameSaveData
├─ PhaseData              (day, PhaseType, UnlockedRecipes, SelectedMenus)
├─ BasicStats             (stamina, day, time, money)
├─ InventorySaveData      (List<InventoryItemEntry> with batches)
├─ OrderSaveData          (List<OrderEntry>)
├─ DeliveryQuestSaveData  (per-NPC 진행도)
├─ ToolUpgradeSaveData
├─ StorageUpgradeSaveData
├─ FarmUpgradeSaveData
└─ FarmTilesSaveData
```

- **저장 위치**: `Application.persistentDataPath/gamedata.json`
- **코드**: `SaveManager`, `GameSaveData` (wrapper)
- **마이그레이션**: 구 5-파일 포맷 → `MigrateLegacyIfNeeded()`

---

## Catalog / Asset Loading (Phase 2-D 결과)

`Resources.Load` 폐기 → `CatalogProvider` 경유.

| Catalog | 용도 | 자산 위치 |
|---|---|---|
| `FoodCatalogSO` | 모든 FoodData | `Assets/Bundles/Catalogs/FoodCatalog.asset` |
| `RecipeCatalogSO` | 모든 RecipeData | `RecipeCatalog.asset` |
| `IngredientCatalogSO` | 모든 IngredientData | `IngredientCatalog.asset` |
| `CookingToolCatalogSO` | T001~T005 도구 | `CookingToolCatalog.asset` |
| `DeliveryNpcCatalogSO` | 배달 NPC | `DeliveryNpcCatalog.asset` |
| `DialogueConfigCatalogSO` | NPC별 Dialogue Config (groupId 매핑) | `DialogueConfigCatalog.asset` |
| `PrefabCatalogSO` | 7개 동적 인스턴스화 프리팹 | `PrefabCatalog.asset` |
| `CsvCatalogSO` | 6개 CSV 데이터테이블 | `CsvCatalog.asset` |
| `CropSpriteCatalogSO` | 작물 스프라이트 (id → Sprite) | `CropSpriteCatalog.asset` |

- **접근**: `CatalogProvider.Food`, `CatalogProvider.Prefabs.OrderTicket`, `CatalogProvider.Csvs.farmUpgrade` 등
- **씬 등록**: `Assets/Scenes/ForReal/Managers.unity`의 `CatalogProvider` GameObject에 SerializeField로 연결

---

## 도메인 다이어그램

```
[Player] ─선택─→ [Menu] ─포함─→ [Bento] ─구성─→ [Food(MAIN), Food(SIDE)×3]
                                                       ↑
                                                  [Recipe] ─적용─ [CookingTool] ─실행─ [MiniGame]
                                                       ↑
                                                  [Food(INGREDIENT, PROCESSING)×N]

Cooking 씬:  [Customer] ─발주─→ [Order]      ─기대─→ [Bento]
Mall 씬:    [NPC]      ─발주─→ [DeliveryOrder] ─기대─→ [Bento]  ↔  [DeliveryQuest] (NPC별 진행도)

[Day] ─구성─→ [Phase × N] ─수행─→ [Action]
[BasicStats: money, stamina, day, time] ─변동─→ [Settlement]

[Shop(UnifiedShopManager)] ┬─판매─→ [Ingredient]
                           ├─판매─→ [ToolUpgrade]
                           ├─판매─→ [StorageUpgrade]
                           └─판매─→ [FarmUpgrade]

[Farm] ─격자─→ [FarmTile × N] ─재배─→ [Crop] ─수확─→ [Food(INGREDIENT)]
```

---

## 결정된 용어 매트릭스

코드베이스가 도메인 분리(Cooking↔Mall, Customer↔NPC)를 이미 깔끔히 적용하여 대규모 rename은 불필요. 다음만 정리:

| 모호한 용어 | 통일 후 | 이유 |
|---|---|---|
| Customer vs NPC | **Customer = Cooking 씬, NPC = Mall 씬** | 도메인별 1:1 매핑 (이미 코드 분리됨) |
| Order vs DeliveryQuest | **Order = 단일 요청, DeliveryQuest = NPC별 누적 진행도** | 상태 vs 진행도 (이미 별도 클래스) |
| Food vs Ingredient | **Food = 모든 식재료/요리 단위, Ingredient = `FoodType.INGREDIENT` 타입 + 매대용 메타** | Food가 상위, Ingredient는 부분집합 + 보조 정보 |
| Menu vs Recipe vs Bento | **Menu = 영업 라인업, Recipe = 변환 규칙, Bento = 최종 도시락 단위** | 책임 분리 명확 |
| Schema vs Data | **Data = Config SO (정적), Schema = State 인스턴스 (런타임)** | 접미사 컨벤션 명시 |
| Model vs Behavior | **Model = 뷰+로직, Behavior = 순수 뷰** | 접미사 컨벤션 명시 |
| Manager vs System | **System = Stats/Progress/Weather 등 핵심 게임 상태 한정. Manager = 그 외 대다수.** | 역사적 분리 인정 (강제 rename 안 함) |

### 후속 rename 후보 (선택적, Phase 2-E 본 PR 외 별도)

현재 식별된 강제 rename 없음. 새 코드 작성 시 이 문서의 컨벤션을 따르면 자연 수렴.

만약 발견되면:
- 잘못된 접미사 사용 (예: `XManager`가 전역 수명인 경우 → `XSystem`)
- 도메인 경계 위반 (예: Mall 씬에서 `Customer` 명칭 사용)
