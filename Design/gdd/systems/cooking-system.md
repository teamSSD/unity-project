# 조리 시스템 (Cooking System)

Aftertaste의 핵심 게임루프. 손님 주문 → 재료 조합 → 미니게임 → 도시락 완성 → 서빙의 순환을 담당한다. 본 문서는 Cooking 씬의 구조, 조리 파이프라인, 미니게임 5종의 판정 방식을 다룬다.

관련 문서: [customer-system.md](./customer-system.md), [cooking-tools.md](./cooking-tools.md), [../content/foods.md](../content/foods.md), [../content/recipes.md](../content/recipes.md), [menu-compositions.md](./menu-compositions.md)

---

## 1. Cooking 씬 개요

Cooking 씬은 하루 4개의 페이즈(Morning/Afternoon/Evening/Night) 중 실제 영업(주문 처리)이 이뤄지는 서브 씬이다. Mall/Cooking을 왕복하며 진행되고, 하루 종료 시 정산으로 이어진다.

### 1.1 주요 GameObject

`Assets/Scripts/Unity/Cooking/CookingSceneManager.cs`가 씬 진입 시점 조립을 담당한다.

| 컴포넌트 | 스크립트 | 역할 |
|---|---|---|
| CookingSceneManager | `CookingSceneManager.cs` | 도구/저장소 주입, 인벤토리 재료 스폰, 튜토리얼 트리거 |
| MiniGameManager | `MiniGameManager.cs` | 미니게임 프리팹 라우팅 및 실행 |
| CustomerManager | `CustomerManager.cs` | 손님 스폰 루프 및 lifecycle 코디네이션 |
| Refrigerator/UpperShelf/LowerShelf | `BaseStorage.cs` 계열 | 재료 저장소 3종 (그리드/수평선형/수평선형) |
| CookingTool ×5 | `CookingToolModel.cs` | 팬/냄비/보울/도마/철판 (T001~T005) |
| BentoSet | `BentoSetModel.cs` | 클릭 시 새 도시락 인스턴스 스폰 |
| BentoPosition ×N | `BentoPositionModel.cs` | 도시락이 안착하는 슬롯 (점선 하이라이트) |

### 1.2 저장소 3종 기본 용량

`Assets/Scripts/Unity/Cooking/{Refrigerator,UpperShelf,LowerShelf}.cs`

| 저장소 | Default Capacity | 레이아웃 |
|---|---|---|
| Refrigerator | 7 | 3열 그리드 (`xInterval × yInterval`) |
| UpperShelf | 3 | 수평 선형 |
| LowerShelf | 4 | 수평 선형 |

용량은 `ShopStorageUpgrade` 업그레이드로 증설 가능. `Assets/Scripts/Unity/Cooking/CookingSceneManager.cs:37~39`에서 `InjectStorageCapacity()`가 upgrade 값을 주입한다.

### 1.3 재료가 저장소에 배치되는 규칙

`IngredientData.display`(`Refrigerator` / `UpperShelf` / `LowerShelf` / `NONE`) 필드가 저장소를 결정한다. `CookingSceneManager.FillStorage()`가 씬 진입 시 인벤토리에서 재료를 카테고리별로 뽑아 각 저장소에 인스턴스로 배치한다.

카테고리별 분포는 [../content/ingredients.md](../content/ingredients.md) 참조.

---

## 2. 조리 파이프라인

```
재료(Ingredient) ─drop→ CookingTool ─click→ Minigame ─score→ Result(FoodSchema) ─drop→ 다른 Tool 또는 Bento
```

`Assets/Scripts/Unity/Cooking/CookingToolModel.cs`가 도구 관점의 상태를 관리한다. `Assets/Scripts/Schema/State/Cooking/CookingToolSchema.cs`가 실제 규칙(추가 가능 여부/조리 결과 계산)을 담는다.

### 2.1 재료 드롭 규칙

`Assets/Scripts/Domain/Cooking/IngredientPlacementRules.cs`

- **Tool 진입 조건**: `FoodData.availableTools`에 도구 id가 포함되어 있어야 함.
- **Bento 진입 조건**: `FoodType.MAIN` 또는 `FoodType.SIDE`만 허용. raw INGREDIENT/PROCESSING은 거부.

`FoodModel.OnFoodDropped()` (`FoodModel.cs:195~230`)이 단일 dispatched 진입점으로, 우선순위는 **CookingTool → Bento**.

### 2.2 조리 도구 상태 (CookingToolSchema)

- `Ingredients`: 최대 `maxIngredientSize=7`개 (하지만 실 사용은 레시피 요구량).
- 동일 재료 중복 추가 금지 (`IsAddable`에서 `IsSameFood` 체크).
- `result != null` 상태에서 도구 id가 result의 `availableTools`에 포함되지 않으면 추가 거부.
- `IsCookable() = !locked && Ingredients.Count != 0` — 재료가 하나라도 있으면 미니게임 시작 가능.

### 2.3 조리 시작 → 미니게임 실행

`CookingToolModel.PlayMinigame()` (`CookingToolModel.cs:196`):

1. `UILockManager.IsLocked` 검사 (튜토리얼 등).
2. `SchemaInstance.IsCookable()` 확인.
3. `searchRecipeUsecase.Search(toolId, ingredients)` → RecipeData 조회.
4. `playMinigameUsecase.PlayAsync()` → 미니게임 실행.
5. 미니게임 종료 시 `OnMinigameEnd(recipe, score)` 콜백.

레시피가 없는 재료 조합의 경우 `Search`가 R000(음식물쓰레기)을 반환하도록 catalog에 정의되어 있다.

### 2.4 조리 결과 가격 계산

`CookingToolSchema.Cook()` (`CookingToolSchema.cs:81~100`):

```
newPrice = Σ ( ingredient.Price × (0.85 + inputInfo.foodWeight × score) )
if (foodData.type == MAIN || SIDE):
    chainBonus = 1 + 0.15 × (chainDepth - 1)
    newPrice *= chainBonus
```

- **재료별 기여**: 각 재료 가격 × `(0.85 + weight × score)` 합산. `foodWeight`는 `RecipeData.inputs`의 weight (0.2 ~ 1.5 사이).
- **체인 보너스**: 완성된 요리(MAIN/SIDE)에 대해 `SearchDataUtil.GetChainDepth(foodId)` (`SearchDataUtil.cs:52`)로 재귀 깊이 계산. 원재료=0, 1단계=1, 2단계=2… 깊이가 깊을수록 `1 + 0.15(d-1)` 배 보너스.

### 2.5 조리 도구 업그레이드

`Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv`

| toolId | level | cost | staminaCost | durationMultiplier |
|---|---|---|---|---|
| T001~T005 | 0 | 0 | 5 | 1.00 |
| T001~T005 | 1 | 5000 | 4 | 0.80 |
| T001~T005 | 2 | 12000 | 3 | 0.60 |

`MiniGameManager.PlayAsync()` (`MiniGameManager.cs:47~50`)이 도구별 현재 level에서 `durationMultiplier`와 `staminaCost`를 읽어 미니게임에 `ApplyUpgrade(multiplier, staminaCost)` 주입. 각 미니게임은 자기 방식으로 이 값을 소비 (뒤 §3 참조).

`MiniGameAbstract.EndGame()`이 종료 시 `Stats.SubStamina(upgradedStaminaCost)` 호출 → 스태미나 소모.

### 2.6 결과물 → 다음 도구 or 도시락 이송

`CookingToolModel.OnToolDropped()` (`CookingToolModel.cs:118`):

- 다른 CookingTool 위에 드롭 → `TryTransferToTool()`: 조리 완료된 result가 있으면 result만 이송, 없으면 재료 목록 통째로 이송 (모두 IsAddable 통과 시).
- Bento 위에 드롭 → `TryTransferToBento()`: `GetResult()`가 non-null이고 result가 MAIN/SIDE type일 때만 성공.
- Trashcan 태그 위에 드롭 → `DetectTrashcan()`: 재료 전체 폐기.

### 2.7 도시락 완성 → 서빙

`Assets/Scripts/Unity/Cooking/BentoModel.cs`:

- `maxFoodSlots = 4` — 도시락에 최대 4개 아이템 (Main 1 + Side 3).
- 첫 번째 항목이 자동으로 Main으로 취급됨 (`OrderTicketModel.WaitAndTakeAsync`: `foodList[0]`이 main).
- Side 이미지는 `foodList` 내 SIDE 인덱스(0/1/2)에 따라 `FoodData.GetSideBentoImage(sideOrder)`로 다른 sprite 표시.

`OrderTicketModel.AddToBento()` (영수증 → 도시락):
1. 완성된 도시락 위에 주문 티켓 드래그 앤 드롭.
2. 티켓이 도시락 자식으로 reparent, `attachedScale=0.7` 배로 축소 (`OrderTicketModel.cs:23`).
3. Delivery ticket인 경우 `ValidateExactMatch()`로 요구 조합과 정확히 일치해야 부착 성공.
4. 0.5s~1.5s 랜덤 대기 후 `onTake` 이벤트 발화 → `CustomerLifecycle.OnOrderDelivered()` → `MenuValidator.Validate` + `CalculateReward`.

---

## 3. 미니게임 5종

`MiniGameManager` (`MiniGameManager.cs:66~82`)이 `recipeData.minigameId`(우선) 또는 `toolId`(폴백)로 프리팹 선택.

| minigameId | 프리팹 | 스크립트 | 폴백 도구 |
|---|---|---|---|
| M001 | BakeMinigamePrefab | FireMiniGame (팬 상황) | T001 팬 |
| M002 | BoilMinigamePrefab | FireMiniGame (냄비 상황) | T002 냄비 |
| M004 | MixMinigamePrefab | MixMiniGame | (없음, T003 폴백은 Sauce) |
| M005 | SauseMinigamePrefab | SauceMiniGame | T003 보울 |
| M006 | CutMinigamePrefab | SliceMiniGame | T004 도마 |
| M007 | GrillMinigamePrefab | GriddleMinigame | T005 철판 |

> 참고: `M003`은 사용되지 않는다 (레시피 CSV에도 없음). `ClickMiniGame.cs`는 임시/튜토리얼 용도의 스텁으로만 존재.
> `FireMiniGame`은 팬(M001)과 냄비(M002) 두 프리팹이 같은 스크립트를 공유 (사운드/시각만 다름).

공통 인터페이스는 `MiniGameAbstract.cs`:

- `StartGame()` → `OnGameStarted()` (SFX loop 시작 등) → 매 프레임 `OnUpdate()` → `EndGame()` → `CalculateScore()` → `OnGameFinished(score)`.
- 종료 시 `Stats.SubStamina(upgradedStaminaCost)` 호출.
- `SetIngredients(foods, toolId)`: 재료 sprite를 스택 렌더링 (`SpriteStackRenderer`).
- `ApplyUpgrade(multiplier, staminaCost)`: 각 미니게임이 자기 방식으로 `multiplier`를 소비.

### 3.1 FireMiniGame (M001 팬 / M002 냄비)

`Assets/Scripts/Unity/Cooking/FireMiniGame.cs`

- **입력**: 스페이스바 누르면 화살표 상승, 뗴면 하강.
- **목표**: 게이지의 세이프존(중앙 `safeZoneRatio=0.25` 폭) 안에서 화살표 유지.
- **점수 산출**: 매 프레임 `CalculateFrameScore()`:
  - `distFromCenter ≤ safeHalf` → 1.0
  - 그 밖 → `1 - (diff² / maxPenaltyDist²)`
  - 프레임 점수를 `Time.deltaTime / gameDuration` 가중치로 누적 → `accScore` (`Mathf.Clamp01`).
- **duration**: `gameDuration = 3.0f`, 업그레이드로 `gameDuration *= multiplier` (L2 시 1.8초).
- **cold start**: 시작 후 `coldStartTime=0.5s` 동안 가속/최대 속도 점진 증가 (`rampFactor`).
- **파라미터**: `acceleration=2.0`, `maxVelocity=1.0`.

### 3.2 MixMiniGame (M004 보울)

`Assets/Scripts/Unity/Cooking/MixMiniGame.cs`

- **입력**: 스페이스바 반복 프레스.
- **목표**: `pressRequiringCount = 20`회 프레스 (업그레이드 시 `(int)(count × multiplier)`, L2 시 12회).
- **duration**: `duration = 15초` (강제 종료).
- **점수 산출**: `Mathf.SmoothStep(1, 0, (timer - idleClearTime=2) / (duration - idleClearTime))` — 빠르게 끝낼수록 만점, 지연될수록 0에 가까워짐.
- **애니메이션**: 프레스 사이 시간이 짧을수록 회전 rod가 빠르게 한 바퀴 도는 반응(수제 회전 애니).

### 3.3 SauceMiniGame (M005 보울 계열 소스류)

`Assets/Scripts/Unity/Cooking/SauceMiniGame.cs`

- **입력**: 위/아래 화살표 번갈아 프레스 (매 프레스마다 `decreasePerPress=2.5%`씩 게이지 감소).
- **목표**: 랜덤 목표 게이지(`targetGauge`)에 근접해서 멈추기.
  - 매 게임 시작 시 `GameRandom.Normal(mean=50, σ=10)` 정규분포에서 뽑아 `[20,70]` 클램프 후 `2.5%` 배수로 스냅.
- **정지 조건**: `waitingThreshold=0.7s` 동안 입력 없음 or 게이지 0.
- **점수 산출**:
  - `diff = |current - target|`
  - `diff ≤ tolerance=3%` → 1.0
  - `diff ≥ zeroScoreDiff=20%` → 0.0
  - 그 사이 linear.
- **업그레이드**: `tolerance = Ceil(tolerance / multiplier)` — L2 시 `Ceil(3/0.6)=5%` 허용.

### 3.4 SliceMiniGame (M006 도마)

`Assets/Scripts/Unity/Cooking/SliceMiniGame.cs`

- **입력**: 마우스 드래그 (위→아래 스와이프).
- **목표**: `totalSlices = 6`번의 세로 절단선 위를 정확히 따라 긋기.
- **각 절단**: `segmentsPerSlice = 20` 세그먼트로 분할, 세그먼트별 `SliceScorer.RecordSegment()`가 마우스 x 좌표와 목표 x 오차를 `tolerance=0.1` 기준으로 채점.
- **점수 산출**: `SliceScorer.GetFinalScore()` — 각 슬라이스 세그먼트별 hit 비율.
- **업그레이드**: `tolerance /= multiplier` — L2 시 `0.1/0.6 ≈ 0.167` (더 관대).
- **pieceSprite**: `FoodData.pieceSprite`가 있으면 절단 시 조각 스프라이트 표시 (INGREDIENT + availableTools 포함 T004인 재료만: I009, I012 등).

### 3.5 GriddleMinigame (M007 철판)

`Assets/Scripts/Unity/Cooking/GriddleMinigame.cs`

- **입력**: 4방향 화살표 키.
- **목표**: `totalArrowCount = 10`개의 방향 화살표를 순서대로 정확히 눌러 처리.
- **동시 표시**: `MaxVisibleCount = 3`개의 화살표가 스택으로 표시, 정답 시 제거되고 다음 화살표 스폰.
- **오답 처리**: 오답도 화살표는 처리되고 넘어감 (`Fail()` 처리 후 `_processedCount++`).
- **점수 산출**: `_successCount / totalArrowCount`, 최소값 0.01.
- **업그레이드**: `totalArrowCount = max(3, (int)(count × multiplier))` — L2 시 6개.

---

## 4. 성공/실패 판정

미니게임 자체는 항상 종료되며 `score ∈ [0,1]`을 반환한다. "실패"의 개념은:

1. **미니게임 낮은 점수** → `RecipeData` 자체는 성공하지만 `foodWeight × score` 곱에 의해 가격이 크게 줄어듦 (`Cook()` 공식).
2. **재료 조합 불일치** → `Search()`가 R000(음식물쓰레기)을 반환할 수 있음. 처리 필요.
3. **손님 인내심 소진** → 대기 손님이 `defaultCustomerWaitTime=90초` 안에 서빙받지 못하면 `WaitingCustomer.OnExit()` → `CustomerLifecycle.OnCustomerTimeout()` → 손님 exit 애니.
4. **주문과 불일치한 도시락** → 손님이 받긴 하지만 `MenuValidator`가 낮은 점수로 판정, 손님의 `unsatisfiedMessage` 표시. 상세 산식은 [customer-system.md §5](./customer-system.md#5-menuvalidator-점수--보상-공식).

---

## 5. 도시락 배치와 사이드 이미지

`BentoModel.AddIngredient()` (`BentoModel.cs:69~98`):

- MAIN 첫 추가 시 → `foodData.GetMainBentoImage()` = `bentoVariants[0]`.
- SIDE 추가 시 → 현재 도시락 내 SIDE 개수를 `sideCount`로 → `foodData.GetSideBentoImage(sideCount)` = `bentoVariants[1 + sideCount]` (0/1/2 순서로 left/middle/right).

따라서 같은 SIDE라도 도시락 내 배치 순서에 따라 다른 sprite가 표시된다. FoodData의 `bentoVariants[4]` 배열은 `[0]main, [1]left, [2]middle, [3]right` 구조.

---

## 6. 도시락 재활용 (BentoSet)

`Assets/Scripts/Unity/Cooking/BentoSetModel.cs`

- 씬에 하나 배치되는 "빈 도시락 스택".
- 클릭+드래그 시작 시 새 도시락 프리팹 인스턴스를 마우스 위치에 생성 (`SpawnBento`).
- 인스턴스의 `localScale`은 소스 오브젝트의 `lossyScale`을 따르도록 최근 수정됨 (2026-07 commit `ddfbc8e`).

빈 도시락은 BentoPosition 위에 안착시켜야 재료를 받을 수 있음. 안착 전 다른 위치에 놓이면 자동 파괴.

---

## 7. 관련 파일 목록

**핵심 스크립트**
- `Assets/Scripts/Unity/Cooking/CookingSceneManager.cs` — 씬 컨트롤러
- `Assets/Scripts/Unity/Cooking/CookingToolModel.cs` — 도구 뷰-컨트롤러
- `Assets/Scripts/Schema/State/Cooking/CookingToolSchema.cs` — 도구 상태 규칙 + `Cook()` 가격 공식
- `Assets/Scripts/Unity/Cooking/MiniGameManager.cs` — 미니게임 라우팅
- `Assets/Scripts/Unity/Cooking/MiniGameAbstract.cs` — 공통 인터페이스
- `Assets/Scripts/Unity/Cooking/FoodModel.cs` — 재료 뷰
- `Assets/Scripts/Unity/Cooking/BentoModel.cs` — 도시락 뷰
- `Assets/Scripts/Domain/Cooking/IngredientPlacementRules.cs` — 드롭 규칙 (POCO)
- `Assets/Scripts/Unity/Common/SearchDataUtil.cs` — 카탈로그 조회 + 체인 깊이 계산

**미니게임**
- `Assets/Scripts/Unity/Cooking/FireMiniGame.cs` (M001 / M002)
- `Assets/Scripts/Unity/Cooking/MixMiniGame.cs` (M004)
- `Assets/Scripts/Unity/Cooking/SauceMiniGame.cs` (M005)
- `Assets/Scripts/Unity/Cooking/SliceMiniGame.cs` (M006)
- `Assets/Scripts/Unity/Cooking/GriddleMinigame.cs` (M007)
- `Assets/Scripts/Unity/Cooking/ClickMiniGame.cs` (임시/튜토리얼)

**저장소**
- `Assets/Scripts/Unity/Cooking/BaseStorage.cs` — 추상 기반
- `Assets/Scripts/Unity/Cooking/Refrigerator.cs`
- `Assets/Scripts/Unity/Cooking/UpperShelf.cs`
- `Assets/Scripts/Unity/Cooking/LowerShelf.cs`

**데이터 정의**
- `Assets/Scripts/Schema/Config/Cooking/FoodData.cs`
- `Assets/Scripts/Schema/Config/Cooking/RecipeData.cs`
- `Assets/Scripts/Schema/Config/Cooking/IngredientData.cs`
- `Assets/Scripts/Schema/Config/Common/CookingToolData.cs`

**CSV 데이터 소스**
- `Assets/Bundles/driveAssets/dataTables/food.csv` (66행)
- `Assets/Bundles/driveAssets/dataTables/recipe.csv` (31행)
- `Assets/Bundles/driveAssets/dataTables/ingredient.csv` (35행)
- `Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv` (15행)
