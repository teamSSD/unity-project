# 메뉴 조합 규칙 (Menu Compositions)

플레이어가 하루 영업 전 도시락 3개 슬롯을 선정하고, 손님/배달 주문이 이 메뉴에서 뽑히는 흐름. 도시락 구성 제약과 보상 공식을 다룬다.

관련 문서: [customer-system.md](./customer-system.md), [cooking-system.md](./cooking-system.md), [../content/foods.md](../content/foods.md), [../content/recipes.md](../content/recipes.md)

---

## 1. 데이터 모델

### 1.1 MenuSelection (플레이어 편성 상태)

`Assets/Scripts/Schema/State/Cooking/MenuSelection.cs`

```csharp
public class MenuSelection {
    public string Name;                  // "도시락 1" / "도시락 2" / "도시락 3"
    public FoodData MainMenu;            // 메인 요리 (필수, MAIN type)
    public List<FoodData> SideMenus;     // 사이드 최대 3개 (SIDE type)

    public bool HasSelection() { return MainMenu != null; }
    public bool IsComplete()   { return MainMenu != null; }
    public bool HasSideOnly()  { return MainMenu == null && SideMenus.Count > 0; }

    public void AddSide(FoodData food) {
        if (SideMenus.Count < 3) SideMenus.Add(food);
    }
}
```

- **메인**: 1개 (필수).
- **사이드**: 0~3개 (최대 3).
- Main 없이 Side만 있는 상태 = 성립 불가 (`HasSideOnly` → Confirm 시 팝업 트리거).

### 1.2 MenuSelectionService (도시락 3 슬롯 관리)

`Assets/Scripts/Domain/Cooking/MenuSelectionService.cs`

- 하루에 3개 도시락 슬롯 (`menuSelections[3]`) — "도시락 1/2/3".
- `HasAnySelection()`: 최소 1개 슬롯이 편성되었는지.
- `GetInvalidSlotIndices()`: Main 없이 Side만 있는 슬롯 (Confirm 검증용).
- `GetAllMenusAsSchema()`: 편성된 슬롯을 `MenuSchema` 리스트로 반환 (실 영업에서 사용).
- Save/Load: `RecipeBookSaveData`로 `gamedata.json`에 직렬화.

### 1.3 MenuSchema (런타임 주문 표현)

`Assets/Scripts/Schema/State/Cooking/MenuSchema.cs`

```csharp
public class MenuSchema {
    public string name;                   // 도시락 이름
    public int orderNumber;               // 손님 주문 번호 (증가)
    public List<FoodData> mainMenus;      // 실질적으로 1개 (첫 요소가 mainMenu)
    public List<FoodData> sideMenus;      // 0~3개

    public FoodData mainMenu => mainMenus?.FirstOrDefault();
}
```

`MenuSchema`는 배달 퀘스트에서 `mainMenus`가 2개 이상일 수 있게 설계됨 (예: `night_market` 그룹 quest → 구룡면 + 스트리트 스테이크 49).

---

## 2. 메뉴 편성 흐름

1. 플레이어가 Recipe Book UI에서 "도시락 1/2/3" 편성.
2. 각 슬롯에 대해:
   - Main으로 사용 가능한 FoodData: `type == MAIN`
   - Side로 사용 가능한 FoodData: `type == SIDE`
   - 잠금 해제된 (unlocked) 요리만 선택 가능 (`IUnlockedFoodProvider`).
3. Confirm 시 `GetInvalidSlotIndices` 검증 → Side만 있는 슬롯 존재 시 경고 팝업.
4. Cooking 씬 진입 시 `MenuSelectionService.GetAllMenusAsSchema()`가 `RecipeBookMenuProvider.GetTodaysMenu()`를 통해 CustomerManager에 전달.

`RecipeBookMenuProvider` (`Assets/Scripts/Unity/Cooking/RecipeBookMenuProvider.cs`):
- 편성된 메뉴가 하나도 없으면 fallback으로 카탈로그 첫 FoodData를 "디버그 메뉴"로 사용.

---

## 3. 손님이 뽑는 메뉴

`CustomerManager.PickRandomMenu()` (`CustomerManager.cs:168~179`):

- `salesMenus` (편성된 도시락 리스트)에서 uniform random pick.
- 각 픽마다 `nextOrderNumber++` → 주문번호 라벨링.
- 슬롯당 편성이 있어야 (`HasAnySelection` false면 씬 진입 자체가 막힘, `CustomerManager.Awake:65~70`).

**즉**: 플레이어가 편성한 도시락 종류만 손님이 주문한다. 편성이 3개면 골고루, 1개면 100% 그것만 주문.

---

## 4. MenuValidator 보상 공식

상세 산식은 [customer-system.md §5](./customer-system.md#5-menuvalidator-점수--보상-공식) 참조. 핵심만 재수록.

`MenuValidator.CalculateReward()` (`Assets/Scripts/Domain/Common/MenuValidator.cs:203~234`):

```
totalPrice = providedMain.Price + Σ providedSide.Price       # 페널티 없이 단순 합
mainMultiplier = mainMatches ? 1.0 : 0.7
sideMultiplier = 1.0 + matchingSidesCount × 0.15             # 2026-07: 5% → 15% 튜닝

reward = round(totalPrice × mainMultiplier × sideMultiplier)
```

- `providedMain.Price` — 도시락에 담긴 메인의 `Price` (조리 시 `CookingToolSchema.Cook`이 산출).
- `providedSide.Price` — 각 사이드의 `Price` 합.
- 메인이 order와 일치하지 않으면 전체 총액의 30% 감점.
- 사이드가 order와 매칭될 때마다 +15% 승수.

### 4.1 매칭 로직

- **Main 매칭**: `order.mainMenu.id == providedMain.foodData.id`.
- **Side 매칭**: `HashSet<string>(order.sideMenus.Select(id))` 기반 id 매칭 (순서 무관).

### 4.2 accuracyScore와 reward의 관계

- **accuracyScore** (0.0~1.0): S/A/B/C/D/F 등급 산출용 (`MenuValidator.CalculateAccuracyScore`).
- **reward** (골드): 실제 지급 골드 산출용 (`MenuValidator.CalculateReward`).
- 두 산식은 독립적으로 유지된다 (예: 사이드 정확도는 accuracyScore에선 페널티 방식, reward에선 승수 방식).

---

## 5. 실제 사용되는 조합 예시 (배달 퀘스트)

`Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv`:

| GroupId | MenuName | Main1 | Main2 | Side1 | Side2 | Side3 |
|---|---|---|---|---|---|---|
| power_room_pair | 전력실 도시락 | I044 기계장 고기정식 | - | I058 전력실 꼬치 | - | - |
| night_market | 야시장 도시락 | I039 구룡면 | I049 스트리트 스테이크 49 | - | - | - |
| nimo_solo | 새벽국 도시락 | I034 새벽국 | - | - | - | - |
| seraph_solo | 네온 샐러드 | I060 옥상 오믈렛 | - | I056 네온 샐러드 | - | - |
| lede_solo | 삼각밥 도시락 | I053 폐건물 삼각밥 | - | - | - | - |
| gabriel_solo | 기계장 고기정식 | I044 기계장 고기정식 | - | - | - | - |

**관측 사항**:
- `night_market`은 메인이 **2개** (예외적, 그룹/페어 quest 용도).
- 대부분 solo quest는 메인만 있고 사이드 없음 (단순 요구).
- `power_room_pair`, `seraph_solo`가 메인+사이드 1개 조합.

배달 퀘스트는 정확 일치 요구 (`OrderTicketModel.ValidateExactMatch`) — 부분 매칭 불가.

일반 손님 주문은 플레이어가 편성한 도시락 3종 중 하나를 랜덤 픽 → 편성이 [1 main] 또는 [1 main + N sides] 조합이므로 실제 조합은 편성에 달림.

---

## 6. 도시락 물리적 슬롯 (Bento 인스턴스)

`BentoModel.cs`

- `maxFoodSlots = 4` — 도시락 하나에 최대 4개 FoodSchema.
- 첫 번째 추가된 항목이 자동으로 Main으로 취급 (도시락 완성 시 `foodList[0]` = main, 나머지 = sides — `OrderTicketModel.WaitAndTakeAsync:133~135`).
- MAIN/SIDE type만 허용 (`IngredientPlacementRules.CanFoodEnterBento` → PROCESSING/INGREDIENT는 거부).

**중요**: 도시락 자체는 순수 컨테이너다. Main이 반드시 첫번째 슬롯인지는 물리적 규칙이 아니라 "먼저 넣은 게 main으로 취급"이라는 관례. 플레이어가 side를 먼저 넣으면 side가 main으로 취급되어 주문 검증에서 mainCorrect=false → 30% 감점 위험.

---

## 7. 편성 UI 컴포넌트

- `Assets/Scripts/Unity/UI/BentoSelectionController.cs` — 도시락 편성 UI.
- Uses `IUnlockedFoodProvider`로 잠금 해제된 요리만 후보로 표시.
- `MenuSelectionService.AddSide` 호출 시 max 3 검증.

---

## 8. 관련 파일

- `Assets/Scripts/Schema/State/Cooking/MenuSelection.cs` — 편성 상태
- `Assets/Scripts/Schema/State/Cooking/MenuSchema.cs` — 주문 표현
- `Assets/Scripts/Schema/State/Cooking/RecipeBookSaveData.cs` — 저장 형식
- `Assets/Scripts/Domain/Cooking/MenuSelectionService.cs` — 3슬롯 관리 서비스
- `Assets/Scripts/Domain/Common/MenuValidator.cs` — 정확도/보상 산식
- `Assets/Scripts/Unity/Cooking/RecipeBookMenuProvider.cs` — Cooking 씬 어댑터
- `Assets/Scripts/Unity/UI/BentoSelectionController.cs` — 편성 UI
- `Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv` — 배달 퀘스트 조합
