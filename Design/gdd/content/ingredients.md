# IngredientData 전수 목록

`Assets/Bundles/ScriptableObjects/IngredientData/*.asset` — 총 **35개**의 IngredientData ScriptableObject.

CSV 원천: `Assets/Bundles/driveAssets/dataTables/ingredient.csv` (35행)
정의: `Assets/Scripts/Schema/Config/Cooking/IngredientData.cs`

관련 문서: [foods.md](./foods.md), [recipes.md](./recipes.md)

---

## 1. IngredientData 스키마

```csharp
public string id;                        // I001 ~ I070 (FoodData와 공유 id)
public string description;               // 텍스트 설명 (FoodData.description과 대체로 동일)
public IngredientDisplayCategory display;// NONE=0 / Refrigerator=1 / UpperShelf=2 / LowerShelf=3
public int defaultPrice;                 // 상점 기본 판매가 (재고 재료의 원가)
public IngredientTag tag;                // Grains=0 / Vegetables=1 / Sauces=2 / Meat=3 / Liquid=4 / Seafood=5 / Dairy=6 / Other=7
public int expirationDay;                // 유통기한 (일)
```

### 1.1 IngredientDisplayCategory (저장소 배치)

| 값 | 저장소 | 씬 위치 |
|---|---|---|
| Refrigerator | 냉장고 | 그리드 3열 |
| UpperShelf | 상단 선반 | 수평 선형 |
| LowerShelf | 하단 선반 | 수평 선형 |
| NONE | 없음 | 어디에도 배치 안됨 |

카테고리별 표시 규칙과 용량은 [../systems/cooking-system.md §1.2](../systems/cooking-system.md#12-저장소-3종-기본-용량) 참조.

### 1.2 IngredientTag

Enum 값: `Grains, Vegetables, Sauces, Meat, Liquid, Seafood, Dairy, Other`. 현재 로직상 tag는 IngredientData의 분류 라벨(상점 필터/정렬 등)로 사용되며 게임플레이에는 직접 영향 없음.

---

## 2. 전체 목록 (35개)

| ID | 이름 | tag | display | price | expirationDay |
|---|---|---|---|---:|---:|
| I001 | 흑미 | Grains | LowerShelf | 1500 | 5 |
| I002 | 밀면 | Grains | LowerShelf | 1500 | 5 |
| I003 | 두부 | Grains | Refrigerator | 1500 | 5 |
| I004 | 마늘 | Vegetables | Refrigerator | 1500 | 5 |
| I005 | 가지 | Vegetables | Refrigerator | 1500 | 5 |
| I007 | 고추장 | Sauces | UpperShelf | 1500 | 7 |
| I008 | 레몬 | Sauces | Refrigerator | 600 | 5 |
| I009 | 인공고기 | Meat | Refrigerator | 600 | 3 |
| I010 | 루미 계란 | Meat | Refrigerator | 600 | 3 |
| I012 | 절연 버섯 | Vegetables | LowerShelf | 300 | 2 |
| I013 | 새벽풀 | Vegetables | Refrigerator | 1500 | 5 |
| I014 | 청유 | Sauces | UpperShelf | 1500 | 7 |
| I015 | 오징어먹물 | Meat | LowerShelf | 1500 | 7 |
| I016 | 건조 새우 | Meat | Refrigerator | 1500 | 5 |
| I017 | 스틸루트 | Vegetables | LowerShelf | 1500 | 7 |
| I018 | 대파 | Vegetables | Refrigerator | 1500 | 5 |
| I019 | 검은 된장 | Sauces | UpperShelf | 1500 | 7 |
| I020 | 조명 시럽 | Sauces | UpperShelf | 1500 | 7 |
| I021 | 황동소금 | Sauces | UpperShelf | 1500 | 7 |
| I022 | 연기잎 버터 | Dairy | UpperShelf | 600 | 3 |
| I023 | 건조멸치 | Meat | LowerShelf | 1500 | 7 |
| I024 | 검은깨 | Sauces | UpperShelf | 1500 | 7 |
| I025 | 청양잎 | Vegetables | Refrigerator | 300 | 5 |
| I026 | 루미잎 | Vegetables | Refrigerator | 1500 | 5 |
| I027 | 빛 토마토 | Vegetables | Refrigerator | 600 | 3 |
| I028 | 바람콩 | Vegetables | LowerShelf | 1500 | 5 |
| I029 | 전기꽃 | Vegetables | LowerShelf | 1500 | 5 |
| I030 | 합성 닭고기 | Meat | Refrigerator | 1500 | 7 |
| I031 | 인공 연어 | Meat | Refrigerator | 1500 | 5 |
| I065 | 달의 뿌리 | Vegetables | LowerShelf | 1500 | 5 |
| I066 | 양파 | Vegetables | Refrigerator | 1500 | 5 |
| I067 | 잿빛 간장 | Sauces | UpperShelf | 1500 | 7 |
| I068 | 신문지 | Other | LowerShelf | 600 | 5 |
| I069 | 물 | Liquid | Refrigerator | 600 | 5 |
| I070 | 절인 해초 | Seafood | Refrigerator | 1500 | 3 |

**결번**: I006, I011, I032~I062, I063, I064 — 완성품/중간재는 IngredientData가 없음 (FoodData만 존재). 총 35개 유효.

---

## 3. 저장소별 분포

### 3.1 Refrigerator (냉장고) — 15종
I003 두부, I004 마늘, I005 가지, I008 레몬, I009 인공고기, I010 루미 계란, I013 새벽풀, I018 대파, I025 청양잎, I026 루미잎, I027 빛 토마토, I030 합성 닭고기, I031 인공 연어, I066 양파, I069 물, I070 절인 해초

### 3.2 UpperShelf (상단 선반) — 9종
I007 고추장, I014 청유, I019 검은 된장, I020 조명 시럽, I021 황동소금, I022 연기잎 버터, I024 검은깨, I067 잿빛 간장

### 3.3 LowerShelf (하단 선반) — 11종
I001 흑미, I002 밀면, I012 절연 버섯, I015 오징어먹물, I017 스틸루트, I023 건조멸치, I028 바람콩, I029 전기꽃, I065 달의 뿌리, I068 신문지

> 세 저장소의 기본 용량 총합은 7 + 3 + 4 = 14개. 재료 종류가 그보다 많으므로 인벤토리 재고에 따라 매 페이즈 시작 시 어떤 재료가 실제 씬에 배치될지 결정됨 (`CookingSceneManager.FillStorage`).

---

## 4. 가격 · 유통기한 분포

### 4.1 가격대별

| 가격 | 재료 |
|---|---|
| 300 | I012 절연 버섯, I025 청양잎 |
| 600 | I008 레몬, I009 인공고기, I010 루미 계란, I022 연기잎 버터, I027 빛 토마토, I068 신문지, I069 물 |
| 1500 | 나머지 26종 |

### 4.2 유통기한별

| 일 | 재료 |
|---|---|
| 2 | I012 절연 버섯 |
| 3 | I009 인공고기, I010 루미 계란, I022 연기잎 버터, I027 빛 토마토, I070 절인 해초 |
| 5 | 대부분 (18종) |
| 7 | I007 고추장, I014 청유, I015 오징어먹물, I017 스틸루트, I019 검은 된장, I020 조명 시럽, I021 황동소금, I023 건조멸치, I024 검은깨, I030 합성 닭고기, I067 잿빛 간장 |

---

## 5. 태그별 분포

| Tag | 개수 | 재료 |
|---|---:|---|
| Grains | 3 | I001 흑미, I002 밀면, I003 두부 |
| Vegetables | 13 | I004, I005, I012, I013, I017, I018, I025, I026, I027, I028, I029, I065, I066 |
| Sauces | 8 | I007, I008, I014, I019, I020, I021, I024, I067 |
| Meat | 6 | I009, I010, I015, I016, I023, I030, I031 |
| Liquid | 1 | I069 물 |
| Seafood | 1 | I070 절인 해초 |
| Dairy | 1 | I022 연기잎 버터 |
| Other | 1 | I068 신문지 |

> 태그 분류가 완벽히 매핑되진 않음: I015 오징어먹물이 Meat, I024 검은깨가 Sauces 등 편의적 배치가 섞여 있음.

---

## 6. 관련 파일

- `Assets/Bundles/ScriptableObjects/IngredientData/I{XXX}.asset` — 35개
- `Assets/Bundles/driveAssets/dataTables/ingredient.csv` — CSV 원천
- `Assets/Scripts/Schema/Config/Cooking/IngredientData.cs` — 클래스 정의
- `Assets/Scripts/Schema/Catalog/IngredientCatalogSO.cs` — 런타임 카탈로그
