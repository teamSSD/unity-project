# QA 인풋 — 콘텐츠 관찰 지점 인벤토리

> **이 문서 성격**: 게임 콘텐츠(요리·재료·레시피·NPC·작물·배달 그룹·도구)가 **게임 내 어디서 어떻게 관찰되는가**를 정리. 전체 리스트는 원본 [`../content/`](../content/) 참조.
> **QA 영역 침범 금지**: 테스트 절차/기대치/우선순위/pass·fail 판정 없음.
> **누락 방지**: 각 콘텐츠 유형마다 (1) 정적 사양 (2) 관찰 지점 (3) 변형·상태 (4) 조건부 노출.

---

## 개요 — 콘텐츠 유형 요약

| 유형 | 개수 | 정의 스크립트 | 원본 |
|---|---:|---|---|
| FoodData | 66 | [`FoodData.cs`](../../../Assets/Scripts/Schema/Config/Cooking/FoodData.cs) | [`content/foods.md`](../content/foods.md) |
| IngredientData | 35 | [`IngredientData.cs`](../../../Assets/Scripts/Schema/Config/Cooking/IngredientData.cs) | [`content/ingredients.md`](../content/ingredients.md) |
| RecipeData | 31 | [`RecipeData.cs`](../../../Assets/Scripts/Schema/Config/Cooking/RecipeData.cs) | [`content/recipes.md`](../content/recipes.md) |
| CookingToolData | 5 | [`CookingToolData.cs`](../../../Assets/Scripts/Schema/Config/Common/CookingToolData.cs) | [`systems/cooking-tools.md`](../systems/cooking-tools.md) |
| DeliveryNpcData | 10 | [`DeliveryNpcData.cs`](../../../Assets/Scripts/Schema/Config/Mall/DeliveryNpcData.cs) | [`content/npcs.md`](../content/npcs.md) |
| CropData | 13 | (CSV `cropData.csv`) | [`content/crops.md`](../content/crops.md) |
| DeliveryGroup | 6 | (CSV `deliveryQuest.csv`) | [`content/delivery-groups.md`](../content/delivery-groups.md) |

---

## 1. FoodData (66개) — 요리·재료·중간재

### 정적 사양 요약
- 필드: `id`, `ingredientName`, `description`, `image`, `ingredient(→IngredientData)`, `availableTools[]`, `type(GARBAGE/INGREDIENT/PROCESSING/MAIN/SIDE)`, `toolVariants[5]`, `bentoVariants[4]`, `pieceSprite`
- toolVariants 인덱스: `[0]팬 [1]냄비 [2]볼 [3]도마 [4]철판`
- bentoVariants 인덱스: `[0]main [1]left [2]middle [3]right`

### 관찰 지점 (Where It Shows Up)

| 씬/UI | 표시 지점 | 사용 필드 | 관련 코드 |
|---|---|---|---|
| Cooking — 재료 저장소 | 냉장고/상단선반/하단선반의 raw 스프라이트 | `image` (raw) | `Refrigerator/UpperShelf/LowerShelf` |
| Cooking — 도구 위 조리 중 | 도구 슬롯 내 진행 중 스프라이트 | `GetImageForTool(toolId)` → `toolVariants[i]` | `CookingToolModel`, `FireMiniGame`, `MixMiniGame`, `SauceMiniGame`, `SliceMiniGame`, `GriddleMinigame` |
| Cooking — 도마 절단 조각 | 도마에서 자를 때 나뉜 조각 | `pieceSprite` | `SliceMiniGame`, `GriddleMinigame` |
| Cooking — 도시락 메인 슬롯 | 도시락의 큰 슬롯 | `bentoVariants[0]` = main | `BentoModel` |
| Cooking — 도시락 사이드 슬롯 | 도시락의 좌/중/우 | `bentoVariants[1..3]` = left/middle/right | `BentoModel` |
| Cooking — OrderTicket | 손님 주문표에 요리 이미지 | `bentoVariants[0]` + sides | `OrderTicketController` |
| Cooking — 완성 도시락 → 손님 전달 | 완성물 이미지 유지 | 위와 동일 | 손님 서빙 흐름 |
| Mall — DialogueManager 대사 삽입 | (일부) 요리 이름/스프라이트 인용 | `image`, `ingredientName` | 배달 그룹 대사 |
| RecipeBook (Tab 오버레이) | 레시피 카드에 요리 이미지 | `image` | `RecipeBookManager`, `MenuCardController`, `MenuCardItemHelper` |
| BentoSelection (Preparation) | 도시락 4칸 선택 시 완성물 미리보기 | `bentoVariants` | `BentoSelectionController`, `MenuSlot`, `MenuSelectionItem` |
| Shop — Item 탭 | 재료 아이콘 (INGREDIENT만) | `image` | `ShopUIAdapter.Item` |
| Inventory HUD (있으면) | 인벤토리 목록에 재료 스프라이트 | `image` | `InventoryService` 표시 |

### 변형/상태 (Variants)
- **raw (`image`)** — 저장소·인벤토리·상점 표시
- **toolVariants[5]** — 팬/냄비/볼/도마/철판 진행 상태 (도구별)
- **bentoVariants[4]** — 도시락 main/left/middle/right 표시
- **pieceSprite** — 도마 절단 시 조각
- **음식물쓰레기(I000, GARBAGE)** — 실패 시 반환 스프라이트

### 조건부 노출
- **type=INGREDIENT**만 Shop·Inventory·저장소 렌더 대상
- **type=PROCESSING**은 저장소·도시락 진입 불가 (중간재)
- **type=MAIN/SIDE**는 도구·인벤토리 아님, BentoModel에만 진입 가능
- **RecipeBook에 표시되는 요리** — `UnlockedFoodService.IsUnlocked(foodId)` 여부에 따라 카드 활성/비활성
- **BentoSelection에 표시되는 요리** — 언락된 MAIN/SIDE만 선택 가능

### 실 데이터 상세
- 완전한 66개 목록: [`../content/foods.md`](../content/foods.md)

---

## 2. IngredientData (35개) — 원재료 실물

### 정적 사양 요약
- 필드: `id`, `displayName`, `image`, `price`, `expDays`, `expReward`, `storageCategory(Refrigerator/UpperShelf/LowerShelf)`, `initialQuantity`

### 관찰 지점

| 씬/UI | 표시 지점 | 사용 필드 |
|---|---|---|
| Shop — Item 탭 | 재료 목록 행 (이름·아이콘·가격) | `displayName`, `image`, `price` |
| Cooking — 저장소 배치 위치 | 카테고리에 따라 냉장고/상단선반/하단선반 | `storageCategory` |
| Inventory HUD | 재료 재고 표시 | `displayName`, `image`, 수량 |
| Settlement — 지출 라인 | 매입 지출 요약 | (label에 재료명 부분 포함) |

### 변형/상태
- **날짜 경과에 따른 유통기한** — 표시 없음 (내부만; QA 관찰 불가) 다만 만료 시 자동 폐기
- **매입 배치 병합** — 같은 `daysRemaining`이면 한 배치로 병합 (2026-07 fix)

### 조건부 노출
- **Shop 아이템 노출** — `PurchaseService`가 `(day, phase)` 캐시. Day/페이즈별 로테이션 규칙 있음. 상세는 [`../systems/shop-system.md`](../systems/shop-system.md)
- Special 아이템 (19개) — 조건부, General 16개는 상시

### 실 데이터 상세
- 완전한 35개 목록: [`../content/ingredients.md`](../content/ingredients.md)

---

## 3. RecipeData (31개) — 레시피

### 정적 사양 요약
- 필드: `id`, `outputFood(FoodData)`, `minigameId(M001~M005)`, `inputs[]` (List<RecipeIngredient>)
- 미니게임 매핑: M001=Fire(팬), M002=Fire(냄비), M003=Sauce(볼), M004=Mix(볼), M005=Slice(도마), M006=Click(철판) 등

### 관찰 지점

| 씬/UI | 표시 지점 | 사용 필드 |
|---|---|---|
| RecipeBook (Tab) | 레시피 카드에 인풋·도구·미니게임 아이콘 | `inputs[]`, `minigameId`, `outputFood` |
| RecipeBook — 상세 카드 | DFS 트리로 하위 레시피 체인 재귀 표시 | `RecipeChain` 알고리즘 |
| Cooking — 결과 산출 | 재료 조합 매칭 → outputFood 스폰 | `RecipeLookupService.Search(inputs)` |
| Cooking — 실패 시 | 매칭 실패 → I000(음식물쓰레기) 반환 | `outputFood` 없음 |
| BentoSelection | 언락된 MAIN/SIDE 카드 표시 | `outputFood.type == MAIN or SIDE` |

### 변형/상태
- **DFS 재귀 표시** — MenuCard V2가 하위 레시피 체인을 recipe 카드 안에 nested 표시. 참조: [`MenuCardController.cs`](../../../Assets/Scripts/Unity/UI/MenuCardController.cs)
- **인풋 아이템 표시**:
  - raw INGREDIENT: 재료 이미지만
  - PROCESSING (중간재): 도구 아이콘 + 요리 이미지 오버레이

### 조건부 노출
- **RecipeBook에 표시** — `UnlockedFoodService.IsUnlocked(recipe.outputFood.id)`에 따라 카드 활성/블러
- **Day별 언락 곡선** — 상세는 [`../systems/progression-system.md`](../systems/progression-system.md)

### 실 데이터 상세
- 완전한 31개 목록 + DFS 트리: [`../content/recipes.md`](../content/recipes.md)

---

## 4. CookingToolData (5개) — 도구

### 정적 사양 요약
- 필드: `id(T001~T005)`, `cookerName`, `defaultImage`
- 도구 목록: T001 팬, T002 냄비, T003 볼, T004 도마, T005 철판

### 관찰 지점

| 씬/UI | 표시 지점 | 사용 필드 |
|---|---|---|
| Cooking — 조리대 | 5 도구 스프라이트 배치 | `defaultImage` |
| Cooking — 미니게임 UI 배경 | 도구 아이콘 표시 | `defaultImage` |
| RecipeBook — 레시피 카드 | 미니게임 섹션의 도구 아이콘 배경 | `defaultImage` |
| Shop — Tool 탭 | 도구 업그레이드 목록 | `cookerName`, `defaultImage` |

### 변형/상태
- **업그레이드 등급** — `ToolUpgradeService.GetLevel(toolId)` — 등급별 durationMultiplier / staminaCost 변화
- **미니게임 주입** — 등급값이 `CookingToolModel`에 주입되어 미니게임 파라미터에 영향

### 조건부 노출
- 초기부터 모두 노출 (숨김 없음)
- 업그레이드 여부는 Shop에서 관찰 가능

### 실 데이터 상세
- 상세: [`../systems/cooking-tools.md`](../systems/cooking-tools.md), [`../systems/upgrade-system.md`](../systems/upgrade-system.md)

---

## 5. DeliveryNpcData (10명) — NPC 프로필

### 정적 사양 요약
- 필드: `id`, `characterName`, `sprite`, `groupId`, `prerequisiteGroupId`, 초기 state, 배달 파트너, 일상 사이클 섹션 수, CasualDialogue(Any/Good/Bad)
- 대사 소스: 그룹 대사 (`Dialogue/<groupId>/`) + 일상 사이클 (`Dialogue/npc_normal/<name>/Section_*.asset`)
- 10명: 자르, 게토로, 파자마, 모아이, 스라냐, 니모, 세라프, 레데, 가브리엘, 린

### 관찰 지점

| 씬/UI | 표시 지점 | 사용 필드 |
|---|---|---|
| Mall — 씬 걸어다니는 스프라이트 | 층·위치별 NPC 배치 (레거시 CSV 좌표 참조) | `sprite` |
| Mall — 대화창 초상화 | `DialogueManager` 대화 시 표시 | `sprite`, `characterName` |
| Mall — 근접 프롬프트 | Space 상호작용 트리거 | `characterName` (필요 시) |
| Dialogue 선택지 | 배달 수락/거절 선택 | 그룹별 대사 |
| Cooking — 배달 티켓 | 6번째 슬롯(QuestSlotIndex=5) 세로 스택 | 배달 그룹 정보 |

### 변형/상태
- **NPC state 사이클** — `Orderable → Ordered → Delivering → Waiting → Completed`
- **대사 필터** — Weather(Good/Bad) 조건에 따라 CasualDialogue 다르게 뽑음 (게토로/파자마 등 일부만 반응)
- **초상화 표정** — 현 SO는 단일 sprite (기본 default). 표정 다수 정의 안 됨

### 조건부 노출
- **prerequisiteGroupId 언락 체인**:
  ```
  power_room_pair (자르+게토로) → night_market (파자마+모아이+스라냐)
    → nimo_solo → seraph_solo → lede_solo → gabriel_solo
  npc_lin (린) — 캐주얼 전용 (groupId 없음)
  ```
- 이전 그룹 `Completed` 전까지 배달 대사 대신 일상 사이클만 재생

### 실 데이터 상세
- 10명 프로필 전문: [`../content/npcs.md`](../content/npcs.md)

---

## 6. CropData (13개) — 텃밭 작물

### 정적 사양 요약
- CSV `cropData.csv`. 필드: `id`, `displayName`, `spritePath`, `spawnWeight`, `growPhaseCount`, `harvestOutputFoodId`
- Catalog: `CropSpriteCatalog` (sprite 로드)

### 관찰 지점

| 씬/UI | 표시 지점 | 사용 필드 |
|---|---|---|
| Garden — FarmTile cropSpriteRenderer | 밭 위 작물 스프라이트 | `spritePath` (성장 단계별) |
| Garden — 성장 게이지 | 성장 진행률 표시 | `growPhaseCount` 기반 |
| Garden — 작물 이름 라벨 | 작물 아래 TMP | `displayName` |
| Garden — 액션 프롬프트 | "성장 중..." / "수확" | 상태 조합 |
| Cooking — 저장소 진입 | 수확 후 결과물이 재료로 진입 | `harvestOutputFoodId` |

### 변형/상태
- **성장 단계 스프라이트** — Sprout / Growing / Ready 등 단계별 sprite 교체 (`CropSpriteCatalog`)
- **잠긴 밭** — `farmIndex >= FarmUpgrade.tile.value` → 표시만 잠금 표시

### 조건부 노출
- **spawnWeight 랜덤 심기** — 잠기지 않고 비어있으면 자동으로 가중치 랜덤 심기
- **밭 확장** — `FarmUpgradeService.tile` 값으로 활성 밭 개수 결정 (초기 3)

### 실 데이터 상세
- 완전한 13개 목록: [`../content/crops.md`](../content/crops.md)

---

## 7. DeliveryGroup (6개) — 배달 그룹

### 정적 사양 요약
- CSV `deliveryQuest.csv` + 그룹 대사 SO (`Assets/Bundles/ScriptableObjects/Dialogue/<groupId>/`)
- 그룹: `power_room_pair`, `night_market`, `nimo_solo`, `seraph_solo`, `lede_solo`, `gabriel_solo`
- 각 그룹: NPC 조합 + 의뢰 메뉴 + prerequisiteGroupId + 언락 조건

### 관찰 지점

| 씬/UI | 표시 지점 |
|---|---|
| Mall — NPC 대화 | 그룹 대사 재생 (수락/거절 분기) |
| Cooking — 배달 슬롯 | 6번째 슬롯 (QuestSlotIndex=5) 세로 스택으로 티켓 표시 |
| Cooking — 배달 성공 | 요리 서빙 후 재수주 정책에 따라 다음 스테이지 |
| Settlement — 배달 수입 | 배달 보상이 수입 라인에 라벨링됨 |
| RecipeBook | 언락된 배달 메뉴 확인 가능 (해당 시) |

### 변형/상태
- **DeliveryQuestStage 6단계**:
  1. Orderable (주문 가능)
  2. Ordered (주문 접수)
  3. Delivering (배달 중, Cooking 씬에 배달 손님 스폰)
  4. Waiting (완성 도시락 서빙 대기)
  5. Served/Failed (서빙 결과)
  6. Completed (완료 → 다음 그룹 언락)
- **재수주 정책** — 실패 시 재도전 가능 여부 (상세 [`../systems/delivery-quest-system.md`](../systems/delivery-quest-system.md))

### 조건부 노출
- **선행 그룹 완료** — `prerequisiteGroupId` 완료 전까지 배달 대사 잠금 (일상 사이클만 재생)
- **npc_lin (린)** — 그룹 없음, 순수 캐주얼 (스토리에 안 얽힘)

### 실 데이터 상세
- 6 그룹 상세: [`../content/delivery-groups.md`](../content/delivery-groups.md)

---

## 8. 정적 콘텐츠 관찰 지점 크로스 요약

콘텐츠별 렌더/표시 지점을 씬별로 뒤집은 뷰:

| 씬 | 관찰되는 콘텐츠 |
|---|---|
| Mall | NPC 스프라이트/대화창 초상화·이름 (10명), 배달 그룹 대사 (6개) |
| Shop | IngredientData 목록 (Item 탭 최대 35), CookingToolData 5, 업그레이드 CSV 값 |
| Cooking | FoodData 대부분 (raw/toolVariants/bentoVariants/pieceSprite), CookingToolData 5, DeliveryNpcData sprite (배달 손님), RecipeData 매칭 결과 |
| CookingTutorial | Cooking과 동일 세트 (mock 스폰 손님 포함) |
| Garden | CropData 13 (성장 스프라이트 단계별) |
| Settlement | 라벨 텍스트에 배달/수입 카테고리 요약 |
| RecipeBook (overlay) | FoodData image, RecipeData inputs, CookingToolData 아이콘 |
| BentoSelection (overlay) | FoodData MAIN/SIDE bentoVariants + 언락 여부 |

---

## 9. 알려진 관찰 이슈 (dev-notes 후보)

- 일부 PROCESSING FoodData의 `image` 필드가 비어있을 수 있음 (`content/foods.md` 표에서 "N" 마크). 실제 게임에선 도구 위 조리 중에만 노출되므로 raw 이미지 없어도 무방하지만, 저장소에 진입할 일이 있으면 빈 스프라이트로 관찰될 수 있음.
- `NPCData.sprite`는 현재 각 NPC 1장(default)만. 표정별 초상화가 도입되면 관찰 지점 갱신 필요.
- **음식물쓰레기 (I000)** — 실패 시 쓰레기통 태그로 폐기; 인벤토리로 들어가지 않음.
- Weather 시스템은 대사 필터 외 관찰 지점 없음 (게토로/파자마 대사만 반응).

상세: [`dev-notes.md`](./dev-notes.md)

---

## 이 문서 원칙

- **QA 인풋 성격**: "어디서 관찰되는가"의 인벤토리. 콘텐츠 값 전수는 [`../content/`](../content/) 원본 참조.
- **QA 영역 침범 금지**: 관찰 결과가 맞는지 판정, 테스트 절차 설계, 우선순위 결정은 QA 몫.
- **실 코드/asset 기반**: 관찰 지점은 코드 grep 및 GDD 크로스레퍼런스로 확인. 값 세부는 원본 CSV/SO/GDD 참조.
- **값 변경 시**: 원본 CSV → 원본 GDD 갱신 → 이 문서는 관찰 지점만 유지 (값은 반복 기재하지 않음).
