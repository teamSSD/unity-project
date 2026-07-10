# Aftertaste 시스템 기능 인벤토리 (QA 인풋)

Aftertaste의 게임 시스템 16개에 대해 **관찰 가능한 기능/UI/상호작용/자동 동작/데이터/접점**을 카테고리별로 나열한 문서. QA가 이 재료를 바탕으로 테스트 케이스를 자유롭게 설계할 수 있도록, "무엇이 존재하는가"만 기록하고 "어떻게 검증해야 하는가"는 다루지 않는다.

기준 소스: `Design/gdd/systems/*.md` (실 코드 기반) + `Assets/Scripts/**` 실 코드.

목차
1. [Cooking System](#1-cooking-system)
2. [Cooking Tools](#2-cooking-tools)
3. [Customer System](#3-customer-system)
4. [Delivery Quest System](#4-delivery-quest-system)
5. [Dialogue System](#5-dialogue-system)
6. [Garden System](#6-garden-system)
7. [Mall System](#7-mall-system)
8. [Menu Compositions](#8-menu-compositions)
9. [Progression System](#9-progression-system)
10. [Save System](#10-save-system)
11. [Settlement System](#11-settlement-system)
12. [Shop System](#12-shop-system)
13. [Time & Phase System](#13-time--phase-system)
14. [Tutorial System](#14-tutorial-system)
15. [Upgrade System](#15-upgrade-system)
16. [Weather System](#16-weather-system-사장-상태)

---

## 1. Cooking System

관련 GDD: [`systems/cooking-system.md`](../systems/cooking-system.md)

### 주요 기능
- Cooking 씬 진입 시 인벤토리에서 저장소 3종(Refrigerator/UpperShelf/LowerShelf)으로 재료 자동 배치
- 재료 → 조리 도구 드롭 → 미니게임 → 요리 결과 산출 → 다른 도구/도시락으로 이송의 조리 파이프라인
- 미니게임 5종(FireMiniGame, MixMiniGame, SauceMiniGame, SliceMiniGame, GriddleMinigame) 라우팅 및 실행
- 재료 조합에서 `RecipeData` 검색 (실패 시 R000 음식물쓰레기 반환)
- 조리 결과 가격 계산(`(0.85 + weight × score)` 재료 합 + 체인 보너스)
- 도시락(Bento) 인스턴스 스폰 (BentoSet 클릭 드래그)
- 완성 도시락에 영수증 부착 → 손님 서빙 흐름
- Trashcan 태그 드롭 시 재료 폐기 + SFX
- 미니게임에 도구 업그레이드(`durationMultiplier`, `staminaCost`) 주입

### UI/시각 요소
- 저장소 3종 배치 (냉장고 그리드, 윗/아랫 찬장 수평선형)
- 5개 조리 도구 (팬 T001, 냄비 T002, 보울 T003, 도마 T004, 철판 T005)
- BentoSet (빈 도시락 스택)
- BentoPosition 슬롯 하이라이트 (점선)
- 도시락 안의 Main/Side 이미지 (`bentoVariants[0/1/2/3]` = main/left/middle/right)
- 5개 미니게임 UI 각각 (게이지, 화살표, 스택 아이콘 등)
- 재료 스프라이트 스택 렌더링 (미니게임 안, `SpriteStackRenderer`)
- 도구별 조리 결과 이미지 (`FoodData.GetImageForTool(toolId)`)
- 재료의 조각(piece) 스프라이트 (도마 절단 시)

### 상호작용
- 재료 드래그 → 조리 도구 드롭
- 조리 도구 클릭 → 미니게임 시작
- 미니게임 종료 후 도구 결과물을 다른 도구/도시락으로 드래그
- BentoSet 클릭+드래그 → 새 도시락 인스턴스 스폰
- 빈 도시락을 BentoPosition에 안착 → 재료 수용 시작
- 완성 도시락에 영수증(OrderTicket) 드래그 부착
- 재료/도구 결과물을 Trashcan에 드래그
- 미니게임별 입력: 스페이스바 프레스, 방향키, 마우스 드래그

### 자동 동작/이벤트
- Cooking 씬 진입 시 `CookingSceneManager.FillStorage()` 실행 → 카테고리별 인벤토리 스폰
- 저장소 용량은 업그레이드 값을 씬 진입 시 주입 (`InjectStorageCapacity`)
- 미니게임 종료 시 `Stats.SubStamina(upgradedStaminaCost)` 자동 소모
- 미니게임 시작 시 `UILockManager.IsLocked` 체크 (튜토리얼 등에서 차단)
- Bento에 재료 넣으면 첫 항목은 자동 Main, 이후 SIDE는 순서대로 `bentoVariants[1..3]` 자동 매핑
- BentoSet 스폰된 도시락이 안착 전 다른 위치에 놓이면 자동 파괴
- 영수증 부착 후 0.5~1.5s 랜덤 대기 → `onTake` 이벤트 자동 발화

### 관련 데이터 표시
- 조리 도구 위 재료 스택 시각화
- 요리 결과 스프라이트(도구 위)
- 미니게임 점수는 화면상 노출 없음 (내부 가격 산정에만 반영)
- 각 재료의 원가/`Price` (내부, UI 미노출)

### 시스템 접점
- 발행: 미니게임 종료 → 재료가격 반영 → Bento 부착 → 손님 lifecycle 이벤트
- 구독: OrderTicket `OnAttached` / `onTake`, Storage `OnFoodDestroyed`
- 데이터: `RecipeData`, `FoodData`, `IngredientData`, `CookingToolData`, `upgrade_tool.csv`
- 관련: cooking-tools, customer-system, menu-compositions, upgrade-system, tutorial-system(CookingTutorial 씬 mock)

---

## 2. Cooking Tools

관련 GDD: [`systems/cooking-tools.md`](../systems/cooking-tools.md)

### 주요 기능
- 5개 조리 도구 데이터 정의 (T001 팬 / T002 냄비 / T003 보울 / T004 도마 / T005 철판)
- 도구별 진입 가능 재료 판정 (`FoodData.availableTools`에 도구 id 포함 여부)
- 도구 안의 재료 최대 7개 제한
- 도구 안의 재료 중복 추가 금지 (동일 `foodData.id`)
- 도구 안에 결과물(result) 있으면 재료 추가 규칙 변경
- 도구 → 다른 도구로 결과물/재료 이송
- 도구 → 도시락으로 결과물 이송 (MAIN/SIDE type만)
- Trashcan 태그와 겹치면 재료 전체 폐기 + SFX
- 도구 업그레이드 3단계(L0/L1/L2) 적용 시 미니게임에 `ApplyUpgrade` 주입
- 도구별 미니게임 폴백 매핑(레시피에 `minigameId` 없을 때)

### UI/시각 요소
- 5개 조리 도구 스프라이트 (`CookingToolBehavior`)
- 도구 위 재료 시각 배치
- 도구 완료 결과 이미지 (`defaultImage` 또는 `FoodData.GetImageForTool`)
- 도구 클릭/드래그 상호작용 컴포넌트

### 상호작용
- 재료 → 도구 드롭 (`OnFoodDropped`)
- 도구 클릭 → `PlayMinigame()`
- 도구 위 결과물/재료를 다른 도구 위로 드래그 (`OnToolDropped`)
- 도구 위 결과물/재료를 도시락으로 드래그
- 도구 위 결과물/재료를 Trashcan으로 드래그

### 자동 동작/이벤트
- 재료 드롭 시 `IngredientPlacementRules`가 CookingTool → Bento 순으로 진입 후보 결정
- 조리 결과 존재 상태에서 도구 id가 result의 `availableTools`에 없으면 재료 추가 자동 거부
- 도구 안에서 미니게임 시작 시 `searchRecipeUsecase.Search`가 자동 실행
- 미니게임 종료 시 `Stats.SubStamina(upgradedStaminaCost)` 자동 소모

### 관련 데이터 표시
- 도구별 cookerName ("팬"/"냄비"/"보울"/"도마"/"철판") — 스프라이트/UI 라벨
- 업그레이드 레벨은 도구 자체 UI에는 노출 없음 (Shop에서만)

### 시스템 접점
- 발행: 조리 결과 → CustomerLifecycle 서빙 판정
- 구독: `RecipeDataManager`(도구 id → 미니게임 폴백 매핑), `ToolUpgradeService`
- 데이터: `Assets/Bundles/ScriptableObjects/CookingTools/*.asset`, `upgrade_tool.csv`
- 관련: cooking-system, upgrade-system, tutorial-system(스텝 4~8 도구 안내)

---

## 3. Customer System

관련 GDD: [`systems/customer-system.md`](../systems/customer-system.md)

### 주요 기능
- 페이즈별 스폰 파라미터(Morning 35s±8, Afternoon 30s±8, Evening 22s±5, Night 15s±4)로 손님 자동 스폰
- 3단계 손님 상태 flow (Ordering → Waiting → Taking → Destroy) + Exit 분기
- 손님 종류 풀 10종 CustomerData (성격 라벨/npcId/스프라이트/디스플레이 스케일/메시지 4종)
- 대기 슬롯 5개 중 최대 동시 대기 3명 (랜덤 슬롯 픽)
- 대기 위치 자연스러움을 위한 오프셋 (±0.5x, ±0.3y)
- 인내심(waitingTimer) 90초 카운트다운, 영업 종료 후 2배 가속
- 활성 손님 + 활성 배달 주문 npcId 다음 스폰 후보에서 제외
- 주문 티켓 5+1 슬롯 배치 (일반 5 + 배달 세로 스택 1)
- 배달 티켓은 정확 일치(ValidateExactMatch) 요구, 일반 티켓은 부분 매칭 허용
- MenuValidator 정확도 점수(0.0~1.0) + 등급(S/A/B/C/D/F) + 보상(골드) 산출
- 등급/정확도/보상/피드백 메시지를 ValidationFeedbackUI에 표시
- 세션 통계(`CustomerSessionStats`): TotalOrders/PerfectOrders/TotalEarnings/Averages
- EndEarly (조기 종료) — 활성 손님/티켓 즉시 파괴 + `OnGameEnd`

### UI/시각 요소
- Ordering/Waiting/Taking 각 프리팹 (별도 스프라이트 세트)
- Waiting 손님 머리 위 인내심 게이지(GaugeUI, y=426, x-65 오프셋, scale 0.6)
- 손님 초상화 + 스피치 버블 메시지 (Ordering/Satisfied/Unsatisfied/Escape)
- 주문 티켓 UI (도시락 이미지 + 슬롯 인덱스에 따라 위치 결정)
- 배달 티켓 세로 스택 (index 5)
- ValidationFeedbackUI (등급/정확도/보상/피드백)
- Ordering 손님 위치 (`3.02, -0.26`), Exit 위치 (`-9.89, -0.85`)
- Ordering/Taking scale = 0.45 × displayScale, Waiting scale = 0.297 × displayScale

### 상호작용
- Ordering 손님 클릭 → 주문 받기 (Waiting으로 전환)
- 완성 도시락 위에 영수증 드래그 → 서빙 확정
- 배달 티켓의 경우 정확 일치 조합만 부착 성공

### 자동 동작/이벤트
- CustomerSpawner: `GameRandom.Normal(baseInterval, variance)` 정규분포로 다음 스폰 시간 결정
- 첫 손님: 페이즈 시작 후 약 Normal(3s, σ=1s)에 스폰
- 스폰 게이트: 카운터에 이미 orderingCustomer 존재하거나 대기석 3자리 참 시 skip
- 매 프레임 인내심 게이지 감소 (Time.deltaTime)
- 영업 종료 시각 도달 시 `localTimerScale = 2f` 자동 적용
- 90초 초과 → `WaitingCustomer.OnExit()` → 자동 exit 애니 + Destroy
- 서빙 성공 시 `waitingCustomer.StopTimer()` 자동 호출
- 부착된 티켓은 도시락 자식으로 reparent, scale 0.7 축소, sortingOrder +100
- 부착 후 0.5~1.5s 랜덤 대기 → 서빙 확정 자동 트리거
- 성공 시 `Settlement.AddIncome(phaseLabel, TotalEarnings)` 자동 호출
- 배달 완료 시 `MarkCookedWithPrice(questId, reward)` 자동 호출
- 조기 종료 시 활성 티켓 소진 후 `OnGameEnd` 자동 발화

### 관련 데이터 표시
- 세션 매출 (`sessionStats.TotalEarnings`) — 내부 값, Settlement로 전달
- 손님 인내심 게이지 (시간 대비)
- 손님 만족/불만 메시지 (텍스트 버블)
- MenuValidator 피드백 문자열 ("메인 메뉴가 틀렸습니다!" 등)

### 시스템 접점
- 발행: `OnGameEnd`, `Settlement.AddIncome`, `Order.MarkCookedWithPrice`
- 구독: `OnPhaseChanged` (스폰 파라미터 갱신), `TimeManager.OnTimeEnd`, `OrderTicketModel.OnAttached`/`onTake`
- 데이터: `Assets/Bundles/ScriptableObjects/Customer/*.asset`, `deliveryQuest.csv`, `deliveryNPC.csv`
- 관련: cooking-system, menu-compositions, settlement-system, delivery-quest-system, time-phase-system

---

## 4. Delivery Quest System

관련 GDD: [`systems/delivery-quest-system.md`](../systems/delivery-quest-system.md)

### 주요 기능
- 6단계 상태머신 (FirstMeet → QuestStart → Ordering → OrderEnd → Completed → Normal)
- groupId 기반 진행도 저장 (`MallPersistent.questGroupIds/questStages` parallel list)
- NPC별 5개 대사 슬롯 (firstMeet/normal/questStart/ordering/orderEnd)
- QuestStart의 `accept` 선택 시 주문 생성 + 메뉴 레시피 해금
- 배달 티켓을 Cooking 씬에서 6번 슬롯 세로 스택으로 표시
- 배달 완성 시 MenuValidator 공식으로 보상 계산 (`main + sides` 반영)
- Mall 재대화 시 Cooked 감지 → ConsumeBento + OrderEnd 대사로 자동 전환
- 재수주 방지 (`questId = quest_<groupId>` 형식 고정)
- Completed 그룹은 Normal 사이클만 재생 (재수주 없음)
- prerequisiteGroupId 잠금 조건: 선행 그룹 Completed 아니면 일상 대화만 재생
- DeliveryNpcState 3종 (Orderable=흰색 / WaitingReceipt=노란색 / Completed=회색)
- 개발자 리셋 (`DeliveryNpcDialogueInteraction.ResetAll()`)

### UI/시각 요소
- Mall 씬에 배치된 8명의 DeliveryNPC 스프라이트
- 상태별 색상 tint (Orderable white / WaitingReceipt yellow / Completed gray)
- 주문 수락 시 씬에 소환되는 영수증(receipt) 프리팹
- Cooking 씬 6번 슬롯의 세로 스택 배달 티켓들
- 대화 UI 안 초상화 + 화자 이름 + 텍스트 + 선택지 버튼

### 상호작용
- NPC 근처에서 Space → 대사 시작
- 대사 진행: Space/패널 클릭/꾹 누름(자동 진행)
- 선택지 (QuestStart): "수락(accept)" / "다음에(defer)" 클릭
- 배달 티켓을 도시락에 드래그 부착 (정확 일치 요구)

### 자동 동작/이벤트
- 최초 대화 시 stage 자동 등록 (미등록 → FirstMeet 반환)
- FirstMeet 대화 종료 후 자동으로 QuestStart 전이
- `accept` 선택 시 자동으로 `CreateQuestOrder`, 레시피 해금, 영수증 인스턴스화
- Cooking 씬 진입 시 `OrderService.GetOrders()`에서 Ordered 주문 자동으로 티켓 변환
- 티켓 부착 시 `MenuValidator.CalculateReward` 자동 실행 → `MarkCookedWithPrice`
- 재대화 시 Cooked 감지 자동 훅 → ConsumeBento + 상태 자동 OrderEnd
- OrderEnd 대화 종료 시 자동 Completed 전이
- 활성 손님 스폰 시 활성 배달 npcId 자동 제외

### 관련 데이터 표시
- NPC characterName (대사창)
- 선택지 라벨
- 주문 메뉴 내용 (도시락 이미지)
- `cookedPrice` — 내부값, UI 미노출

### 시스템 접점
- 발행: `MarkCookedWithPrice`, `ConsumeBento`, `UnlockMenuRecipes`, stage 저장
- 구독: `OnDialogueEnded(resultTag)`
- 데이터: `deliveryQuest.csv`, `deliveryNPC.csv`, `Assets/Bundles/ScriptableObjects/Dialogue/**`, `DialogueConfigCatalogSO`
- 관련: dialogue-system, customer-system, mall-system, cooking-system, progression-system

---

## 5. Dialogue System

관련 GDD: [`systems/dialogue-system.md`](../systems/dialogue-system.md)

### 주요 기능
- 단일 `DialogueManager`가 모든 NPC 대사 재생
- DialogueSO(SO 자산) + `npcCasualDialogue.csv`(런타임 파싱) 두 소스 지원
- 순차 entries 진행 + 각 entry에 선택지 옵션
- 선택지 `responses` 분기 후 메인 entries로 자동 복귀
- 선택 결과 `resultTag`를 `OnDialogueEnded` 이벤트로 전달
- 타이핑 애니메이션 (0.04s/글자, 3글자마다 blip SFX)
- Space 꾹 누름 (>0.3s) 자동 진행 (0.15s 간격)
- 초상화 처리 ("플레이어" 이름은 공백으로 렌더, 로직 filter는 유지)
- 스피커에 매칭 스프라이트 없으면 회색 tint (0.5, 0.5, 0.5)
- 그룹 대화용 초상화 dictionary (같은 groupId NPC 모두 수집)
- 캐주얼 NPC 대사: 날씨 조건(`Any`/`Good`/`Bad`) 필터
- NPC별 Normal 사이클(NpcNormalDialogueCatalogSO/Service)의 인덱스 진행
- 1프레임 지연 unlock (같은 프레임 Space 재입력 방어)

### UI/시각 요소
- Dialogue Panel (자체 Canvas, ScreenSpaceOverlay, sortingOrder=50)
- 화자 이름 텍스트
- 본문 텍스트 (타이핑)
- 선택지 버튼(들) — 삼각형 마커 ▶ + 라벨, HorizontalLayoutGroup
- 초상화 이미지 (portraitContainer 안)
- 패널 자체가 Button (클릭도 진행)

### 상호작용
- Space 키 → 진행 (타이핑 스킵 → 다음 entry)
- Space 꾹 누름 → 자동 진행
- 패널 클릭 → 진행 (동일 로직)
- 선택지 버튼 클릭 → `OnChoiceSelected(choice)` + resultTag 저장
- Esc는 dialogue-system 자체에는 없음 (다른 UI에서 사용)

### 자동 동작/이벤트
- `StartDialogue` 호출 시 자체 Canvas 스폰 → 패널 활성화 → SoundManager.PlayUIBook
- 타이핑 시 매 글자마다 delay + blip SFX
- 첫 프레임 `justStarted` 플래그로 입력 무시 (진입 keydown 재소비 방지)
- 마지막 entry 도달 시 자동 `EndDialogue()`
- `EndDialogue` 시 패널 비활성, 1프레임 지연 후 unlock, `OnDialogueEnded(resultTag)` 발화
- `RegisterButtons`로 선택지 hover/click SFX 자동 wiring
- `CasualDialogueProvider.GetRandomDialogue` — 날씨 조건 자동 필터

### 관련 데이터 표시
- 화자 이름
- 대사 텍스트
- 선택지 라벨
- 초상화 스프라이트

### 시스템 접점
- 발행: `OnDialogueEnded(resultTag)`
- 구독: (외부에서 호출), Weather.IsBadWeather (필터에서)
- 데이터: `Assets/Bundles/ScriptableObjects/Dialogue/**/*.asset`, `npcCasualDialogue.csv`, `dialog.csv`(레거시), `NpcNormalDialogueCatalog.asset`, `DialoguePanel.prefab`, `DialoguePanelRefs`
- 관련: delivery-quest-system, mall-system, weather-system, tutorial-system(안내 파트)

---

## 6. Garden System

관련 GDD: [`systems/garden-system.md`](../systems/garden-system.md)

### 주요 기능
- Garden 씬(`Garden.unity`) 7개 FarmTile 프리팹 인스턴스 (farmIndex 0~6)
- Farm 컴포넌트 + FarmTile POCO 페어 구조
- 자동 심기 (씬 진입 시 빈 타일 && !IsLocked면 랜덤 작물 자동 심음)
- 페이즈 기반 성장 (매 phase tick 시 성장 계산)
- 성장 완료 시 Space로 수확 → 인벤토리에 harvestCount만큼 자동 추가
- 수확 후 자동 재파종 (crop=null → 랜덤 픽)
- 잠긴 타일은 farmIndex >= `tile` upgrade value 기준
- `timeReduction` 업그레이드로 requiredPhases 감소
- `harvestCount` 업그레이드로 수확량 증가
- 씬 이탈 시 자동 저장 (`FarmTileSaveData { cropId, plantedPhase }`)
- 백그라운드 성장 (Garden 씬 안 있어도 `CurrentPhaseIndex` 계속 증가)
- Mall 씬에서 `FarmPathInteraction`으로 진입 / `ExitToMall`로 복귀

### UI/시각 요소
- 밭 타일 스프라이트 × 7
- 작물 스프라이트 (`cropSpriteRenderer`)
- 성장 게이지 UI (`growthGauge` + `gaugeCanvas`)
- 작물 이름 라벨 (`cropNameLabel` — crop sprite 하단)
- 상호작용 프롬프트 텍스트 ("잠겨 있음"/"(스페이스바로 수확)"/"(스페이스바로 심기)"/"성장 중...")
- 카메라 (CameraFollow, 배경 경계 clamp)
- ExitToMall 오브젝트

### 상호작용
- 밭 타일 근처에서 Space → 수확 (성장 완료 시) 또는 심기 (실질적으로 자동)
- ExitToMall Space → Mall 씬 복귀

### 자동 동작/이벤트
- 씬 진입 시 `GardenPersistent.tiles`에서 저장 상태 복원
- 빈 타일이면 자동 심기 (CropCatalog.GetRandomCropByWeight)
- `Progress.OnPhaseChanged` 구독 → 매 페이즈 시각 즉시 성장 게이지 갱신
- 수확 후 자동 재파종
- 씬 이탈 시 (`Farm.OnDestroy`) 저장 데이터 자동 저장
- 카메라 자동 clamp

### 관련 데이터 표시
- 작물 이름
- 성장 게이지 진행률
- 수확 가능/성장 중/잠김 상태 프롬프트

### 시스템 접점
- 발행: `Inventory.AddHarvestedCrop(cropId, harvestCount)`
- 구독: `Progress.OnPhaseChanged`, `TimePhaseProvider.CurrentPhaseIndex`
- 데이터: `crop_data.csv`, `Assets/Bundles/Catalogs/CropSpriteCatalog.asset`, `upgrade_farm.csv`, `GardenPersistent.tiles[8]`
- 관련: mall-system(진입), time-phase-system(성장 tick), upgrade-system(farm), save-system(FarmTileSaveData)

---

## 7. Mall System

관련 GDD: [`systems/mall-system.md`](../systems/mall-system.md)

### 주요 기능
- Mall 씬(`Mall.unity`)이 세계 허브 역할 — Cooking/Shop/Garden/Settlement 왕복 진입점
- 상가 파노라마 배경 + 층 구조 + NPC 배치 + 상호작용 지점
- 페이즈별 UI 자동 표시 로직 (`MallSceneController.Start`)
- `PhaseActionSelector` (Work/Rest/Shopping) 페이즈 진입 시 자동 활성
- Cooking → Mall 복귀 시 자동 표시, Shop/Garden → Mall 복귀 시 표시 안 함
- GoHome 인터랙션 — 페이즈에 따라 분기 (Preparation → 메뉴 선택 모달 / Afternoon 튜토리얼 시 Closing → Settlement / 그 외 → 확인 모달)
- 층 이동은 StairsInteraction의 2-phase 코루틴 이동 (씬 전환 아님)
- 파노라마 카메라 (CameraFollow의 boundsSource clamp)
- Delivery NPC 8명 배치 + 상태별 인터랙션 스위칭 (Orderable/WaitingReceipt/Completed)
- 캐주얼 NPC (groupId 빈값) — CasualNpcInteraction으로 일상 대화
- MallReturnPosition — 이탈 전 위치 저장하여 복귀 시 override

### UI/시각 요소
- 상가 파노라마 배경 SpriteRenderer
- 8명 Delivery NPC 스프라이트 (자르/게토로/스라냐/파자마/모아이/니모/레데/가브리엘)
- 상태별 tint (white/yellow/gray)
- `PhaseActionSelector` 3택 UI (영업/휴식/상가 이동)
- BentoSelection 모달 (Preparation에서 GoHome 시)
- 확인 모달 ("이 페이즈를 마치시겠습니까?" / "하루를 마치시겠습니까?")
- Blackout 페이드 (Rest 페이즈 전환)
- 계단 오르내림 애니 (SpriteRenderer flipX)
- `playerStore`, `ExitToFarm`, 상점 카운터 위 상호작용 프롬프트

### 상호작용
- A/D 또는 ←/→ 이동
- Space: 각 상호작용 지점(playerStore, ExitToFarm, 상점 카운터, 계단, NPC)
- `playerStore` Space → GoHome 흐름
- `ExitToFarm` Space → Garden 씬 로드
- 상점 카운터 Space → `ShopUIAdapter.OpenShop(tab)` (오버레이)
- 계단 Space → 짝 계단으로 이동
- NPC Space → 대화 시작
- Esc → PhaseActionSelector Hide, 확인 모달 취소 등

### 자동 동작/이벤트
- 씬 진입 시 EventSystem nav 이벤트 차단 (Space 재트리거 방어)
- MallReturnPosition 있으면 플레이어 위치 override + 카메라 스냅
- BentoSelection / PhaseActionSelector prefab 인스턴스화 (Hide 상태로 대기)
- `Progress.OnPhaseChanged` 구독 (체류 중 phase 전환 대응)
- 계단 이동 중 `UILockManager.Lock(Loading)`
- Rest 선택 시 Blackout 감싸서 페이즈 전환, Night는 즉시 Settlement 로드 (재귀 락 회피)
- 튜토리얼 훅 자동 실행 (WelcomeAtSpawn / PhaseSelectAfternoon)

### 관련 데이터 표시
- NPC characterName (대화창)
- 페이즈별 UI 자동 분기
- 상호작용 프롬프트 텍스트

### 시스템 접점
- 발행: 씬 전환(SceneLoader), MallReturnPosition 저장
- 구독: `Progress.OnPhaseChanged`
- 데이터: `deliveryNPC.csv`, `npcCasualDialogue.csv`
- 관련: cooking-system, shop-system, garden-system, settlement-system, delivery-quest-system, dialogue-system, tutorial-system, time-phase-system

---

## 8. Menu Compositions

관련 GDD: [`systems/menu-compositions.md`](../systems/menu-compositions.md)

### 주요 기능
- 하루 3개 도시락 슬롯 편성 (`MenuSelectionService.menuSelections[3]`, "도시락 1/2/3")
- 각 슬롯 = Main 1개(필수, MAIN type) + Side 0~3개(SIDE type)
- Main 없이 Side만 있는 슬롯 = 성립 불가 → Confirm 시 경고 팝업
- 잠금 해제된 요리만 후보로 표시 (`IUnlockedFoodProvider`)
- Cooking 씬 진입 시 `GetAllMenusAsSchema` → CustomerManager 손님 주문 소스
- `CustomerManager.PickRandomMenu` — 편성된 도시락 중 uniform random pick, 주문번호 증가
- 배달 퀘스트는 별도 MenuSchema 사용 (`night_market`은 mainMenus 2개인 예외)
- 도시락 물리 슬롯 (Bento 인스턴스) `maxFoodSlots = 4`
- 첫 넣은 항목이 자동 Main 취급 (관례, 물리 규칙 아님)
- 편성 저장 형식: `"mainId|side1,side2,..."` (`RecipeBookSaveData`)
- MenuValidator 정확도 점수 & 보상 산식은 customer-system과 공유

### UI/시각 요소
- BentoSelectionController UI (도시락 편성 UI)
- 도시락 슬롯 3개 표시
- 메인/사이드 선택 카드들 (잠금 해제된 것만)
- 확인 버튼 / 뒤로가기 버튼

### 상호작용
- 슬롯 선택 → 메인/사이드 각각 선택
- 메뉴 카드 클릭 → 선택/취소 토글
- 확인 버튼 → GetInvalidSlotIndices 검증 → 성립 시 확정 + Progress.PassPhase()
- 뒤로가기 → Mall 복귀

### 자동 동작/이벤트
- Preparation 시 `MenuSelection.ClearAllMenus()` 자동 리셋
- Confirm 시 자동 검증 (Main 없는 Side 슬롯 있으면 경고)
- Confirm 성공 시 자동 Progress.PassPhase() + Cooking 씬 로드 (튜토리얼 시 CookingTutorial)
- Cooking 씬 CustomerManager.Awake에서 `HasAnySelection` false면 씬 진입 자체 blocked
- `RecipeBookMenuProvider`가 편성 없을 시 fallback으로 카탈로그 첫 FoodData를 디버그 메뉴로 사용

### 관련 데이터 표시
- 슬롯별 도시락 이름 ("도시락 1/2/3")
- Main 이미지 / Side 이미지들
- 잠금 여부 (잠긴 것은 표시/선택 불가)

### 시스템 접점
- 발행: `Progress.PassPhase()`, `GetAllMenusAsSchema` → CustomerManager
- 구독: `IUnlockedFoodProvider` (해금 상태)
- 데이터: `MenuSelection`, `MenuSchema`, `RecipeBookSaveData`, `deliveryQuest.csv`
- 관련: cooking-system, customer-system, progression-system, save-system, tutorial-system(스텝 2), mall-system(GoHome Preparation)

---

## 9. Progression System

관련 GDD: [`systems/progression-system.md`](../systems/progression-system.md)

### 주요 기능
- 시작 자산: money 12000G / stamina 100 / time 05:00 / Day 0 / Phase Preparation
- 시작 인벤토리 9종 × 3개 (I007/I008/I009/I010/I017/I019/I020/I026/I027)
- 기본 해금 메뉴 4종 (I044 기계장 고기정식, I060 옥상 오믈렛, I046 루미 젤리, I062 환기구 연어구이)
- 배달 퀘스트 6그룹의 accept 시 메뉴 메인+사이드 자동 해금
- 배달 퀘스트 해금 순서 (`deliveryQuest.csv`의 prerequisiteGroupId)
- Day/Phase 진행 서비스 (`ProgressService`)
- Day 전환 시 관리비 -1000G 자동 차감
- Day 전환 시 Stamina 100 자동 복구, Inventory 유통기한 감소, Weather 갱신, Settlement Reset
- `_cumulativePhaseIndex` — Day * 5 + Phase 절대 카운터
- Save는 `PassDay`에서만 발생 (Settlement 씬 진입 프레임 다음)
- 씬 흐름: Boot → Managers → GameStart → Mall ⇄ (Cooking/Garden/Shop) → Settlement → Mall
- 스태미나 0 → `Die()` → PassDay (명시적 게임오버 없음)

### UI/시각 요소
- 진행도 관련 전용 UI는 없음 (각 시스템 UI가 표현)
- Day 표시 (Settlement 씬 "N일차 정산")
- Stats(money/stamina/time) 각 씬 상단 HUD

### 상호작용
- 직접 상호작용은 없음 — 각 시스템의 상호작용을 통해 진행

### 자동 동작/이벤트
- NewGame: `GameStart.ApplyNewGameDefaults()` 실행
- Continue: `ProcessContinue` → SaveManager.LoadAll → 각 서비스 ApplySaveData
- `PassPhase`: Preparation → Morning → Afternoon → Evening → Night → Settlement 자동 전이
- `PassDay`: 관리비 차감, Day++, Phase=Preparation, Stamina=100, Inventory.AdvanceDay, Weather.UpdateWeather, Settlement.Reset, SaveAll
- `OnPhaseChanged(PhaseType)` 이벤트 발화
- `UnlockDefaultRecipes()` — NewGame 시 자동

### 관련 데이터 표시
- Day (Settlement 헤더에 표시)
- Phase (게임 로직 내 참조, UI에는 시각 요소로 반영)
- money/stamina/time (HUD)

### 시스템 접점
- 발행: `OnPhaseChanged`, `SaveManager.SaveAll` 트리거, `UnlockedFood.UnlockDefaultRecipes`
- 구독: (전역, 대부분의 시스템이 구독)
- 데이터: `PhaseData`, `BasicStats`, `UnlockedRecipesSaveData`, `TutorialSaveData`, `deliveryQuest.csv`
- 관련: 모든 시스템 (Day/Phase 카운터가 밴드폭)

---

## 10. Save System

관련 GDD: [`systems/save-system.md`](../systems/save-system.md)

### 주요 기능
- 단일 파일 `gamedata.json`에 모든 도메인 상태 저장 (13개 슬롯)
- 5-파일 legacy 자동 마이그레이션 (`MigrateLegacyIfNeeded`)
- Save adapter 패턴 (Garden/Shop/Mall 각각 Capture/Apply)
- 저장 시점: `ProgressService.PassDay()` 안 (Settlement 씬 진입 프레임 다음)
- Load 시점: `GameStart.ProcessContinue()` (Continue 버튼 클릭 시)
- WebGL은 `FS.syncfs`로 IndexedDB 강제 flush
- `HasSaveData()` → Continue 버튼 활성 여부 판정
- Piggyback 분리: `PhaseData.UnlockedRecipes/SelectedMenus`는 legacy fallback으로만 사용
- 파일 corrupt 시 default 인스턴스로 자동 회복
- Tutorial state는 별도 슬롯 (`GameSaveData.tutorial`)

### UI/시각 요소
- Save 관련 UI는 Settlement의 "저장 중..." → "아무 키나 눌러서 계속" 텍스트만
- Continue 버튼 (GameStart 씬)

### 상호작용
- Continue 버튼 클릭 → LoadAll
- New Game 버튼 클릭 → ApplyNewGameDefaults + SaveAll
- Settlement 씬에서 아무 키 → Mall 로드 (저장 이후)

### 자동 동작/이벤트
- `PassDay` 안에서 자동 `SaveManager.SaveAll()`
- `NewGame` 시 초기값 자동 저장
- Continue 진입 시 자동 legacy migration 검사
- WebGL 저장 완료 시 자동 `SyncFiles`
- Load 시 `unlockedRecipes` 비어있으면 자동으로 `PhaseData.UnlockedRecipes`(legacy) fallback
- Load 시 `recipeBook` 비어있으면 자동으로 `PhaseData.SelectedMenus`(legacy) fallback

### 관련 데이터 표시
- Settlement 씬 하단 "저장 중..." → "아무 키나 눌러서 계속" 텍스트

### 시스템 접점
- 발행: 파일 저장 (persistentDataPath/saves/gamedata.json)
- 구독: `ProgressService.PassDay` 트리거
- 데이터: `GameSaveData` (13개 슬롯), legacy `progress.json`/`stats.json`/`inventory.json`/`orders.json`/`deliveryQuest.json`
- 관련: 모든 시스템 (13 슬롯 각각 대응)

---

## 11. Settlement System

관련 GDD: [`systems/settlement-system.md`](../systems/settlement-system.md)

### 주요 기능
- Settlement 씬에서 일일 정산 UI 표시
- 수입/지출 카테고리별 항목 표시
- 카테고리 라벨 자동 매핑 (Phase → "아침 영업/점심 영업/저녁 영업/야간 영업")
- 지출 카테고리 3종: "재료 구매" / "업그레이드" / "관리비"
- 관리비 1000G — Settlement UI에 자동 추가 + `PassDay` 시 실차감
- Balance 라인 (`{finalMoney}G ({sign}{netChange})`)
- Reset(currentMoney) — 다음 하루 시작 시 카테고리 클리어
- 씬 진입 프레임 다음에 `PassDay` 자동 실행 → 저장
- 아무 키 입력 → Mall 씬 로드
- 파산 로직 없음 (money 0으로 clamp)
- 배달 수익은 현재 Settlement에 미기록 (Stats.money만 반영)

### UI/시각 요소
- Day 헤더 (`"{Day}일차 정산"`)
- Income 섹션 (라인 아이템들 + 총계 `+{total}G`)
- Expense 섹션 (라인 아이템들 + 관리비 라인 + 총계 `-{total}G`)
- Balance 라인 (최종 money + net change)
- SettlementLineItemUI prefab (라벨 + 금액 + ± 기호)
- saveStatusText ("저장 중..." → "아무 키나 눌러서 계속")

### 상호작용
- 씬 진입 후 저장 완료되면 아무 키 → Mall 로드

### 자동 동작/이벤트
- 씬 진입 즉시 `BuildUI` (Income/Expense/Balance 렌더)
- 관리비 라인 자동 추가
- `SaveRoutineAsync` — 씬 진입 프레임+1에 `PassDay` 자동 호출
- `PassDay` 내부: 관리비 차감, Day++, Phase Preparation, Stamina 100, Inventory.AdvanceDay, Weather.UpdateWeather, Settlement.Reset, SaveAll
- 저장 완료 후 `waitingForInput = true`, saveStatusText 업데이트

### 관련 데이터 표시
- Day (헤더)
- 각 라인 라벨 + 금액
- 총 수입 / 총 지출
- 최종 잔액 + net delta

### 시스템 접점
- 발행: `PassDay` 트리거, Mall 씬 로드
- 구독: `AddIncome`/`AddExpense` (외부 서비스로부터)
- 데이터: `incomeMap`/`expenseMap` (POCO Dictionary), `ManagementFee = 1000`
- 관련: progression-system, customer-system(매출), shop-system(재료/업그레이드 지출), upgrade-system, time-phase-system

---

## 12. Shop System

관련 GDD: [`systems/shop-system.md`](../systems/shop-system.md)

### 주요 기능
- Shop 씬 진입 or Mall 오버레이 UI 진입 두 가지
- `ShopUIAdapter` DontDestroyOnLoad singleton, `ShopBook.prefab` 재사용
- 4개 탭 (Item / Tool / Storage / Farm)
- 재료 매입 (`PurchaseService`)
- 3종 업그레이드 서비스 (Tool / Storage / Farm) 공통 패턴
- ProductType 2종: General(무제한 재고) / Special(한정 재고 + 페이즈별 라인업)
- Special은 페이즈마다 새 4개 랜덤 픽 + 페이즈별 구매수 리셋
- 재현성 RNG: `GameRandom.PhaseRandom(day, phase)` — 같은 day/phase면 동일 라인업
- 인벤토리 수용량 체크 (`CanAcceptType`) — 카테고리별 슬롯 상한
- Purchase 트랜잭션: money 차감 → Expense.Add("재료 구매") → Inventory.AddFood → 페이즈 구매수 누적
- Upgrade 트랜잭션: money 차감 → Expense.Add("업그레이드") → SetLevel

### UI/시각 요소
- ShopBook UI (책 형태)
- 4개 탭 북마크 (선택된 탭 105px, 나머지 70px)
- Item 탭: 재료 리스트 (General 16종 + Special 4종 랜덤)
- Tool 탭: 5개 도구 리스트 (T001~T005)
- Storage 탭: 3종 창고 (냉장고/윗찬장/아랫찬장)
- Farm 탭: 3종 텃밭 (타일/수확 시간/수확량)
- 상세 패널 (선택된 아이템 이름, 가격, 재고, 수량 조정)
- Buy/Upgrade 버튼
- 헤더 라벨 (탭별)
- 카테고리 라벨 ("냉장고 확장"/"윗 찬장 확장"/"아랫 찬장 확장"/"농장 확장"/"수확 시간 감소"/"수확량 증가")
- 재고 표시 (Special: "재고 N", General: 미표시)
- 총액 실시간 표시 (`qty * unitPrice`)
- MAX 도달 시 버튼 비활성

### 상호작용
- Mall 카운터 Space → OpenShop
- 탭 북마크 클릭 → SwitchTab
- 아이템 클릭 → 상세 패널 표시
- +/- 버튼 → qty 조정 (0 ~ remainingStock)
- Buy 버튼 클릭 → TryBuy
- Upgrade 버튼 클릭 → TryUpgrade
- Esc → CloseShop

### 자동 동작/이벤트
- OpenShop 시 `UILockManager.Lock(Shop)` — 중복 오픈 차단
- OpenShop 시 `SoundManager.PlayUIBook`
- 페이즈 변경 시 캐시 키 `(day << 8) | phaseIndex` 자동 갱신 → 라인업 재빌드 + `_phasePurchased.Clear()`
- General은 항상 `int.MaxValue` 반환
- Special은 `stock - purchasedThisPhase` 계산
- Buy 성공 시 페이즈 구매수 자동 누적
- Upgrade 성공 시 자동 SetLevel + 다음 레벨 데이터 재계산

### 관련 데이터 표시
- 각 아이템 단가 (`FoodData.ingredient.defaultPrice`)
- Special 잔여 재고
- 현재/다음 레벨 값 (예: "Lv.1 → Lv.2", "7칸 → 10칸", "타일 3개→4개")
- 업그레이드 코스트

### 시스템 접점
- 발행: `Money.TrySpend`, `Expense.Add`, `Inventory.AddFood`, `SetLevel`
- 구독: (없음 - 사용자 클릭이 트리거)
- 데이터: `FoodShopConfig.asset`, `foodStore.csv`(참조), `upgrade_tool.csv`, `upgrade_storage.csv`, `upgrade_farm.csv`
- 관련: mall-system(진입), settlement-system(지출 기록), upgrade-system, cooking-system(저장소 용량 주입), garden-system(farm 업그레이드), progression-system(총 코스트)

---

## 13. Time & Phase System

관련 GDD: [`systems/time-phase-system.md`](../systems/time-phase-system.md)

### 주요 기능
- PhaseType enum 5종 (Preparation/Morning/Afternoon/Evening/Night)
- 페이즈별 시작/종료 시각 (Preparation 05:00~07:00, Morning 07:00~12:00, Afternoon 12:00~17:00, Evening 17:00~22:00, Night 22:00~익일 04:00)
- 실시간 → 게임 시간 변환 (`gameTimeScale = 120f`, 실시간 0.5s = 게임 1분)
- Cooking 씬 영업 시간창 (`startHour=11 ~ endHour=15`, SerializeField)
- 영업 종료 트리거 (`CheckBreakPoint` — endTime 도달 시 `PauseTime` + `OnTimeEnd`)
- 로컬 타이머 (`defaultCustomerWaitTime = 90f`) — IsPaused와 무관하게 갱신
- `closedLocalTimerScale = 2f` — 영업 종료 후 손님 인내심 2배 가속
- Ticking SFX — 매 분(OnTimeChanged) 재생
- `_cumulativePhaseIndex` = Day * 5 + Phase (crop 성장 관측)
- PassPhase — Night → Settlement 씬 자동 로드, 그 외 → phase++ + SetPhaseTime + OnPhaseChanged
- PassDay — 관리비 -1000G, Day++, Phase=Preparation, Stamina=100, 저장

### UI/시각 요소
- ClockUI: 시침(hourHand) + fill 이미지 (720분 = 12h 기준 정규화)
- 시침 lerp 회전, fill lerp 감소
- Cooking 씬 상단 시계 (startHour 11:00 ~ endHour 15:00 fill)
- Mall/Settlement 등에서는 phase range 기준 fill

### 상호작용
- 시간/페이즈 직접 조작 UI 없음 (다른 시스템 통해 진행)

### 자동 동작/이벤트
- TimeManager Update: gameTimer 누적 → `stats.AddTime(0, 1)` per game minute
- Stats.OnTimeChanged 이벤트 발화
- `PlayTickingSfx` — 매 분 tick 시 SFX 재생
- `CheckBreakPoint` — endTime 도달 시 자동 PauseTime + OnTimeEnd 발화
- OnTimeEnd → CustomerManager.OnTimeEnd (스폰 종료)
- 로컬 타이머 자동 tick + onComplete 콜백
- Progress.OnPhaseChanged → 각 시스템(Garden, Mall, etc.) 자동 반응
- Settlement 씬 자동 로드 (Night 종료 시)
- `GameRandom.InitDay(day)` PassDay 안에서 자동 실행

### 관련 데이터 표시
- Stats.time (현재 시각, HUD)
- ClockUI 시침/fill
- Day (Settlement 헤더)
- Phase (내부 상태, UI 시각 요소로 반영)

### 시스템 접점
- 발행: `OnTimeChanged`, `OnTimeEnd`, `OnPhaseChanged`, Scene 로드
- 구독: `Stats.OnTimeChanged` (ClockUI, TickingSfx)
- 데이터: `PhaseData`, `BasicStats.time`, `SettlementService.ManagementFee`
- 관련: customer-system(스폰/타이머), garden-system(성장), cooking-system(영업 종료), settlement-system(PassDay), progression-system, save-system

---

## 14. Tutorial System

관련 GDD: [`systems/tutorial-system.md`](../systems/tutorial-system.md)

### 주요 기능
- 스크립트형 튜토리얼 (최초 세션 1회 재생)
- 6개 스텝 asset 배치 (WelcomeAtSpawn, MenuSelection, CookingIntro, PhaseSelectAfternoon, MallCorridor, Closing)
- 각 스텝은 파트(bubble) 순차 표시
- CookingTutorial 격리 씬 (mock 손님 1명, 재료 무한 리필, 시간 정지)
- 스텝별 target 위치 지정 (`TutorialTarget` 등록 key)
- Bubble tail 방향 자동 flip (`autoFlipByScreenSide`)
- Recipe Book 상호작용 flag (forceOpen / blockOpen / dismissOnClose)
- 파트별 dismissKey 커스텀 (Space/Tab/ESC/None)
- 파트별 `allowSceneInteraction` — UILockManager 해제
- `freeCamera` — 카메라 조작 허용
- Space dismiss 파트는 좌클릭도 허용 (레시피북 카드 활성 시 무시)
- 각 스텝 dismiss 마지막에 `SaveManager.SaveAll()` 자동
- 완료 시 모든 hook 비활성 (`IsCompleted`)
- 개발자 리셋 없음 (파일 삭제 필요)

### UI/시각 요소
- TutorialBubble (VLG+CSF, tail 방향 sprite)
- Body: OptionalImage + Contents(TMP)
- Tail (root origin 지점)
- 스텝별 메시지 (한 줄 텍스트)
- optionalImage (미니게임 스냅샷 등)

### 상호작용
- Space (또는 좌클릭) → 다음 파트
- Tab (스텝 7) → 레시피북 열기 안내
- ESC (스텝 9) → 레시피북 닫기 안내
- None (씬 이벤트로만 dismiss — mock 손님 흐름)
- Skip 버튼 (CookingTutorial의 EarlyEndButton) → 튜토리얼 종료

### 자동 동작/이벤트
- MallSceneController에서 조건에 맞으면 자동 Show 호출
- CookingSceneManager 진입 시 CookingIntro 자동 트리거
- BentoSelectionController.Show에서 MenuSelection 자동 트리거
- Closing 스텝 dismiss 후 자동 `Complete()` + Settlement 씬 로드
- CookingTutorial 씬에서 message prefix 라우팅 (손님 오면/도시락 놓기/완성 요리/영수증/시간/스킵)
- 각 파트 dismiss 시 자동 SaveAll
- `EnableDynamicFollow` — target 이동 시 매 프레임 재계산
- CookingTutorial에서 재료 무한 리필 (OnFoodDestroyedForRefill)
- CookingTutorial 종료 시 Inventory.ResetToDefault (refill 오염 초기화)
- ReleaseTutorialLocks (마지막 파트 dismiss 후) → Unlock, HorizontalCameraMove 복구, TimeManager.ResumeTime

### 관련 데이터 표시
- 튜토리얼 메시지 텍스트
- optional 이미지
- 완료 여부 (내부 상태, UI 미노출)

### 시스템 접점
- 발행: `MarkShown(stepId)`, `Complete()`, `SaveAll`, `OnPartShown`, `OnRecipeBookClosed` 구독 hook, Settlement 씬 로드
- 구독: 각 씬 진입 이벤트 (MallSceneController, CookingSceneManager, BentoSelectionController)
- 데이터: `Assets/Bundles/TutorialSteps/*.asset`, `TutorialStepCatalog.asset`, `TutorialSaveData`
- 관련: mall-system, cooking-system, menu-compositions, save-system, dialogue-system(안 사용, 별도)

---

## 15. Upgrade System

관련 GDD: [`systems/upgrade-system.md`](../systems/upgrade-system.md)

### 주요 기능
- 3개 서비스 (Tool / Storage / Farm) — 공통 POCO 패턴
- CSV 기반 테이블 로드 (`upgrade_tool.csv`, `upgrade_storage.csv`, `upgrade_farm.csv`)
- Persistent 저장 (Garden/Shop)
- 공통 API: GetCurrentData, GetNextData, IsMax, TryUpgrade
- 결제 위임: `IMoneyService.TrySpend` + `IExpenseLog.Add("업그레이드")`
- Tool: 5개 도구 × 3레벨 (L0~L2), 모두 동일 값 (cost/staminaCost/durationMultiplier)
- Storage: 3종 저장소 × 4레벨 (L0~L3), 슬롯 수 증가 (냉장고 7→15, 윗찬장 3→8, 아랫찬장 4→8)
- Farm: 3종 × 5레벨 (L0~L4), tile 개수/timeReduction/harvestCount
- 런타임 적용: MiniGameManager, InventoryService.CanAcceptType, Farm.IsLocked, FarmTile.IsHarvestable, InventoryPageController 등
- Persistent 자동 부트: CSV에 있는 모든 key를 Persistent에 등록 (없으면 level 0)

### UI/시각 요소
- Shop 씬의 Tool/Storage/Farm 탭 (shop-system 참고)
- 업그레이드 상세 패널 (현재 → 다음 레벨 값 라벨)
- MAX 도달 시 버튼 비활성
- 라벨 예: "미니게임 시간 100%→80% / 스태미나 5→4", "냉장고 7칸→10칸", "타일 3개→4개"

### 상호작용
- Shop 씬에서 각 탭 클릭 → 업그레이드 상세 확인
- Upgrade 버튼 클릭 → TryUpgrade

### 자동 동작/이벤트
- Session 시작 시 CSV 로드 → `_table` 자동 구성
- Persistent 자동 부트 (없는 key는 level 0으로 추가)
- Load 시 saved level만 덮어씀
- TryUpgrade 성공 시 자동 SetLevel + Persistent 반영
- 미니게임 시작 시 `MiniGameManager.PlayAsync`가 `GetCurrentData(toolId)` 자동 조회 → `ApplyUpgrade` 주입
- Inventory 신규 재료 구매 시 `CanAcceptType`이 storage upgrade value 자동 조회
- Farm 성장/수확 시 farm upgrade value 자동 조회

### 관련 데이터 표시
- 현재 레벨 값 / 다음 레벨 값
- 코스트
- MAX 여부

### 시스템 접점
- 발행: `Money.TrySpend`, `Expense.Add("업그레이드")`
- 구독: (사용자 클릭이 트리거)
- 데이터: `upgrade_tool.csv`, `upgrade_storage.csv`, `upgrade_farm.csv`, `ShopPersistent.toolIds/Levels`, `ShopPersistent.storageTypes/Levels`, `GardenPersistent.upgradeTypes/Levels`
- 관련: shop-system(UI), cooking-system(도구/저장소 적용), garden-system(farm 적용), settlement-system(지출 카테고리), save-system(persistent)

---

## 16. Weather System (사장 상태)

관련 GDD: [`systems/weather-system.md`](../systems/weather-system.md)

### 주요 기능
- 하루 단위 `IsBadWeather` bool 판정 (`WeatherService`)
- `BadWeatherChance = 0.4f` — 나쁜 날씨 확률 40%
- `GameRandom.Immutable` 시퀀스로 결정적 갱신
- **현재 반영 범위 극히 제한적**: NPC 일상 대사 필터링 + Editor 시뮬레이션 로그만
- 손님 수/매출/텃밭 성장/BGM/VFX/미니게임 난이도 등 밸런스 결합 **미구현**

### UI/시각 요소
- 날씨 표시 UI 없음
- VFX/파티클 없음
- BGM/앰비언스 훅 없음

### 상호작용
- 플레이어가 날씨와 상호작용할 수 있는 UI 없음

### 자동 동작/이벤트
- `PassDay` 안에서 `Weather.UpdateWeather(day)` 자동 실행
- GameStart NewGame 시 (day=0 초기화 후) 자동 실행
- `CasualDialogueProvider.GetRandomDialogue`가 IsBadWeather 자동 조회하여 대사 필터
- SimHarness (Editor 전용) — 하루 종료 스냅샷에 IsBadWeather 자동 기록

### 관련 데이터 표시
- Weather 상태를 유저에게 보여주는 시각 요소 없음
- 게토로/파자마 NPC의 Good/Bad 대사가 유일하게 관찰 가능한 결과

### 시스템 접점
- 발행: (없음 — 이벤트 발행 안 함)
- 구독: `Progress.PassDay` 안에서 호출됨
- 데이터: `GameRandom.Immutable` 시퀀스
- 관련: dialogue-system(NPC 대사 필터), progression-system(PassDay), (외 시스템과 결합 없음)

**주의**: Weather는 현재 NPC 대사 필터 외에는 게임에 실제 반영되지 않음. QA는 이 시스템의 시각/청각/밸런스 결과를 관찰할 수 없다는 사실을 인지하고 접근할 것.

---

## 이 문서 원칙

- **QA 인풋 성격**: 이 문서는 QA가 테스트 케이스를 자유롭게 설계할 수 있도록, 시스템에 존재하는 관찰 가능한 기능/UI/상호작용/자동 동작/데이터/접점을 인벤토리 형태로 나열한다.
- **QA 영역 침범 금지**: 테스트 절차/기대치/우선순위/합격 기준은 이 문서에서 작성하지 않는다. QA가 이 문서를 재료 삼아 자신의 테스트 케이스를 설계한다.
- **관찰 가능성 우선**: 실제 게임 실행 시 플레이어가 보거나 자동으로 발생하는 것만 나열. 내부 로직이라도 결과가 관찰되지 않으면 "표시 안 됨"으로 명기하거나 생략.
- **실 코드 기반**: 각 항목은 실 GDD 문서(코드에서 파생) 및 실 코드에서 확인된 사실만 기록. 추측/의도/향후 계획은 배제.
- **경로 명시**: 각 시스템 상단에 대응 GDD 파일 링크. 필요 시 QA는 GDD로 drill down.
- **사장 상태도 기록**: Weather처럼 미반영 시스템도 "존재하지만 관찰 불가"임을 명기하여 QA가 시간 낭비하지 않도록 안내.
