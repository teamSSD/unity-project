# Garden.unity — 텃밭 씬

> 파일: `Assets/Scenes/ForReal/Garden.unity`
> 진입: Mall의 `FarmPathInteraction`(Space).

## 역할 한 줄

**FarmTile 다수를 격자 배치**한 텃밭 뷰. 심기/성장/수확을 처리, 인벤토리에 결과물 적재.

## GameObject 계층

```
Garden.unity (root)
├── EventSystem
├── Canvas
├── Main Camera                  CameraFollow + DynamicBoundaryWalls
├── background
├── ground
├── SignPanel                    간판 UI (텃밭 상태 표시 등)
├── ExitToMall                   SceneTransitionInteraction → Mall
├── GardenSceneController        SubSceneController 부착 (returnSceneName = "Mall")
└── FarmTile 인스턴스 다수        Prefab (40칸 상당, 잠금은 FarmUpgradeService.tile 값에 따라)
```

**주**: Garden 씬은 41개 PrefabInstance를 포함 — 1개 Player + 40개 FarmTile로 추정.

## 붙어있는 스크립트

| 클래스 | 위치 | 부착 대상 |
|---|---|---|
| `SubSceneController`  | `Unity/Common/SubSceneController.cs`      | GardenSceneController GO (returnScene=Mall) |
| `SceneTransitionInteraction` | `Unity/Mall/SceneTransitionInteraction.cs` | ExitToMall |
| `Farm`                | `Unity/Garden/Farm.cs`                   | 각 FarmTile prefab 인스턴스 |
| `CameraFollow`, `DynamicBoundaryWalls` | `Unity/Common/…`      | Main Camera |

## Farm 컴포넌트 (각 밭 타일)

[`Farm.cs`](../../../Assets/Scripts/Unity/Garden/Farm.cs)

### 인스펙터 필드
```csharp
public TextMeshProUGUI actionPrompt;      // 플레이어 근접 시 프롬프트
public SpriteRenderer cropSpriteRenderer; // 작물 표시
public float uiOffsetY = 0.5f;

// 성장 게이지 UI (crop 위)
public GaugeUI growthGauge;
public GameObject gaugeCanvas;

// 작물 이름 UI (crop 아래)
public GameObject cropNameCanvas;
public TextMeshProUGUI cropNameLabel;
public float nameOffsetY = 0.3f;

public int farmIndex;                     // 이 타일의 인덱스 (0..N)
```

### IsLocked

```csharp
public bool IsLocked => farmIndex >= FarmUpgrade.GetCurrentData("tile").value;
```
초기값 3 — 3번 인덱스부터 잠김. 업그레이드로 확장.

### Start() — 상태 복원 + 자동 심기
1. `GardenPersistent.tiles[farmIndex]`에 저장 데이터 있으면 `FarmTile.ApplySaveData(...)` 복원
2. **잠기지 않고 비어있으면** `CropCatalog.GetRandomCropByWeight()`로 자동 심기 (가중치 랜덤)
3. `Progress.OnPhaseChanged += OnTimePassed` — 페이즈 전환마다 UI 갱신

### Update() — 플레이어 상호작용
플레이어가 트리거 안에 있고 `Space` 눌렀을 때:
- **잠김**: 무동작 (프롬프트 "잠겨 있음")
- **수확 가능**: `FarmTile.Harvest(out id, out crops, harvestCount)` → `Inventory.AddHarvestedCrop(id, crops)` + 다음 crop 자동 심기
  - `harvestCount`는 `FarmUpgrade.harvestCount` (기본 5)
- **성장 중**: 프롬프트 "성장 중..."
- **빈 타일**: (원래는 심기 가능하지만 Start에서 자동 심기 처리)

### OnDestroy() — 저장
씬 나가기 전 `GardenPersistent.tiles[farmIndex] = tile.GetSaveData()`.

### 프롬프트 문구
| 조건 | 텍스트 |
|---|---|
| 잠김 | "잠겨 있음" |
| 수확 가능 | "(스페이스바로 수확)" |
| 빈 타일 | "(스페이스바로 심기)" |
| 성장 중 | "성장 중..." |

### UI 배치 (`AdjustUIPosition`)
- 성장 게이지: `cropSpriteRenderer.bounds.max.y + uiOffsetY`
- 작물 이름: `cropSpriteRenderer.bounds.min.y - nameOffsetY`

## FarmTile POCO (모델)

`FarmTile.cs` (Domain/Garden).
- `Plant(CropData)`, `IsEmpty()`, `IsHarvestable()`, `GetPassedPhases()`, `GetCurrentCrop()`
- `ApplySaveData(FarmTileSaveData)` / `GetSaveData()`
- 페이즈 진행에 따라 growPhaseCount만큼 자라면 수확 가능

## 종료 조건

- `ExitToMall` 트리거 → `SceneTransitionInteraction` → Mall 씬
  - `SceneLoader.SetMallReturnPosition`으로 정확한 X 복귀
- `SubSceneController.ReturnToIdle()`은 여기선 사용되지 않음 (Garden에서 phase 자동 종료 안 함)

## 관련 시스템

- [Mall 씬](./scene-mall.md) — 진입/복귀
- `Farming System` (POCO): `CropCatalog`, `FarmUpgrade`, `Inventory`
- `GardenPersistent.tiles[]` — 타일 상태 저장 (`gamedata.json` 일부)
- Csv: `cropData` (weight/growPhaseCount/sprite path 등), `farmUpgrade` (tile/harvestCount)
