# FoodData 전수 목록

`Assets/Bundles/ScriptableObjects/FoodData/*.asset` — 총 **66개**의 FoodData ScriptableObject.

CSV 원천: `Assets/Bundles/driveAssets/dataTables/food.csv` (66행)
정의: `Assets/Scripts/Schema/Config/Cooking/FoodData.cs`

관련 문서: [ingredients.md](./ingredients.md), [recipes.md](./recipes.md), [../systems/cooking-tools.md](../systems/cooking-tools.md), [../systems/cooking-system.md](../systems/cooking-system.md)

---

## 1. FoodData 스키마

```csharp
public string id;                    // I000 ~ I070 (일부 결번)
public string ingredientName;        // 한글 표시명
public string description;           // 인게임 툴팁/설명 (INGREDIENT만 채워짐, 중간재/완성품은 대개 비어있음)
public Sprite image;                 // 대표 sprite (raw)
public IngredientData ingredient;    // INGREDIENT type일 때만 non-null. IngredientData.asset 참조.
public List<string> availableTools;  // "T001", "T002"... 드롭 가능한 도구 목록
public FoodType type;                // GARBAGE=0, INGREDIENT=1, PROCESSING=2, MAIN=3, SIDE=4
public Sprite[] toolVariants;        // [5] pan/pot/bowl/cut/plate 도구별 표시 sprite
public Sprite[] bentoVariants;       // [4] 도시락 [0]main / [1]left / [2]middle / [3]right
public Sprite pieceSprite;           // 도마(T004) 절단 시 조각 sprite (일부 INGREDIENT만)
```

### 1.1 FoodType 정수 매핑

| Enum | int | 의미 |
|---:|---:|---|
| GARBAGE | 0 | 실패 시 반환되는 음식물쓰레기 (I000 전용) |
| INGREDIENT | 1 | 원재료 (인벤토리·상점 구매 대상, IngredientData 짝) |
| PROCESSING | 2 | 중간 조리물 (레시피 인풋용, 도시락에 못 담음) |
| MAIN | 3 | 완성 메인 요리 (도시락 첫 슬롯) |
| SIDE | 4 | 완성 사이드 요리 (도시락 2~4 슬롯) |

### 1.2 toolVariants / bentoVariants 인덱스

`FoodData.cs` 상단 상수:

- `toolSuffixes = ["_pan","_pot","_bowl","_cut","_plate"]` → `toolVariants[0]=T001 팬, [1]=T002 냄비, [2]=T003 보울, [3]=T004 도마, [4]=T005 철판`.
- `bentoSuffixes = ["_bento","_bento_left","_bento_middle","_bento_right"]` → `bentoVariants[0]=main, [1]=left, [2]=middle, [3]=right`.

`GetImageForTool(toolId)` — 도구 슬롯 안에서의 표시 스프라이트. `GetMainBentoImage()` — 도시락 메인 슬롯. `GetSideBentoImage(sideOrder)` — sideOrder(0/1/2)에 따라 `bentoVariants[1+sideOrder]`.

### 1.3 availableTools 규칙

- INGREDIENT / PROCESSING: 실제 사용 도구 id 리스트 (예: `T004/T005`).
- GARBAGE / MAIN / SIDE: 빈 리스트 (도구에 진입 불가). BentoModel만 받을 수 있음.

---

## 2. 전체 목록 (66개)

- **image**: 대표 sprite 존재 여부 (Y/N — 일부 PROCESSING은 미확인)
- **toolVariants**: 채워진 슬롯 수 (5 중)
- **bentoVariants**: 채워진 슬롯 수 (4 중)
- **pieceSprite**: 절단 조각 스프라이트 존재
- **price / exp / display**: INGREDIENT type만 IngredientData에서 채워짐. 나머지는 `-`.

| ID | 이름 | type | availableTools | price | exp | display | image | toolVariants | bentoVariants | pieceSprite |
|---|---|---|---|---:|---:|---|:---:|:---:|:---:|:---:|
| I000 | 음식물쓰레기 | GARBAGE | - | - | - | - | Y | 5 | 0 | N |
| I001 | 흑미 | INGREDIENT | T002 | 1500 | 5 | LowerShelf | Y | 1 | 0 | N |
| I002 | 밀면 | INGREDIENT | T002 | 1500 | 5 | LowerShelf | Y | 1 | 0 | N |
| I003 | 두부 | INGREDIENT | T002 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I004 | 마늘 | INGREDIENT | T001 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I005 | 가지 | INGREDIENT | T003 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I007 | 고추장 | INGREDIENT | T003 | 1500 | 7 | UpperShelf | Y | 1 | 0 | N |
| I008 | 레몬 | INGREDIENT | T003 | 600 | 5 | Refrigerator | Y | 1 | 0 | N |
| I009 | 인공고기 | INGREDIENT | T004/T005 | 600 | 3 | Refrigerator | Y | 2 | 0 | Y |
| I010 | 루미 계란 | INGREDIENT | T001/T002 | 600 | 3 | Refrigerator | Y | 2 | 0 | N |
| I012 | 절연 버섯 | INGREDIENT | T002/T003/T004 | 300 | 2 | LowerShelf | Y | 3 | 0 | Y |
| I013 | 새벽풀 | INGREDIENT | T002 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I014 | 청유 | INGREDIENT | T001 | 1500 | 7 | UpperShelf | Y | 1 | 0 | N |
| I015 | 오징어먹물 | INGREDIENT | T003 | 1500 | 7 | LowerShelf | Y | 1 | 0 | N |
| I016 | 건조 새우 | INGREDIENT | T001 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I017 | 스틸루트 | INGREDIENT | T001 | 1500 | 7 | LowerShelf | Y | 1 | 0 | N |
| I018 | 대파 | INGREDIENT | T003 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I019 | 검은 된장 | INGREDIENT | T003 | 1500 | 7 | UpperShelf | Y | 1 | 0 | N |
| I020 | 조명 시럽 | INGREDIENT | T003 | 1500 | 7 | UpperShelf | Y | 1 | 0 | N |
| I021 | 황동소금 | INGREDIENT | T001/T005 | 1500 | 7 | UpperShelf | Y | 2 | 0 | N |
| I022 | 연기잎 버터 | INGREDIENT | T003/T005 | 600 | 3 | UpperShelf | Y | 2 | 0 | N |
| I023 | 건조멸치 | INGREDIENT | T001/T003 | 1500 | 7 | LowerShelf | Y | 1 | 0 | N |
| I024 | 검은깨 | INGREDIENT | T001/T003 | 1500 | 7 | UpperShelf | Y | 1 | 0 | N |
| I025 | 청양잎 | INGREDIENT | T001/T003 | 300 | 5 | Refrigerator | Y | 0 | 0 | N |
| I026 | 루미잎 | INGREDIENT | T001/T003 | 1500 | 5 | Refrigerator | Y | 2 | 0 | N |
| I027 | 빛 토마토 | INGREDIENT | T001/T003 | 600 | 3 | Refrigerator | Y | 2 | 0 | N |
| I028 | 바람콩 | INGREDIENT | T001 | 1500 | 5 | LowerShelf | Y | 1 | 0 | N |
| I029 | 전기꽃 | INGREDIENT | T003 | 1500 | 5 | LowerShelf | Y | 1 | 0 | N |
| I030 | 합성 닭고기 | INGREDIENT | T003 | 1500 | 7 | Refrigerator | Y | 1 | 0 | N |
| I031 | 인공 연어 | INGREDIENT | T003 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I032 | 해초 육수 | PROCESSING | T002 | - | - | - | Y | 1 | 0 | N |
| I033 | 새벽국 베이스 | PROCESSING | T002 | - | - | - | Y | 1 | 0 | N |
| I034 | 새벽국 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I035 | 삶은 밀면 | PROCESSING | T002/T001 | - | - | - | N | 2 | 0 | N |
| I036 | 다진 절연 버섯 | PROCESSING | T004/T003 | - | - | - | Y | 2 | 0 | N |
| I037 | 다크소이 | PROCESSING | T003/T001 | - | - | - | Y | 2 | 0 | N |
| I038 | 새우기름 베이스 | PROCESSING | T001 | - | - | - | Y | 1 | 0 | N |
| I039 | 구룡면 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I040 | 깍둑 썬 인공고기 | PROCESSING | T004/T001 | - | - | - | Y | 2 | 0 | N |
| I042 | 구운 스틸루트 | PROCESSING | T001 | - | - | - | N | 1 | 0 | N |
| I043 | 장소스 | PROCESSING | T003/T001 | - | - | - | Y | 2 | 0 | N |
| I044 | 기계장 고기정식 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I045 | 루미 젤 베이스 | PROCESSING | T002/T003 | - | - | - | Y | 2 | 0 | N |
| I046 | 루미 젤리 | SIDE | - | - | - | - | Y | 1 | 3 | N |
| I047 | 인공고기 스테이크 | PROCESSING | T005 | - | - | - | Y | 1 | 0 | N |
| I048 | 캐러멜 토핑 | PROCESSING | T001/T005 | - | - | - | Y | 1 | 0 | N |
| I049 | 스트리트 스테이크 49 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I050 | 흑미밥 | PROCESSING | T002/T003 | - | - | - | Y | 2 | 0 | N |
| I051 | 삼각형 밥 | PROCESSING | T003 | - | - | - | Y | 1 | 0 | N |
| I052 | 간장 코팅 밥 | PROCESSING | T003/T005 | - | - | - | Y | 2 | 0 | N |
| I053 | 폐건물 삼각밥 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I054 | 구운바람콩 | PROCESSING | T001/T003 | - | - | - | Y | 2 | 0 | N |
| I055 | 샐러드 베이스 | PROCESSING | T003 | - | - | - | Y | 1 | 0 | N |
| I056 | 네온 샐러드 | SIDE | - | - | - | - | Y | 1 | 3 | N |
| I057 | 꼬치 조합 | PROCESSING | T003/T001 | - | - | - | Y | 2 | 0 | N |
| I058 | 전력실 꼬치 | SIDE | - | - | - | - | Y | 1 | 3 | N |
| I059 | 오믈렛 베이스 | PROCESSING | T001 | - | - | - | Y | 1 | 0 | N |
| I060 | 옥상 오믈렛 | MAIN | - | - | - | - | Y | 1 | 1 | N |
| I061 | 양념 연어 | PROCESSING | T001 | - | - | - | Y | 2 | 0 | N |
| I062 | 환기구 연어구이 | SIDE | - | - | - | - | Y | 1 | 3 | N |
| I065 | 달의 뿌리 | INGREDIENT | T001 | 1500 | 5 | LowerShelf | Y | 1 | 0 | N |
| I066 | 양파 | INGREDIENT | T001 | 1500 | 5 | Refrigerator | Y | 1 | 0 | N |
| I067 | 잿빛 간장 | INGREDIENT | T003 | 1500 | 7 | UpperShelf | Y | 0 | 0 | N |
| I068 | 신문지 | INGREDIENT | T001/T005 | 600 | 5 | LowerShelf | Y | 2 | 0 | N |
| I069 | 물 | INGREDIENT | T002 | 600 | 5 | Refrigerator | Y | 1 | 0 | N |
| I070 | 절인 해초 | INGREDIENT | T002 | 1500 | 3 | Refrigerator | Y | 1 | 0 | N |

**결번**: I006, I011, I041, I063, I064 — 존재하지 않음 (총 66개 유효).

---

## 3. 타입별 요약

### 3.1 GARBAGE (1개)

| ID | 이름 | 용도 |
|---|---|---|
| I000 | 음식물쓰레기 | 잘못된 재료 조합 시 반환. R000이 outputFood=I000, inputs=[] |

### 3.2 INGREDIENT (30개)

원재료 — IngredientData와 1:1 매핑, 인벤토리에 저장되며 상점에서 구매 가능. 자세한 IngredientData 필드는 [ingredients.md](./ingredients.md) 참조.

I001~I005, I007~I010, I012~I031, I065~I070 (35 IngredientData 중 I032~I062 사이 결번 감안 30개).

> **참고**: FoodData INGREDIENT 30개 vs IngredientData 35개. 차이는 FoodData쪽 결번(I006/I011) + IngredientData 중 아직 FoodData가 없는 항목이 아니라 반대로 FoodData가 다중 사용되는 재활용성 때문. 실제 catalog에 로드되는 재료는 [ingredients.md](./ingredients.md) 표 기준.

### 3.3 PROCESSING (23개)

중간 조리물 (도시락 못 담음, 다음 도구로만 이송).

I032 해초 육수, I033 새벽국 베이스, I035 삶은 밀면, I036 다진 절연 버섯, I037 다크소이, I038 새우기름 베이스, I040 깍둑 썬 인공고기, I042 구운 스틸루트, I043 장소스, I045 루미 젤 베이스, I047 인공고기 스테이크, I048 캐러멜 토핑, I050 흑미밥, I051 삼각형 밥, I052 간장 코팅 밥, I054 구운바람콩, I055 샐러드 베이스, I057 꼬치 조합, I059 오믈렛 베이스, I061 양념 연어.

### 3.4 MAIN (6개)

도시락 메인 (첫 슬롯).

| ID | 이름 | 완성 레시피 |
|---|---|---|
| I034 | 새벽국 | R004 |
| I039 | 구룡면 | R009 |
| I044 | 기계장 고기정식 | R014 |
| I049 | 스트리트 스테이크 49 | R019 |
| I053 | 폐건물 삼각밥 | R023 |
| I060 | 옥상 오믈렛 | R030 |

### 3.5 SIDE (4개)

도시락 사이드 (2~4번 슬롯).

| ID | 이름 | 완성 레시피 |
|---|---|---|
| I046 | 루미 젤리 | R016 |
| I056 | 네온 샐러드 | R026 |
| I058 | 전력실 꼬치 | R028 |
| I062 | 환기구 연어구이 | R032 |

---

## 4. Sprite 변형 특이사항

### 4.1 pieceSprite 보유

도마(T004)로 절단 시 조각 sprite가 표시되는 재료.

| ID | 이름 |
|---|---|
| I009 | 인공고기 |
| I012 | 절연 버섯 |

파싱 규칙: INGREDIENT + `availableTools` 포함 T004 조건. 해당하는 재료는 `_piece` 접미사 sprite를 로드 (`FoodData.LoadPieceSprite`).

### 4.2 image 미확인

| ID | 이름 | 비고 |
|---|---|---|
| I035 | 삶은 밀면 | image: fileID=0 (asset 값 미할당) |
| I042 | 구운 스틸루트 | image: fileID=0 |

이 재료들은 대표 sprite 없이 `toolVariants`만으로 표시되도록 설계된 것으로 보임 — `GetImageForTool` fallback이 image라 실제 게임에선 문제 발생 여지 (기획 확인 필요).

### 4.3 toolVariants가 0인 재료

| ID | 이름 | availableTools | 비고 |
|---|---|---|---|
| I025 | 청양잎 | T001/T003 | variant 미할당 — `GetImageForTool` fallback으로 원본 image 표시 |
| I067 | 잿빛 간장 | T003 | variant 미할당 — 동일 |

---

## 5. 관련 파일

- `Assets/Bundles/ScriptableObjects/FoodData/I{XXX}.asset` — 66개
- `Assets/Bundles/driveAssets/dataTables/food.csv` — CSV 원천
- `Assets/Scripts/Schema/Config/Cooking/FoodData.cs` — 클래스 정의
- `Assets/Scripts/Schema/Catalog/FoodCatalogSO.cs` — 런타임 카탈로그
