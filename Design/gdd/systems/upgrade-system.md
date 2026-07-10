# Upgrade System

Aftertaste의 3개 업그레이드 라인 — 조리 도구 / 저장소 / 텃밭. 모두 CSV 테이블 기반, POCO Service 패턴, 상태는 도메인 Persistent에 저장.

관련 코드:
- `ToolUpgradeService` — `Assets/Scripts/Domain/Shop/ToolUpgradeService.cs`.
- `StorageUpgradeService` — `Assets/Scripts/Domain/Shop/StorageUpgradeService.cs`.
- `FarmUpgradeService` — `Assets/Scripts/Domain/Garden/FarmUpgradeService.cs`.
- UI 어댑터: `ShopUIAdapter.UpgradeTabs.cs` — `Assets/Scripts/Unity/Shop/ShopUIAdapter.UpgradeTabs.cs`.

## 1. 공통 패턴

세 서비스 모두 동일 흐름:

1. **CSV 로드** — `GameSessionRoot.WireCatalogAndUpgrades()` (`Assets/Scripts/Unity/Common/GameSessionRoot.cs:70`) 에서 `CsvModelConverter.Parse<T>` 로 파싱.
2. **테이블 구성** — `_table = rows.GroupBy(key).ToDictionary(g.Key, g.ToList())`.
3. **상태 저장** — Persistent 상태(GardenPersistent / ShopPersistent)의 `parallel List<string> keys` + `List<int> levels`.
4. **결제 위임** — `IMoneyService.TrySpend(cost)` 성공 시 `IExpenseLog.Add("업그레이드", cost)` → `SettlementService.AddExpense`.
5. **레벨 상승** — `SetLevel(key, next.level)`.

공통 API:
- `GetCurrentData(key)` : 현재 레벨 데이터 row.
- `GetNextData(key)` : 다음 레벨 데이터 row (없으면 null).
- `IsMax(key)` : `GetNextData == null`.
- `TryUpgrade(key)` : 골드 차감 + 레벨 상승. 실패 시 false.
- `GetAllTypes()` / `GetAllToolIds()` : 등록된 모든 키.

`TryUpgrade` 실질 로직 (`FarmUpgradeService.cs:57`, 세 서비스 동일):

```csharp
var next = GetNextData(type);
if (next == null) return false;
if (!_money.TrySpend(next.cost)) return false;
_expense.Add("업그레이드", next.cost);
SetLevel(type, next.level);
return true;
```

## 2. 조리 도구 (Tool Upgrade)

### 2.1 CSV: `upgrade_tool.csv`

`Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv`.

헤더: `toolId, level, cost, staminaCost, durationMultiplier`

| toolId | level | cost | staminaCost | durationMultiplier |
|--------|-------|------|-------------|--------------------|
| T001 | 0 | 0 | 5 | 1.00 |
| T001 | 1 | 5000 | 4 | 0.80 |
| T001 | 2 | 12000 | 3 | 0.60 |
| T002 | 0 | 0 | 5 | 1.00 |
| T002 | 1 | 5000 | 4 | 0.80 |
| T002 | 2 | 12000 | 3 | 0.60 |
| T003 | 0 | 0 | 5 | 1.00 |
| T003 | 1 | 5000 | 4 | 0.80 |
| T003 | 2 | 12000 | 3 | 0.60 |
| T004 | 0 | 0 | 5 | 1.00 |
| T004 | 1 | 5000 | 4 | 0.80 |
| T004 | 2 | 12000 | 3 | 0.60 |
| T005 | 0 | 0 | 5 | 1.00 |
| T005 | 1 | 5000 | 4 | 0.80 |
| T005 | 2 | 12000 | 3 | 0.60 |

- **5개 도구, 각 3레벨 (L0/L1/L2)**. 모두 동일 값.
- `durationMultiplier`: 미니게임 duration에 곱하는 배율. L2 = 60% (40% 단축).
- `staminaCost`: 미니게임 완료 시 소진 스태미나. L0 = 5, L2 = 3.

Tool ID → 도구 대응 (`recipe.csv`의 MinigameId로부터 매핑, `MEMORY.md` 참고):
- T001 : 손질/썰기 계열 (M001)
- T002 : 삶기 (M002)
- T003 : 볶기 (M003)
- T004 : 굽기 (M004)
- T005 : 인공고기 스테이크 계열 (M005)

### 2.2 데이터 클래스

`ToolUpgradeData` — `Assets/Scripts/Domain/Shop/ToolUpgradeData.cs`.

```csharp
public string toolId;
public int    level;
public int    cost;
public int    staminaCost;
public float  durationMultiplier;
```

### 2.3 상태 (ShopPersistent)

`ShopPersistent` — `Assets/Scripts/Schema/State/Shop/ShopPersistent.cs`.

```csharp
public List<string> toolIds    = new();
public List<int>    toolLevels = new();
```

### 2.4 런타임 적용

`MiniGameManager.cs:47`.

```csharp
var upgradeData = GameSessionRoot.Instance?.ToolUpgrade?.GetCurrentData(toolId);
float multiplier = upgradeData?.durationMultiplier ?? 1f;
int staminaCost  = upgradeData?.staminaCost ?? 5;
currentGame.ApplyUpgrade(multiplier, staminaCost);
```

- Fallback 있음: 서비스가 null이면 각각 1f / 5 사용.

### 2.5 UI 라벨

`ShopUIAdapter.UpgradeTabs.cs:36`.
```
"미니게임 시간 {cur*100}%→{next*100}%  /  스태미나 {curStamina}→{nextStamina}"
```

## 3. 저장소 (Storage Upgrade)

### 3.1 CSV: `upgrade_storage.csv`

`Assets/Bundles/driveAssets/dataTables/upgrade_storage.csv`.

헤더: `type, level, cost, value`

| type | level | cost | value (칸 수) |
|------|-------|------|---------------|
| refrigerator | 0 | 0 | 7 |
| refrigerator | 1 | 10000 | 10 |
| refrigerator | 2 | 25000 | 13 |
| refrigerator | 3 | 60000 | 15 |
| upperShelf | 0 | 0 | 3 |
| upperShelf | 1 | 10000 | 5 |
| upperShelf | 2 | 25000 | 6 |
| upperShelf | 3 | 60000 | 8 |
| lowerShelf | 0 | 0 | 4 |
| lowerShelf | 1 | 10000 | 6 |
| lowerShelf | 2 | 25000 | 8 |
| lowerShelf | 3 | 60000 | 8 |

- **3종 저장소 × 4레벨 (L0~L3)**.
- `value` = 저장 가능한 유니크 재료 종류 수 (capacity, 슬롯 수).
- 시작 총량 = 7 + 3 + 4 = 14 슬롯 → 시작 재료 9종 커버 가능.
- MAX 총량 = 15 + 8 + 8 = 31 슬롯.

### 3.2 데이터 클래스

`StorageUpgradeData` — `Assets/Scripts/Domain/Shop/StorageUpgradeData.cs`.

```csharp
public string type;
public int    level;
public int    cost;
public int    value;
```

### 3.3 상태 (ShopPersistent)

```csharp
public List<string> storageTypes  = new();
public List<int>    storageLevels = new();
```

### 3.4 런타임 적용

**Cooking 씬 (배치 로직)**: `CookingSceneManager.cs:64`
```csharp
int capacity = upgradeSvc?.GetCurrentData(storage.UpgradeTypeId)?.value ?? fallback;
```
- `Refrigerator`, `UpperShelf`, `LowerShelf` (BaseStorage 상속) 각각 사이즈 결정.

**인벤토리 페이지 UI**: `InventoryPageController.cs:137` — 슬롯 렌더링 개수.

**Inventory Capacity Check**: `InventoryService.CanAcceptType(FoodData food)` — `Assets/Scripts/Domain/Common/InventoryService.cs:120`.
```csharp
string upgradeType = UpgradeTypeFromCategory(food.ingredient.display);
int max = _storage.GetCurrentData(upgradeType)?.value ?? int.MaxValue;
int unique = LoadIngredientsByCategory(food.ingredient.display).Count;
return unique < max;
```
- 카테고리 → 업그레이드 타입 매핑 (`InventoryService.cs:221`):
  - `IngredientDisplayCategory.UpperShelf` → `"upperShelf"`
  - `IngredientDisplayCategory.LowerShelf` → `"lowerShelf"`
  - `IngredientDisplayCategory.Refrigerator` → `"refrigerator"`

### 3.5 UI 라벨

`ShopUIAdapter.UpgradeTabs.cs:92`.
```
"{StorageTypeName} {cur.value}칸→{next.value}칸"
```
- "냉장고 확장" / "윗 찬장 확장" / "아랫 찬장 확장".

## 4. 텃밭 (Farm Upgrade)

### 4.1 CSV: `upgrade_farm.csv`

`Assets/Bundles/driveAssets/dataTables/upgrade_farm.csv`.

헤더: `type, level, cost, value`

| type | level | cost | value |
|------|-------|------|-------|
| tile | 0 | 0 | 3 |
| tile | 1 | 3000 | 4 |
| tile | 2 | 7000 | 5 |
| tile | 3 | 12000 | 6 |
| tile | 4 | 20000 | 7 |
| timeReduction | 0 | 0 | 0.00 |
| timeReduction | 1 | 3000 | 0.16 |
| timeReduction | 2 | 7000 | 0.24 |
| timeReduction | 3 | 12000 | 0.32 |
| timeReduction | 4 | 20000 | 0.40 |
| harvestCount | 0 | 0 | 5 |
| harvestCount | 1 | 3000 | 7 |
| harvestCount | 2 | 7000 | 9 |
| harvestCount | 3 | 12000 | 11 |
| harvestCount | 4 | 20000 | 13 |

- **3종 × 5레벨 (L0~L4)**.
- `tile`: 개방된 텃밭 타일 수 (하드 상한 8, `GardenPersistent.TileCount = 8`; 현재 CSV 최대 7이라 하나 미사용).
- `timeReduction`: 수확 시간 감소 비율 (0.00 → 0.40 = 40% 단축).
- `harvestCount`: 한 번 수확 시 얻는 작물 수 (L0=5, MAX=13).

### 4.2 데이터 클래스

`FarmUpgradeData` — `Assets/Scripts/Domain/Garden/FarmUpgradeData.cs`.

```csharp
public string type;
public int    level;
public int    cost;
public float  value;
```

### 4.3 상태 (GardenPersistent)

`GardenPersistent` — `Assets/Scripts/Schema/State/Garden/GardenPersistent.cs`.

```csharp
public List<string> upgradeTypes  = new();
public List<int>    upgradeLevels = new();

public const int TileCount = 8;
public FarmTileSaveData[] tiles = new FarmTileSaveData[TileCount];
```

### 4.4 런타임 적용

**tile 잠금 체크**: `Farm.cs:29`
```csharp
public bool IsLocked => farmIndex >= (int)(GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("tile")?.value ?? 3);
```

**harvestCount 조회**: `Farm.cs:93`
```csharp
int harvestCount = (int)(GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("harvestCount")?.value ?? 5);
if (tile.Harvest(out string id, out int crops, harvestCount)) { ... }
```

**timeReduction 조회**: `FarmTile.cs:25`
```csharp
float timeReduction = GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("timeReduction")?.value ?? 0f;
int requiredPhases = Mathf.CeilToInt(crop.growPhaseCount * (1f - timeReduction));
```
- L0: 원본 growPhaseCount. L4: 60% (40% 단축) 후 올림.

### 4.5 UI 라벨

`ShopUIAdapter.UpgradeTabs.cs:155`.
```csharp
"tile"          => "타일 {cur}개→{next}개",
"timeReduction" => "수확 시간 -{cur*100:0}%→-{next*100:0}%",
"harvestCount"  => "수확량 {cur}개→{next}개",
```

## 5. 초기화 (Persistent 자동 부트)

세 서비스 모두 생성자에서 CSV row에 있는 모든 key를 Persistent에 등록 (없으면 level 0로 push). `FarmUpgradeService.cs:30`.

```csharp
foreach (var t in _table.Keys)
    if (!_state.upgradeTypes.Contains(t))
    {
        _state.upgradeTypes.Add(t);
        _state.upgradeLevels.Add(0);
    }
```

Load 시에는 `GardenSaveAdapter.Apply` / `ShopSaveAdapter.Apply`가 기존 key 유지하며 saved level만 덮어씀 (`Assets/Scripts/Unity/Common/SaveAdapters/`).

## 6. 결제 흐름 (요약)

```
UI 클릭
  → ShopUIAdapter.UpgradeTabs (Populate/ShowDetail)
  → Service.TryUpgrade(key)
      → IMoneyService.TrySpend(cost)   // StatsMoneyAdapter → Stats.SubMoney
      → IExpenseLog.Add("업그레이드", cost)  // SettlementExpenseAdapter → SettlementService.AddExpense
      → SetLevel(key, next.level)     // Persistent에 반영
```

Settlement 화면에서 "업그레이드" 카테고리로 집계됨 (`settlement-system.md`).
