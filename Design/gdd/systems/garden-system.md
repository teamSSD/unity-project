# Garden 시스템

## 개요

**Garden (텃밭) 씬**은 재료를 자급자족하기 위한 씬. Mall 씬의 `FarmPathInteraction`을 통해 진입한다. 여러 개의 밭 타일에서 페이즈 단위로 성장하는 작물을 심고 수확한다.

씬 파일: `Assets/Scenes/ForReal/Garden.unity`
핵심 스크립트: [`Farm.cs`](../../../Assets/Scripts/Unity/Garden/Farm.cs), [`FarmTile.cs`](../../../Assets/Scripts/Unity/Garden/FarmTile.cs)
카탈로그 서비스: [`CropCatalogService.cs`](../../../Assets/Scripts/Domain/Garden/CropCatalogService.cs)
업그레이드 서비스: [`FarmUpgradeService.cs`](../../../Assets/Scripts/Domain/Garden/FarmUpgradeService.cs)

## 씬 구조

Garden 씬은 **7개의 FarmTile 프리팹 인스턴스**로 구성. 각 인스턴스는 `farmIndex` 필드로 0~6번 슬롯 배정.

`Assets/Scenes/ForReal/Garden.unity`의 프리팹 override에서 확인된 farmIndex 값: **0(prefab 기본), 1, 2, 3, 4, 5, 6** — 총 7개.

- `GardenPersistent.TileCount` = 8 (하드코딩된 배열 크기, 예비 슬롯 1개 여유)
- 실제 씬 배치는 7개이며, 이는 `tile` 업그레이드 max 레벨(level 4 = 7개)과 일치
- 카메라: `Main Camera` + `CameraFollow` (Mall과 동일 패턴, 배경 경계 clamp)
- `ExitToMall` 오브젝트 → `SceneTransitionInteraction`으로 Mall 복귀

## FarmTile / Farm 컴포넌트 페어

밭 하나는 `Farm` MonoBehaviour + `FarmTile` POCO 두 계층으로 나뉜다.

| 계층 | 역할 | 파일 |
|---|---|---|
| **Farm** (Unity) | 씬 배치, 스프라이트 렌더링, 스페이스바 입력, UI 라벨 | [`Farm.cs`](../../../Assets/Scripts/Unity/Garden/Farm.cs) |
| **FarmTile** (POCO) | 심기/수확 로직, 성장 페이즈 계산, 세이브 데이터 변환 | [`FarmTile.cs`](../../../Assets/Scripts/Unity/Garden/FarmTile.cs) |

### Farm 컴포넌트 필드
- `farmIndex` (0~7) — 슬롯 인덱스
- `cropSpriteRenderer` — 작물 스프라이트
- `growthGauge` (GaugeUI) + `gaugeCanvas` — 성장 진행 UI
- `cropNameCanvas` + `cropNameLabel` — 작물 이름 라벨 (crop sprite 하단 표시)
- `IsLocked` — `farmIndex >= FarmUpgrade.GetCurrentData("tile").value` (해금 안 된 슬롯)

## 심기 / 수확 / 자동 재파종 흐름

### `Farm.Start`
1. `Progress` 서비스 획득 → `TimePhaseProvider` 주입
2. `GardenPersistent.tiles[farmIndex]`에 세이브 데이터 있으면 `FarmTile.ApplySaveData` — 저장 상태 복원
3. **빈 타일이고 잠기지 않았으면 자동 심기**:
   ```
   if (!IsLocked && tile.IsEmpty())
       cropData = CropCatalog.GetRandomCropByWeight();
       tile.Plant(cropData);
   ```
4. `Progress.OnPhaseChanged += OnPhaseChangedHandler` — 페이즈 tick 구독

### 심기 (`FarmTile.Plant(data)`)
- `crop = data`
- `plantedPhase = phaseProvider.CurrentPhaseIndex` (`ProgressService._cumulativePhaseIndex`)
- 각 페이즈가 지날 때마다 `CurrentPhaseIndex`가 1씩 증가 → 성장 판정 기준

### 성장 판정 (`FarmTile.IsHarvestable`)
```csharp
int passed = phaseProvider.CurrentPhaseIndex - plantedPhase;
float timeReduction = FarmUpgrade.GetCurrentData("timeReduction")?.value ?? 0f;
int requiredPhases = Mathf.CeilToInt(crop.growPhaseCount * (1f - timeReduction));
return passed >= requiredPhases;
```

- **성장 단위 = 페이즈 (일수 아님)**. 하루 5페이즈(Preparation/Morning/Afternoon/Evening/Night)이므로 `growPhaseCount=5`인 작물은 1일 만에 수확 가능
- `timeReduction` 업그레이드(0/16/24/32/40%)로 요구 페이즈 감소
- `CeilToInt` — 소수점 올림 (예: 7 × (1-0.16) = 5.88 → 6 페이즈)

### 수확 (`FarmTile.Harvest`)
- Space 입력 → `IsHarvestable` 통과 → `Inventory.AddHarvestedCrop(cropId, harvestCount)`
- `harvestCount`는 `FarmUpgrade.GetCurrentData("harvestCount").value` (5/7/9/11/13개)
- 수확 후 `crop = null` → **자동 재파종**: `CropCatalog.GetRandomCropByWeight()` 다시 뽑아 심음

### 잠긴 타일 (`IsLocked`)
- `farmIndex >= tile.value` (예: level 0에서 `farmIndex >= 3`이면 잠김)
- Space 입력해도 아무 동작 안 함
- `actionPrompt`에 "잠겨 있음" 표시

### 상호작용 프롬프트 (`Farm.UpdatePrompt`)
| 상태 | 프롬프트 |
|---|---|
| 잠긴 타일 | "잠겨 있음" |
| 수확 가능 | "(스페이스바로 수확)" |
| 빈 타일 (해금됨) | "(스페이스바로 심기)" — 실제로는 자동 심기되므로 진입 순간에만 잠깐 표시 |
| 성장 중 | "성장 중..." |

## Farm.OnPhaseChangedHandler

```csharp
progress.OnPhaseChanged += OnPhaseChangedHandler;
private void OnPhaseChangedHandler(PhaseType _) => OnTimePassed();
```

`OnTimePassed`는 `UpdateVisuals` + `UpdatePrompt` 호출 — 씬을 나가지 않아도 페이즈 전환마다 성장 게이지가 즉시 갱신.

- Garden 씬에 없어도 (Mall/Cooking/Shop에 있어도) `CurrentPhaseIndex`가 증가하므로 성장은 백그라운드에서 계속됨
- 씬 재진입 시 저장된 `plantedPhase` 대비 현재 인덱스 차이로 정확히 계산 → 게임 진행에 따라 자연스러운 성장

## 저장 / 로드

### 저장 (`Farm.OnDestroy`)
```csharp
gp.tiles[farmIndex] = tile.GetSaveData();
// FarmTileSaveData { cropId, plantedPhase }
```

Garden 씬 이탈 시 자동 저장. 이후 `SaveManager.SaveAll()` (PassDay)에서 디스크로 flush.

### 로드 (`Farm.Start`)
- `GardenPersistent.tiles[farmIndex]`에서 `FarmTileSaveData` 읽음
- `cropId`로 `CropCatalog.GetCropById(cropId)` 조회 → `crop` 복원
- `plantedPhase` 복원 → 이후 `IsHarvestable` 판정에 그대로 사용

## 텃밭 업그레이드 (레벨별 사이즈)

파일: [`FarmUpgradeService.cs`](../../../Assets/Scripts/Domain/Garden/FarmUpgradeService.cs), 데이터: `upgrade_farm.csv`

| type | 효과 | level 0 | 1 | 2 | 3 | 4 |
|---|---|---|---|---|---|---|
| **tile** | 잠금 해제된 타일 수 | 3 | 4 | 5 | 6 | 7 |
| **timeReduction** | 성장 페이즈 감소율 | 0% | 16% | 24% | 32% | 40% |
| **harvestCount** | 1회 수확 재료 수 | 5 | 7 | 9 | 11 | 13 |
| **코스트** | | — | 3,000G | 7,000G | 12,000G | 20,000G |

- 총 업그레이드 비용 (per type): 42,000G
- 3종 합계: 126,000G

## 텃밭이 경제에 미치는 영향

### 재료 자급자족
- 수확된 작물은 `Inventory.AddHarvestedCrop(cropId, harvestCount)`으로 인벤토리에 직접 추가 (구매 없이)
- 유통기한은 `Ingredient.expirationDay` 그대로 (수확일 = 시작일)
- 인벤토리 슬롯 상한 체크 없이 `AddFood` 호출 → **작물은 창고 용량을 초과해서 들어올 수 있음** (구매와 달리)

### 재료 종류
- 텃밭에서 나오는 작물은 13종 (I004, I005, I012, I013, I017, I018, I025, I026, I027, I028, I029, I065, I066)
- 이 중 상점 General(I004, I005, I018, I025, I066)과 겹치는 5종은 텃밭이 무료 대체재
- 나머지 8종은 상점에서 Special(랜덤)로만 뽑히므로 **텃밭이 안정적 공급원**

### 밸런스 함의
- 밭 3개 (level 0) × 5개 수확 × 하루 1회 수확(성장 5페이즈=1일 기준) = 하루 최대 15개
- 밭 7개 (level 4 max) × 13개 × (1 - 40%)단축 = 페이즈당 최대 91개 규모 (짧은 성장 3페이즈 작물 기준)
- 상점 매입비 대비 무료 → 후반부 재료 병목 완화

## 작물 카탈로그 (CropCatalogService)

- CSV `crop_data.csv`에서 `CropData` POCO 로드 (`Assets/Bundles/ScriptableObjects/CropData/`는 비어있음 — 카탈로그는 CSV 기반)
- 자산 `Assets/Bundles/Catalogs/CropSpriteCatalog.asset`이 `imagePath`를 스프라이트로 매핑
- `GetRandomCropByWeight()` — `GameRandom.WeightedPick(Immutable, crops, c => c.spawnWeight)`
  - 즉 시드 기반 결정적 랜덤 (같은 게임 상태면 같은 작물)

작물 목록/상세는 [`../content/crops.md`](../content/crops.md) 참조.

## 관련 문서

- 페이즈 인덱스 계산: [`../overview/core-loop.md`](../overview/core-loop.md)
- 인벤토리 / 유통기한: [`save-system.md`](save-system.md)
- 농장 업그레이드 UI: [`shop-system.md`](shop-system.md)
- 작물 데이터: [`../content/crops.md`](../content/crops.md)
