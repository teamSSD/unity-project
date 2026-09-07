# Save System

Aftertaste의 게임 저장/로드 파이프라인. 단일 파일 `gamedata.json` 에 모든 도메인 상태 통합 저장. Legacy 5-파일 포맷 자동 마이그레이션 포함.

관련 코드:
- `SaveManager` — 저장/로드 진입점 (`Assets/Scripts/Unity/Common/SaveManager.cs`).
- `SaveRepository` — 게임 세이브의 검증·백업·원자적 교체 (`Assets/Scripts/Unity/Common/Persistence/SaveRepository.cs`).
- `DataSaveUtil` — 설정과 legacy 파일을 읽기 위한 호환 유틸 (`Assets/Scripts/Domain/Common/DataSaveUtil.cs`).
- `GameSaveData` — 최상위 wrapper (`Assets/Scripts/Unity/Common/SaveManager.cs:8`).
- Save Adapters — 도메인별 슬롯 변환 (`Assets/Scripts/Unity/Common/SaveAdapters/`).

## 1. 저장 파일 위치

`SaveManager.cs:74`.

```csharp
private static string Dir => Application.persistentDataPath + "/saves";
private static string SavePath => Dir + "/gamedata.json";
```

실경로:
- Windows: `%userprofile%\AppData\LocalLow\<company>\<product>\saves\gamedata.json`
- macOS: `~/Library/Application Support/<company>/<product>/saves/gamedata.json`
- WebGL: IndexedDB (IDBFS) 하위 `/idbfs/<hash>/saves/gamedata.json`

`SaveRepository`는 `gamedata.json.tmp`에 먼저 기록하고 역직렬화 검증 후 기존 파일을 교체한다. 정상인 이전 파일은 `gamedata.json.bak`으로 보존하며 primary가 손상되면 backup을 읽는다.

## 2. GameSaveData 구조

`Assets/Scripts/Unity/Common/SaveManager.cs:7`.

```csharp
[System.Serializable]
public class GameSaveData
{
    public int schemaVersion = 1;
    public PhaseData phase = new();
    public BasicStats stats = new();
    public InventorySaveData inventory = new();
    public OrderSaveData orders = new();
    public DeliveryQuestSaveData deliveryQuest = new();
    public NpcNormalCycleSaveData npcNormalCycle = new();
    public ToolUpgradeSaveData toolUpgrades = new();
    public StorageUpgradeSaveData storageUpgrades = new();
    public FarmUpgradeSaveData farmUpgrades = new();
    public FarmTilesSaveData farmTiles = new();
    public RecipeBookSaveData recipeBook = new();
    public UnlockedRecipesSaveData unlockedRecipes = new();
    public TutorialSaveData tutorial = new();
}
```

### 2.1 필드 상세

#### `phase` — `PhaseData`
`Assets/Scripts/Schema/State/Common/PhaseData.cs`.
- `int Day` — 현재 일수 (SSOT).
- `PhaseType Phase` — 현재 페이즈 (Preparation/Morning/Afternoon/Evening/Night).
- `List<string> UnlockedRecipes` — **레거시**. 신규 파일은 `unlockedRecipes` 슬롯 사용. Load 시 fallback.
- `List<string> SelectedMenus` — **레거시**. 신규는 `recipeBook` 슬롯 사용.

#### `stats` — `BasicStats`
`Assets/Scripts/Schema/State/Common/BasicStats.cs`.
- `int stamina` — 현재 스태미나. Preparation 진입 시 100.
- `int time` — 현재 시각 (분 단위, 0~1439).
- `int money` — 보유 골드. 시작값 12,000G.
- `int immutableSeed` — 세이브별 고정 seed.
- `int sessionSeed` — Continue 시 세션마다 갱신.

#### `inventory` — `InventorySaveData`
`Assets/Scripts/Schema/State/Common/InventorySaveData.cs`.
- `List<InventoryItemEntry> items` — 신규 배치 형식.
  - `string foodId`.
  - `List<InventoryBatchEntry> batches`: `{ int quantity, int daysRemaining }`.
- `List<string> foodIds` + `List<int> amounts` — **레거시**. 단일 배치로 복원.

#### `orders` — `OrderSaveData`
`Assets/Scripts/Schema/State/Mall/OrderSaveData.cs`.
- `List<OrderEntry> entries`.
  - `string questId` (예: `"quest_gabriel_solo"`)
  - `int orderNumber`
  - `string menuName`
  - `string mainMenuId`
  - `List<string> sideMenuIds`
  - `int state` — `DeliveryOrderState` enum (Ordered/Cooked/Delivered).
  - `string npcId`
  - `int cookedPrice`

#### `deliveryQuest` — `DeliveryQuestSaveData`
`Assets/Scripts/Unity/Common/SaveManager.cs:62`.
- `List<string> groupIds` + `List<int> stages` (parallel).
- stage 값 = `DeliveryQuestStage` int enum (FirstMeet/QuestStart/Ordering/OrderEnd/Normal/Completed).

#### `npcNormalCycle` — `NpcNormalCycleSaveData`
`Assets/Scripts/Schema/State/Mall/NpcNormalCycleSaveData.cs`.
- `List<string> npcIds` + `List<int> indices` (parallel).
- NPC별 일반 대사 재생 인덱스.

#### `toolUpgrades` — `ToolUpgradeSaveData`
`Assets/Scripts/Unity/Common/SaveManager.cs:52`.
- `List<string> ids` (T001~T005) + `List<int> levels`.

#### `storageUpgrades` — `StorageUpgradeSaveData`
`Assets/Scripts/Unity/Common/SaveManager.cs:42`.
- `List<string> types` (refrigerator/upperShelf/lowerShelf) + `List<int> levels`.

#### `farmUpgrades` — `FarmUpgradeSaveData`
`Assets/Scripts/Unity/Common/SaveManager.cs:32`.
- `List<string> types` (tile/timeReduction/harvestCount) + `List<int> levels`.

#### `farmTiles` — `FarmTilesSaveData`
`Assets/Scripts/Schema/State/Garden/FarmTilesSaveData.cs`.
- `FarmTileSaveData[] tiles` — 크기 8 고정 (`GardenPersistent.TileCount`).
- `FarmTileSaveData`: `{ string cropId, int plantedPhase }`.

#### `recipeBook` — `RecipeBookSaveData`
`Assets/Scripts/Schema/State/Cooking/RecipeBookSaveData.cs`.
- `List<string> selectedMenus` — 형식: `"mainId|side1,side2,..."`.
- `MenuSelectionService`가 자체 보유.

#### `unlockedRecipes` — `UnlockedRecipesSaveData`
`Assets/Scripts/Schema/State/Cooking/UnlockedRecipesSaveData.cs`.
- `List<string> recipeIds` — 해금된 food id (I044 등).
- Load 시 비어 있으면 `PhaseData.UnlockedRecipes` (레거시) fallback.

#### `tutorial` — `TutorialSaveData`
`Assets/Scripts/Domain/Common/TutorialService.cs:86`.
- `List<int> shownSteps` — 이미 표시한 튜토리얼 스텝 id.
- `bool completed` — true면 모든 hook 비활성.

## 3. 저장 시점 (SaveAll)

**단 1회 지점**: `ProgressService.PassDay()` — `Assets/Scripts/Unity/Common/ProgressService.cs:87` 에서 `SaveManager.SaveAll()` 호출.

즉, **하루 종료(Settlement 씬) 직후에만 저장**. 페이즈 도중 종료하면 그날 진행분 유실.

예외: `GameStart.NewGame()` — `Assets/Scripts/Unity/Common/GameStart.cs:85` 새 게임 시작 시 초기값 저장.

### SaveAll 흐름 (`SaveManager.cs:97`)

```csharp
var save = new GameSaveData();

// 1. 글로벌 서비스 캡처
save.phase     = GameSessionRoot.Instance.Progress.PhaseData;
save.stats     = GameSessionRoot.Instance.Stats.GetSaveData();
save.inventory = GameSessionRoot.Instance.Inventory.GetSaveData();

// 2. 도메인 Adapter (Persistent → SaveData 변환)
GardenSaveAdapter.Capture(save);   // farmUpgrades, farmTiles
ShopSaveAdapter.Capture(save);     // storageUpgrades, toolUpgrades
MallSaveAdapter.Capture(save);     // orders, deliveryQuest, npcNormalCycle

// 3. Self-contained 서비스 (Piggyback 분리 후, H)
save.recipeBook       = GameSessionRoot.Instance.MenuSelection.GetSaveData();
save.unlockedRecipes  = GameSessionRoot.Instance.UnlockedFood.GetSaveData();
save.tutorial         = GameSessionRoot.Instance.Tutorial.GetSaveData();

SaveWriteResult result = Repository.Save(save);
if (!result.Succeeded) return false;

#if UNITY_WEBGL && !UNITY_EDITOR
    SyncFiles();  // 저장 성공 후에만 FS.syncfs → IndexedDB flush
#endif
```

## 4. 로드 시점 (LoadAll)

**단 1회 지점**: `GameStart.ProcessContinue()` — `Assets/Scripts/Unity/Common/GameStart.cs:46` 에서 `SaveManager.LoadAll()` 호출.

### LoadAll 흐름 (`SaveManager.cs:127`)

```csharp
MigrateLegacyIfNeeded();
var save = DataSaveUtil.LoadData(new GameSaveData(), SavePath);

var session = GameSessionRoot.Instance;
session.Progress.ApplySaveData(save.phase);
session.Stats.ApplySaveData(save.stats);
session.Inventory.ApplySaveData(save.inventory);

GardenSaveAdapter.Apply(save);
ShopSaveAdapter.Apply(save);
MallSaveAdapter.Apply(save);

// UnlockedRecipes: 신규 슬롯 우선, 없으면 PhaseData.UnlockedRecipes 폴백
if (save.unlockedRecipes.recipeIds.Count > 0)
    session.UnlockedFood.ApplySaveData(save.unlockedRecipes);
else
    session.UnlockedFood.LoadUnlocksFromProgressLegacy(save.phase);

// RecipeBook: 신규 슬롯 우선, 없으면 PhaseData.SelectedMenus 폴백
if (save.recipeBook.selectedMenus.Count > 0)
    session.MenuSelection.ApplySaveData(save.recipeBook);
else
    session.MenuSelection.LoadMenusFromProgressLegacy(save.phase);

session.Tutorial.ApplySaveData(save.tutorial);
```

## 5. Save Adapters (도메인 변환)

### GardenSaveAdapter — `Assets/Scripts/Unity/Common/SaveAdapters/GardenSaveAdapter.cs`
- **Capture**: `GardenPersistent.upgradeTypes/upgradeLevels` → `FarmUpgradeSaveData`, `GardenPersistent.tiles.Clone()` → `FarmTilesSaveData`.
- **Apply**: 서비스 초기화로 채워진 upgradeTypes를 유지하며 saved level만 덮어씀. 타일은 정확히 복원.

### ShopSaveAdapter — `Assets/Scripts/Unity/Common/SaveAdapters/ShopSaveAdapter.cs`
- **Capture**: `storageTypes/storageLevels` + `toolIds/toolLevels` → `StorageUpgradeSaveData` + `ToolUpgradeSaveData`.
- **Apply**: `MergeLevels(keys, values, savedKeys, savedValues)` — 기존 key 유지, saved value로 덮어씀.

### MallSaveAdapter — `Assets/Scripts/Unity/Common/SaveAdapters/MallSaveAdapter.cs`
- **Capture**:
  - `OrderService.GetOrders()` → `OrderSaveData` (FoodData 참조 → string id 로 flat).
  - `MallPersistent.questGroupIds/questStages` → `DeliveryQuestSaveData`.
  - `MallPersistent.normalCycleNpcIds/normalCycleIndices` → `NpcNormalCycleSaveData`.
- **Apply**:
  - `sideMenuIds` → `SearchDataUtil.GetFoodDataById(id)` 로 hydration.
  - `OrderService.SetOrders(rebuilt)`.
  - 나머지는 그대로 복원.

## 6. Legacy 마이그레이션 (5→1 파일)

`SaveManager.MigrateLegacyIfNeeded()` — `Assets/Scripts/Unity/Common/SaveManager.cs:164`.

**대상 legacy 파일** (같은 saves 디렉토리):
1. `progress.json` — `PhaseData`.
2. `stats.json` — `BasicStats`.
3. `inventory.json` — `InventorySaveData`.
4. `orders.json` — `OrderSaveData`.
5. `deliveryQuest.json` — `DeliveryQuestSaveData`.

**로직**:
1. 신규 `gamedata.json` 있으면 skip.
2. 없고 `progress.json` 이 존재하면 (구버전 유저) — 5개 파일 각각 로드.
3. `GameSaveData` 조합 후 `SaveRepository`로 기록·재검증·교체.
4. 신규 저장 성공이 확인된 경우에만 5개 legacy 파일 delete (`DeleteLegacyFile`).
5. WebGL: `SyncFiles()` 호출.

호출 지점:
- `HasSaveData()` (Continue 버튼 활성 판단 시).
- `LoadAll()` (실제 로드 시).

## 7. WebGL FS.syncfs 처리

`Assets/Plugins/WebGL/SaveSync.jslib` — IDBFS in-memory 캐시를 IndexedDB에 flush.

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern void SyncFiles();
#endif
```

호출 지점 (매 저장 후):
- `SaveAll()` 완료 시.
- `MigrateLegacyIfNeeded()` 마이그레이션 완료 시.

**주의**: 이 호출 없으면 브라우저 강제 종료 시 저장 유실. 탭 정상 종료 시엔 자동 flush 됨.

## 8. `HasSaveData` (Continue 버튼)

`SaveManager.HasSaveData()` — `Assets/Scripts/Unity/Common/SaveManager.cs:87`.
```csharp
MigrateLegacyIfNeeded();
SaveReadResult result = Repository.Load();
return result.Succeeded && result.Found;
```

`GameStart.Start()` 에서 `ContinueButton.interactable = hasSaveData` 로 사용한다. primary 또는 backup 어느 쪽도 검증되지 않으면 Continue를 활성화하지 않는다.

## 9. `Piggyback` 분리 (진단 항목 H)

기존에 `PhaseData` 안에 `UnlockedRecipes`, `SelectedMenus` 를 piggyback으로 넣어두었던 것을 Phase 2 Sprint 2에서 별도 슬롯으로 분리:
- `recipeBook` (RecipeBookSaveData).
- `unlockedRecipes` (UnlockedRecipesSaveData).

`PhaseData` 안의 두 필드는 아직 남아 있지만 legacy fallback 용도만 (`LoadUnlocksFromProgressLegacy`, `LoadMenusFromProgressLegacy`).

## 10. 에러 처리

`SaveRepository`는 저장·로드 결과를 명시적으로 반환한다.
- 저장 실패 시 기존 primary와 backup을 보존하고 `SaveAll()`이 `false`를 반환한다.
- 로드 전 필수 상태, schema version, parallel list 길이를 검증한다.
- primary가 손상됐지만 backup이 정상이면 backup을 반환하고 경고한다.
- primary와 backup 모두 손상됐으면 상태를 적용하지 않고 `LoadAll()`이 `false`를 반환한다.
