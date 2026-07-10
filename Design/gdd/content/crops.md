# 작물 (Crops) 데이터

## 개요

Garden 씬의 밭에서 자라는 **작물 카탈로그**. 데이터 원본은 CSV 파일이며, ScriptableObject 자산은 존재하지 않는다.

- 데이터 원본: `Assets/Bundles/driveAssets/dataTables/crop_data.csv`
- 스프라이트 카탈로그: `Assets/Bundles/Catalogs/CropSpriteCatalog.asset` (`Game.Schema.Catalog.CropSpriteCatalogSO`)
- 데이터 POCO: [`CropData.cs`](../../../Assets/Scripts/Schema/Config/Common/CropData.cs)
- 카탈로그 서비스: [`CropCatalogService.cs`](../../../Assets/Scripts/Domain/Garden/CropCatalogService.cs)

> `Assets/Bundles/ScriptableObjects/CropData/` 폴더는 **비어 있음**. Crop 데이터는 SO가 아닌 CSV POCO로 관리된다.

## CropData 필드

```csharp
public class CropData : CsvParsable
{
    public string cropId;         // 산출 재료 FoodData ID (I###)
    public int    growPhaseCount; // 성장에 필요한 페이즈 수
    public float  spawnWeight;    // 랜덤 심기 시 가중치
    public string imagePath;      // 스프라이트 카탈로그 키
    public Sprite sprite;         // CropSpriteCatalog에서 lookup
}
```

- `cropId`는 [`../content/ingredients.md`](ingredients.md)의 재료 ID와 1:1 매칭 (수확 시 `Inventory.AddHarvestedCrop(cropId, harvestCount)`으로 재료로 변환)
- `growPhaseCount`는 페이즈 단위 (하루 5페이즈: Preparation, Morning, Afternoon, Evening, Night)
- `spawnWeight`는 총합 대비 상대 확률로 사용 (`GameRandom.WeightedPick`)

## 전체 작물 목록 (13종)

| # | cropId | 재료명 | growPhaseCount | 성장일 수(약) | spawnWeight | 상대 확률 | 스프라이트 키 |
|:-:|---|---|:-:|:-:|:-:|:-:|---|
| 1 | I004 | 마늘 | 7 | 1.4일 | 0.04 | 3.9% | item_garlic_raw |
| 2 | I005 | 가지 | 7 | 1.4일 | 0.04 | 3.9% | item_eggplant_raw |
| 3 | I012 | 절연 버섯 | 3 | 0.6일 | **0.20** | **19.5%** | item_insulatingMushroom_raw |
| 4 | I013 | 새벽풀 | 7 | 1.4일 | 0.04 | 3.9% | item_dawnHerb_raw |
| 5 | I017 | 스틸루트 | 7 | 1.4일 | 0.04 | 3.9% | item_steelRoot_raw |
| 6 | I018 | 대파 | 7 | 1.4일 | 0.04 | 3.9% | item_greenOnion_raw |
| 7 | I025 | 청양잎 | 3 | 0.6일 | **0.20** | **19.5%** | item_cheongyangLeaf_raw |
| 8 | I026 | 루미잎 | 5 | 1.0일 | 0.12 | 11.7% | item_lumiLeaf_raw |
| 9 | I027 | 빛 토마토 | 5 | 1.0일 | 0.12 | 11.7% | item_lightTomato_raw |
| 10 | I028 | 바람콩 | 7 | 1.4일 | 0.04 | 3.9% | item_windBean_raw |
| 11 | I029 | 전기꽃 | 7 | 1.4일 | 0.04 | 3.9% | item_electricFlower_raw |
| 12 | I065 | 달의 뿌리 | 7 | 1.4일 | 0.04 | 3.9% | item_moonRoot_raw |
| 13 | I066 | 양파 | 7 | 1.4일 | 0.04 | 3.9% | item_onion_raw |
| — | — | — | — | — | **합계 1.024** | 100% | — |

> 성장일 수는 참고용. 실제 판정은 `passed >= Mathf.CeilToInt(growPhaseCount × (1 - timeReduction))` (페이즈 카운트 기반). timeReduction 업그레이드는 상세 [`../systems/garden-system.md`](../systems/garden-system.md) 참조.

## 성장 시간 클래스별 분포

| 성장 페이즈 수 | 성장일 | 작물 종류 | 총 spawnWeight | 상대 확률 |
|:-:|:-:|:-:|:-:|:-:|
| 3 | 0.6일 | I012, I025 (2종) | 0.40 | **39.0%** |
| 5 | 1.0일 | I026, I027 (2종) | 0.24 | 23.4% |
| 7 | 1.4일 | 나머지 9종 | 0.36 | 35.2% |

→ **빠른 성장 작물(3페이즈)이 총 40% 확률로 자주 등장**하도록 편향. 짧은 페이즈 = 낮은 가치 재료.

## 산출 재료 매핑

각 작물은 수확 시 `cropId`를 그대로 재료 ID로 사용한다 (`Inventory.AddHarvestedCrop`). 재료 상세:

| cropId | 재료명 | 유통기한 (일) | Ingredient 카테고리 | Ingredient defaultPrice (상점) | 태그 |
|---|---|:-:|---|:-:|---|
| I004 | 마늘 | 5 | Refrigerator | 1,500G | Vegetables |
| I005 | 가지 | 5 | Refrigerator | 1,500G | Vegetables |
| I012 | 절연 버섯 | 2 | LowerShelf | 300G | Vegetables |
| I013 | 새벽풀 | 5 | Refrigerator | 1,500G | Vegetables |
| I017 | 스틸루트 | 7 | LowerShelf | 1,500G | Vegetables |
| I018 | 대파 | 5 | Refrigerator | 1,500G | Vegetables |
| I025 | 청양잎 | 5 | Refrigerator | 300G | Vegetables |
| I026 | 루미잎 | 5 | Refrigerator | 1,500G | Vegetables |
| I027 | 빛 토마토 | 3 | Refrigerator | 600G | Vegetables |
| I028 | 바람콩 | 5 | LowerShelf | 1,500G | Vegetables |
| I029 | 전기꽃 | 5 | LowerShelf | 1,500G | Vegetables |
| I065 | 달의 뿌리 | 5 | LowerShelf | 1,500G | Vegetables |
| I066 | 양파 | 5 | Refrigerator | 1,500G | Vegetables |

(원본: `Assets/Bundles/driveAssets/dataTables/ingredient.csv`, `food.csv`)

### 텃밭 vs 상점 라인업

FoodShopConfig(`Assets/Bundles/ScriptableObjects/Config/FoodShopConfig.asset`)와 대조:

| cropId | 상점 카테고리 |
|---|---|
| I004, I005, I018, I025, I066 | **General** (상점 무한 매입) |
| I012, I013, I017, I026, I027, I028, I029, I065 | **Special** (상점 페이즈별 랜덤 4/19) |
| I029, I065 | 텃밭에서만 확실히 얻을 수 있는 계열 (상점 라인업 뽑기 실패 시 대체 공급) |

## 랜덤 뽑기 (`GetRandomCropByWeight`)

파일: [`CropCatalogService.cs`](../../../Assets/Scripts/Domain/Garden/CropCatalogService.cs)

```csharp
public CropData GetRandomCropByWeight()
{
    if (_crops.Count == 0) return null;
    return GameRandom.WeightedPick(GameRandom.Immutable, _crops, c => c.spawnWeight);
}
```

- `GameRandom.Immutable` 시퀀스 — 시드 기반 결정적
- 호출 시점:
  - `Farm.Start` — 빈 타일 자동 심기
  - `Farm.Update` (수확 후) — 자동 재파종

## 스프라이트 카탈로그

파일: `Assets/Bundles/Catalogs/CropSpriteCatalog.asset` (`CropSpriteCatalogSO`)

`imagePath` 문자열 → `Sprite` 참조. 등록된 키 13개는 위 표의 `cropId`와 정확히 대응:

```
driveAssets/art/item/cooking/food/item_garlic_raw
driveAssets/art/item/cooking/food/item_eggplant_raw
driveAssets/art/item/cooking/food/item_insulatingMushroom_raw
driveAssets/art/item/cooking/food/item_dawnHerb_raw
driveAssets/art/item/cooking/food/item_steelRoot_raw
driveAssets/art/item/cooking/food/item_greenOnion_raw
driveAssets/art/item/cooking/food/item_cheongyangLeaf_raw
driveAssets/art/item/cooking/food/item_lumiLeaf_raw
driveAssets/art/item/cooking/food/item_lightTomato_raw
driveAssets/art/item/cooking/food/item_windBean_raw
driveAssets/art/item/cooking/food/item_electricFlower_raw
driveAssets/art/item/cooking/food/item_moonRoot_raw
driveAssets/art/item/cooking/food/item_onion_raw
```

CSV 로드 후 `CropCatalogService`에서 각 `CropData`의 `sprite` 필드에 매핑.

## 관련 문서

- Garden 씬 흐름 / 성장 판정: [`../systems/garden-system.md`](../systems/garden-system.md)
- 인벤토리 (수확된 재료 취급): [`../systems/save-system.md`](../systems/save-system.md) (있다면)
- 상점 재료 라인업: [`../systems/shop-system.md`](../systems/shop-system.md)
- 재료 정보: [`ingredients.md`](ingredients.md) (있다면)
