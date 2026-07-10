# 조리 도구 (Cooking Tools)

Cooking 씬에서 사용하는 5개 조리 도구의 데이터, 미니게임 매핑, 업그레이드 스펙.

관련 문서: [cooking-system.md](./cooking-system.md), [../content/foods.md](../content/foods.md), [../content/recipes.md](../content/recipes.md)

---

## 1. 도구 5종 개요

`Assets/Bundles/ScriptableObjects/CookingTools/*.asset` — 총 5개.

정의: `Assets/Scripts/Schema/Config/Common/CookingToolData.cs`

| ID | Asset Filename | cookerName | 기본 미니게임 (폴백) | 설명 |
|---|---|---|---|---|
| T001 | Pan.asset | 팬 | FireMiniGame (M001 Bake) | 굽기·볶기 계열 |
| T002 | Pot.asset | 냄비 | FireMiniGame (M002 Boil) | 끓이기·삶기 |
| T003 | Bowl.asset | 보울 | SauceMiniGame (M005) | 섞기·소스 (레시피 explicit로 M004 Mix에도 사용) |
| T004 | CuttingBoard.asset | 도마 | SliceMiniGame (M006) | 자르기 |
| T005 | griddle.asset | 철판 | GriddleMinigame (M007) | 방향키 콤보 그릴 |

> **주의**: 도구 → 미니게임 폴백은 `RecipeData.minigameId`가 비어 있거나 지정되지 않은 경우에만 발동. 대부분의 레시피는 `minigameId`를 명시적으로 지정한다. 예를 들어 R007(다크소이, T003 보울)은 M004 Mix를 명시 → SauceMiniGame이 아닌 MixMiniGame이 실행됨.

### 1.1 CookingToolData 필드

```csharp
public string id;              // T001~T005
public string cookerName;      // 한글 표시명 (팬/냄비/보울/도마/철판)
public Sprite defaultImage;    // 조리 결과 미할당 시 표시할 기본 스프라이트
```

`Init(sprite, id, name)`로 런타임 초기화도 가능. `Equals`/`GetHashCode`가 id 기준으로 오버라이드되어 있음.

---

## 2. 도구 업그레이드

`Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv` — 각 도구마다 3단계(L0/L1/L2).

`Assets/Scripts/Domain/Shop/ToolUpgradeData.cs`

| toolId | level | cost | staminaCost | durationMultiplier |
|---|---:|---:|---:|---:|
| T001 (팬) | 0 | 0 | 5 | 1.00 |
| T001 (팬) | 1 | 5000 | 4 | 0.80 |
| T001 (팬) | 2 | 12000 | 3 | 0.60 |
| T002 (냄비) | 0 | 0 | 5 | 1.00 |
| T002 (냄비) | 1 | 5000 | 4 | 0.80 |
| T002 (냄비) | 2 | 12000 | 3 | 0.60 |
| T003 (보울) | 0 | 0 | 5 | 1.00 |
| T003 (보울) | 1 | 5000 | 4 | 0.80 |
| T003 (보울) | 2 | 12000 | 3 | 0.60 |
| T004 (도마) | 0 | 0 | 5 | 1.00 |
| T004 (도마) | 1 | 5000 | 4 | 0.80 |
| T004 (도마) | 2 | 12000 | 3 | 0.60 |
| T005 (철판) | 0 | 0 | 5 | 1.00 |
| T005 (철판) | 1 | 5000 | 4 | 0.80 |
| T005 (철판) | 2 | 12000 | 3 | 0.60 |

모든 도구의 스펙이 동일하다 (2026-07 시점).

### 2.1 durationMultiplier가 적용되는 방식

`MiniGameManager.PlayAsync()` (`MiniGameManager.cs:47~50`):

```csharp
var upgradeData = GameSessionRoot.Instance?.ToolUpgrade?.GetCurrentData(toolId);
float multiplier = upgradeData?.durationMultiplier ?? 1f;
int staminaCost = upgradeData?.staminaCost ?? 5;
currentGame.ApplyUpgrade(multiplier, staminaCost);
```

각 미니게임이 `ApplyUpgrade()`를 자기 방식으로 소비:

| 미니게임 | 업그레이드 효과 (multiplier 적용) |
|---|---|
| FireMiniGame (M001/M002) | `gameDuration *= multiplier` — 진행 시간 단축 (L2 시 3.0 → 1.8초) |
| MixMiniGame (M004) | `pressRequiringCount = (int)(count × multiplier)` — 필요 프레스 수 감소 (L2 시 20 → 12회) |
| SauceMiniGame (M005) | `tolerance = Ceil(tolerance / multiplier)` — 정답 허용범위 확대 (L2 시 3% → 5%) |
| SliceMiniGame (M006) | `tolerance /= multiplier` — 마우스 오차 허용 확대 (L2 시 0.1 → ~0.167) |
| GriddleMinigame (M007) | `totalArrowCount = max(3, (int)(count × multiplier))` — 화살표 개수 감소 (L2 시 10 → 6개) |

### 2.2 staminaCost가 적용되는 방식

`MiniGameAbstract.EndGame()` (`MiniGameAbstract.cs:65`):

```csharp
GameSessionRoot.Instance?.Stats.SubStamina(upgradedStaminaCost);
```

미니게임 완료 시 도구 upgrade level에 해당하는 stamina만큼 소모 (L0: 5 / L1: 4 / L2: 3).

---

## 3. 도구별 상세 스펙

### 3.1 T001 팬 (Pan)

- **자산**: `Assets/Bundles/ScriptableObjects/CookingTools/Pan.asset`
- **cookerName**: 팬
- **주 미니게임**: FireMiniGame (M001)
- **특징**: 굽는 계열. 스페이스바를 눌러/뗴 화살표를 게이지 중앙 안전 구간(safeZone 25%)에 유지.

이 도구에 진입 가능한 재료: [../content/foods.md](../content/foods.md) 참조 — `availableTools`에 `T001` 포함하는 재료(원재료 및 중간재).

### 3.2 T002 냄비 (Pot)

- **자산**: `Assets/Bundles/ScriptableObjects/CookingTools/Pot.asset`
- **cookerName**: 냄비
- **주 미니게임**: FireMiniGame (M002 — 팬과 스크립트 공유, 프리팹만 별도)
- **특징**: 끓이는 계열. 물, 밀면, 두부 등 액체·삶기 재료에 사용.

### 3.3 T003 보울 (Bowl)

- **자산**: `Assets/Bundles/ScriptableObjects/CookingTools/Bowl.asset`
- **cookerName**: 보울
- **폴백 미니게임**: SauceMiniGame (M005)
- **실제 사용**: 대부분의 T003 레시피는 `minigameId = M004 (Mix)` 명시. M005(Sauce)는 소스류(R022 간장 코팅 밥, R026 네온 샐러드)에서 사용.
- **특징**: 섞기, 소스 만들기.

### 3.4 T004 도마 (Cutting Board)

- **자산**: `Assets/Bundles/ScriptableObjects/CookingTools/CuttingBoard.asset`
- **cookerName**: 도마
- **주 미니게임**: SliceMiniGame (M006)
- **특징**: 세로 스와이프로 6번 절단. 재료가 `pieceSprite`를 가지면 절단 시 조각 스프라이트 표시 (I009 인공고기, I012 절연 버섯 등).

### 3.5 T005 철판 (Griddle)

- **자산**: `Assets/Bundles/ScriptableObjects/CookingTools/griddle.asset` (파일명만 소문자, id는 T005)
- **cookerName**: 철판
- **주 미니게임**: GriddleMinigame (M007)
- **특징**: 방향키 콤보. 스택으로 3개 화살표가 표시되면서 아래부터 정답 입력.

---

## 4. 도구 인터랙션 규칙 요약

`Assets/Scripts/Schema/State/Cooking/CookingToolSchema.cs`:

- **재료 추가**: `FoodData.availableTools`가 도구 id를 포함해야 함.
- **중복 재료 불가**: 동일 `foodData.id`를 두 번 넣을 수 없음.
- **최대 재료 수**: 7개 (`maxIngredientSize`, 기본값).
- **result가 있으면**: 새 재료 추가 시 result가 도구와 호환되어야 하고, result는 `Ingredients` 리스트 마지막에 자동 추가된 뒤 새 재료 추가.
- **미니게임 시작 조건**: `IsCookable() = !locked && Ingredients.Count != 0` — 재료가 하나라도 있으면 미니게임 실행 가능. 실제 유효한 레시피 조합이 아니면 `Search`가 R000(음식물쓰레기)을 반환.

---

## 5. 도구에서 다음 도구/도시락으로 이송

`CookingToolModel.OnToolDropped()` — 다른 도구/도시락 위에 드롭 시:

1. **다른 도구로**: result가 있으면 result만 이송, 없으면 재료 목록 통째로 이송 (모두 IsAddable pass 시).
2. **도시락으로**: result가 non-null이고 result의 type이 MAIN/SIDE일 때만 성공.
3. **쓰레기통**: `Tags.Trashcan` 태그와 겹치면 `ClearIngredient()` + trashcan SFX.

---

## 6. 관련 파일

- `Assets/Scripts/Schema/Config/Common/CookingToolData.cs` — SO 정의
- `Assets/Scripts/Schema/State/Cooking/CookingToolSchema.cs` — 도구 상태/규칙
- `Assets/Scripts/Unity/Cooking/CookingToolModel.cs` — 뷰-컨트롤러
- `Assets/Scripts/Unity/Cooking/CookingToolBehavior.cs` — 렌더/애니
- `Assets/Scripts/Domain/Shop/ToolUpgradeData.cs` — 업그레이드 데이터 파싱
- `Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv` — 업그레이드 CSV
- `Assets/Bundles/ScriptableObjects/CookingTools/` — 5개 도구 asset

**미니게임 매핑 코드**: `Assets/Scripts/Unity/Cooking/MiniGameManager.cs:66~82` (`GetPrefab`)
