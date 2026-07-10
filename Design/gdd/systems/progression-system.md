# Progression System

Aftertaste의 장기 진행 곡선. 시작 자산부터 Day별 오픈 콘텐츠, 레시피 해금 흐름, 머니 싱크(업그레이드 총 코스트), 배달 퀘스트 해금 순서까지 다룬다.

관련 서비스:
- `GameSessionRoot` — 컴포지션 루트, 모든 서비스 wiring 진입점 (`Assets/Scripts/Unity/Common/GameSessionRoot.cs`).
- `ProgressService` — Day / Phase 진행 (`Assets/Scripts/Unity/Common/ProgressService.cs`).
- `UnlockedFoodService` — 레시피 해금 상태 (`Assets/Scripts/Domain/Cooking/UnlockedFoodService.cs`).
- `DeliveryQuestService` — NPC 배달 퀘스트 단계 (`Assets/Scripts/Domain/Mall/DeliveryQuestService.cs`).

## 1. 시작 자산 (New Game)

`GameStart.ApplyNewGameDefaults()` — `Assets/Scripts/Unity/Common/GameStart.cs:98`.

| 필드 | 초기값 | 비고 |
|------|--------|------|
| `Stats.money` | **12000G** | `session.Stats.SetMoney(12000)` |
| `Stats.stamina` | 100 | `session.Stats.SetStamina(100)` |
| `Stats.time` | 05:00 | `session.Stats.SetTime(5, 0)` (Preparation 시작 시각) |
| `PhaseData.Day` | 0 | `session.Progress.PhaseData.Day = 0` (첫 정산 후 1일차) |
| `PhaseData.Phase` | Preparation | `ProgressService.Initialize()` 기본값 |
| Garden tiles | 전부 empty | `Array.Clear(session.State.garden.persistent.tiles, 0, 8)` |
| Delivery quests | 전부 리셋 | `DeliveryNpcDialogueInteraction.ResetAll()` |
| Inventory | 시작 재료 (아래 표) | `Inventory.ResetToDefault()` |
| Unlocked recipes | 기본 4종 (아래) | `UnlockedFood.UnlockDefaultRecipes()` |

### 시작 재료 (StartingIngredients)

`InventoryService.StartingIngredients` — `Assets/Scripts/Domain/Common/InventoryService.cs:19`.

레벨 0 저장소 용량 (Refrigerator 7 / UpperShelf 3 / LowerShelf 4) 안에 맞추어 총 9종 × 3개.

| Food ID | 재료명 | 초기 수량 |
|---------|--------|-----------|
| I007 | 고추장 | 3 |
| I008 | 레몬 | 3 |
| I009 | 인공고기 | 3 |
| I010 | 루미 계란 | 3 |
| I017 | 스틸루트 | 3 |
| I019 | 검은 된장 | 3 |
| I020 | 조명 시럽 | 3 |
| I026 | (Vegetable 계열) | 3 |
| I027 | (Vegetable 계열) | 3 |

- 시작 unlock 메뉴 3개 (I044 기계장 고기정식, I060 옥상 오믈렛, I046 루미 젤리) 재료 커버.
- I062 환기구 연어구이의 원재료 (raw)는 시작 인벤에 없음 — Shop 구매 필요.

## 2. 레시피 해금 (Unlock Flow)

`UnlockedFoodService` — `Assets/Scripts/Domain/Cooking/UnlockedFoodService.cs`.

### 2.1 기본 해금 (defaultMains / defaultSides)

`UnlockDefaultRecipes()` — `UnlockedFoodService.cs:44`.

| 종류 | Food ID | 이름 | 비고 |
|------|---------|------|------|
| defaultMains[0] | I044 | 기계장 고기정식 | 시작 재료로 조리 가능 |
| defaultMains[1] | I060 | 옥상 오믈렛 | 시작 재료로 조리 가능 |
| defaultSides[0] | I046 | 루미 젤리 | 시작 재료로 조리 가능 |
| defaultSides[1] | I062 | 환기구 연어구이 | 원재료 미포함, 구매 필요 (quest 경로에 없어 default 유지 필수) |

### 2.2 배달 퀘스트를 통한 추가 해금

`DeliveryNpcDialogueInteraction.UnlockMenuRecipes(MenuSchema menu)` — `Assets/Scripts/Unity/Mall/DeliveryNpcDialogueInteraction.Quest.cs:138`.

Quest 단계 `QuestStart` → `accept` 응답 시 `CreateQuestOrder()`가 실행되며, 그때 메뉴의 mainMenu + sideMenus 전부를 unlock. 이후 해당 unlock은 영구 저장(`unlockedRecipes` 슬롯).

### 2.3 배달 퀘스트 해금 순서 (deliveryQuest.csv)

`Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv` 실값.

| GroupId | MenuName | MainMenuId | MainMenu2Id | SideMenu1Id | 해금되는 Food |
|---------|----------|------------|-------------|-------------|---------------|
| `power_room_pair` | 전력실 도시락 | I044 | - | I058 | I044 (이미 default) + I058 전력실 꼬치 |
| `night_market` | 야시장 도시락 | I039 | I049 | - | I039 구룡면, I049 스트리트 스테이크 49 |
| `nimo_solo` | 새벽국 도시락 | I034 | - | - | I034 새벽국 |
| `seraph_solo` | 네온 샐러드 | I060 | - | I056 | I060 (이미 default) + I056 네온 샐러드 |
| `lede_solo` | 삼각밥 도시락 | I053 | - | - | I053 폐건물 삼각밥 |
| `gabriel_solo` | 기계장 고기정식 | I044 | - | - | I044 (이미 default) |

퀘스트 전제 조건 (`prerequisiteGroupId`) — 이전 퀘스트가 `Completed` 상태여야 다음 quest hook 활성 (`IsQuestUnlocked()` — `DeliveryNpcDialogueInteraction.Quest.cs:90`).

### 2.4 Day 15 근방: 전 메뉴 오픈

각 퀘스트당 하루에 하나씩 진행하고 재료 확보 후 조리·배달 완료해야 Complete까지 도달. 6개 quest 그룹 × 대략 2~3일씩 = **Day 12~18 근처에서 모든 메뉴 오픈** 목표. (기본 4개 + 퀘스트 unlock 8개 = MAIN·SIDE 총 12종 커버.)

Quest 단계 흐름 (`DeliveryQuestStage` enum):
```
FirstMeet → QuestStart → (accept) → Ordering → OrderEnd → Completed → Normal (사이클)
```

## 3. Day 진행 (Progress Loop)

### 3.1 Phase 진행

`ProgressService.PassPhase()` — `Assets/Scripts/Unity/Common/ProgressService.cs:53`.
- `Preparation → Morning → Afternoon → Evening → Night → Settlement (씬 전환)`.
- Night 종료 시 `SceneLoader.LoadScene(SceneNames.Settlement)`.

### 3.2 Day 전환

`ProgressService.PassDay()` — `Assets/Scripts/Unity/Common/ProgressService.cs:69`.
- 관리비 차감: `stats.SubMoney(SettlementService.ManagementFee)` = **-1000G**.
- `Day++`, `Phase = Preparation`, `cumulativePhaseIndex++`.
- `stats.SetStamina(100)`, `GameRandom.InitDay(day)`, `Weather.UpdateWeather(day)`.
- `Inventory.AdvanceDay()` — 배치별 `daysRemaining--`, 0 이하면 폐기.
- `Settlement.Reset(currentMoney)` — 다음 하루 정산 초기화.
- `SaveManager.SaveAll()` — 디스크 저장 (실질적으로 이 시점이 유일한 저장 지점).

### 3.3 씬 흐름

`Assets/Scripts/Domain/Common/SceneNames.cs`.
```
Boot → Managers → GameStart → Mall ⇄ (Cooking / Garden / Shop) → Settlement → Mall
```

## 4. 머니 싱크 (Upgrade 총 코스트)

### 4.1 조리 도구 (upgrade_tool.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv`. T001~T005 (5개 tool × 2단계 업그레이드).

| Level | Cost/tool | Sub-total 5 tools |
|-------|-----------|-------------------|
| 0 → 1 | 5,000G | 25,000G |
| 1 → 2 | 12,000G | 60,000G |
| **합계** | 17,000G | **85,000G** |

### 4.2 저장소 (upgrade_storage.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_storage.csv`. 3종 (refrigerator / upperShelf / lowerShelf) × 3단계.

| Level | Cost | 3 slots 합 |
|-------|------|------------|
| 0 → 1 | 10,000G | 30,000G |
| 1 → 2 | 25,000G | 75,000G |
| 2 → 3 | 60,000G | 180,000G |
| **합계** | 95,000G | **285,000G** |

### 4.3 텃밭 (upgrade_farm.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_farm.csv`. 3종 (tile / timeReduction / harvestCount) × 4단계.

| Level | Cost/type | 3 types 합 |
|-------|-----------|------------|
| 0 → 1 | 3,000G | 9,000G |
| 1 → 2 | 7,000G | 21,000G |
| 2 → 3 | 12,000G | 36,000G |
| 3 → 4 | 20,000G | 60,000G |
| **합계** | 42,000G | **126,000G** |

### 4.4 최종 목표

| 카테고리 | 총 코스트 |
|----------|-----------|
| 도구 (T001~T005 L2) | 85,000G |
| 저장소 (3종 L3) | 285,000G |
| 텃밭 (3종 L4) | 126,000G |
| **모든 업그레이드 MAX** | **496,000G** |

일일 관리비 1,000G × Day 60 = 60,000G 지출 상수. Day 60~90 사이 마지막 업그레이드 도달을 목표로 설계 (평균 일 순이익 5~8k G 기준).

## 5. 진행 관련 저장 슬롯

`GameSaveData` (`Assets/Scripts/Unity/Common/SaveManager.cs:8`) — 저장 시 아래 슬롯이 함께 flush됨.
- `phase` (PhaseData): Day / Phase / 레거시 UnlockedRecipes / 레거시 SelectedMenus.
- `unlockedRecipes` (UnlockedRecipesSaveData): 현행 해금 slot.
- `recipeBook` (RecipeBookSaveData): 도시락 메뉴 선택.
- `deliveryQuest` (DeliveryQuestSaveData): NPC별 진행 단계.
- `farmUpgrades` / `storageUpgrades` / `toolUpgrades`: 업그레이드 레벨.
- `stats` (BasicStats): money 포함.

상세는 `save-system.md`.
