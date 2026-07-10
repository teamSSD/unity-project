# Cooking.unity — 조리대 씬

> 파일: `Assets/Scenes/ForReal/Cooking.unity`
> 진입: Mall의 Preparation → BentoSelection 확정 → 자동 로드.

## 역할 한 줄

**Top-down 조리대 뷰**. 5종 요리도구·저장소·도시락 슬롯을 배치, 손님(주문/대기/픽업) 흐름과 미니게임을 오케스트레이션.

## GameObject 계층

```
Cooking.unity (root)
├── EventSystem
├── Canvas                        UI (HUD, buttons)
│   ├── EarlyEndButton            "영업 조기 종료" 버튼 (우상단)
│   └── Label
├── TopUICanvas                   상단 UI (시계 등)
├── WorldCanvas                   world-space UI (손님 위 타이머, 영수증 스폰 위치)
├── Main Camera                   HorizontalCameraMove (좌우 이동, 튜토리얼 시 강제 위치)
├── SubSceneController            (Game.Runtime::SubSceneController, returnScene=Mall)
├── GameManager                   CookingSceneManager + CustomerManager + MiniGameManager
├── 배경
│   ├── bg_cuisine_morning        낮 배경 (Preparation/Morning/Afternoon)
│   ├── bg_cuisine_evening        저녁 배경
│   └── bg_cuisine_night          밤 배경
│   └── (bg_cuisine_afternoon 슬롯도 CookingBackgroundController 참조 가능)
├── 요리 도구                     5종 조리도구 그룹
│   ├── 인덕                     (인덕션) — CookingToolModel + 관련 컴포넌트
│   ├── (팬 / 냄비 / 볼 / 도마 / 철판)  각 도구는 CookingToolModel + MinigameId 매핑
│   └── 테이블
├── 냉장고 및 캐비넷              저장소 그룹
│   ├── 냉장고 (Refrigerator + 냉장고 내부)
│   ├── 캐비넷(상) (UpperShelf)
│   └── 캐비넷(하) (LowerShelf, capacity=99, xOffset=-2.23, yPosition=0.35, xInterval=0.755)
├── 도시락 관련
│   ├── 도시락 위치 1            BentoPositionModel
│   ├── 도시락 위치 2            BentoPositionModel
│   ├── 도시락 위치 3            BentoPositionModel
│   └── 도시락묶음                BentoSet (묶음 배치)
└── 쓰레기통                       버리기 트리거
```

## 붙어있는 스크립트

| 클래스 | 위치 | 부착 대상 |
|---|---|---|
| `SubSceneController`           | `Unity/Common/SubSceneController.cs`    | SubSceneController GO (returnScene=Mall) |
| `CookingSceneManager`          | `Unity/Cooking/CookingSceneManager.cs`  | GameManager GO |
| `CustomerManager`              | `Unity/Cooking/CustomerManager.cs`      | GameManager GO |
| `MiniGameManager`              | `Unity/Cooking/MiniGameManager.cs`      | GameManager GO |
| `CustomerSpawner`              | `Unity/Cooking/CustomerSpawner.cs`      | (CustomerManager 자식 또는 인젝션 대상) |
| `OrderTicketController`        | `Unity/Cooking/OrderTicketController.cs`| ticket 관리 컴포넌트 |
| `CookingBackgroundController`  | `Unity/Cooking/CookingBackgroundController.cs` | 배경 그룹 |
| `TimeManager`                  | `Unity/Common/TimeManager.cs`           | Singleton |
| `DeliveryTicketCoordinator`    | `Unity/Cooking/DeliveryTicketCoordinator.cs` | 배달 quest 티켓 |
| `EarlyEndButton`               | `Unity/Cooking/EarlyEndButton.cs`       | Canvas의 EarlyEndButton |

## CookingSceneManager — 씬 컴포지션 루트

[`CookingSceneManager.cs`](../../../Assets/Scripts/Unity/Cooking/CookingSceneManager.cs)

### 인스펙터 필드
```csharp
[SerializeField] List<GameObject> cookingTools;        // 5종 조리도구 (팬/냄비/볼/도마/철판)
[SerializeField] GameObject refrigeratorGameObject;    // 냉장고
[SerializeField] GameObject upperShelfGameObject;      // 캐비넷(상)
[SerializeField] GameObject lowerShelfGameObject;      // 캐비넷(하)
[SerializeField] GameObject foodPrefab;                // 재료 스폰용 prefab
[SerializeField] Canvas canvas;                        // WorldCanvas 참조
```

### Start()
1. `MiniGameManager`(자기 GO), `RecipeLookup`, `Inventory` service 참조 획득
2. 각 `cookingTool`에 `CookingToolModel.Inject(playMinigameUsecase, searchRecipeUsecase)`
3. `Refrigerator / UpperShelf / LowerShelf` 컴포넌트 획득
4. **Capacity 명시 주입** — `InjectStorageCapacity(storage, DefaultCapacity)` (BaseStorage 패턴)
5. **FillStorage** — 인벤토리 재료를 저장소별 카테고리(`Refrigerator/UpperShelf/LowerShelf`)로 배치
6. `TryStartCookingTutorial()` — `CookingIntro` 스텝 활성 시 표시 후 완료 시 `TutorialCookingController.ReleaseTutorialLocks()`

## BaseStorage 파생 3종

패턴: [MEMORY.md의 BaseStorage 리팩터링 참조](../systems/save-system.md))

- **Refrigerator**: `DefaultCapacity` 정의, `CalculatePositionForIndex(int index)` 오버라이드
- **UpperShelf**: 캐비넷(상) 상단 배치
- **LowerShelf**: 캐비넷(하) 하단 배치. Cooking 씬 인스펙터 값: `capacity=99, xOffset=-2.23, yPosition=0.35, xInterval=0.755`

공통 로직 (`BaseStorage.cs`): `AddIngredients(list)`, `HandleFoodDestroyed`, `RefreshPosition`. 서브클래스는 위치 계산만.

`OnFoodDestroyedForRefill` 이벤트를 CookingTutorial이 구독해 자동 refill.

## CustomerManager — 손님 흐름 오케스트레이터

[`CustomerManager.cs`](../../../Assets/Scripts/Unity/Cooking/CustomerManager.cs)

역할: `CustomerSpawner` + `OrderTicketController` + `CustomerLifecycle` 조정.

### 이벤트
- `OnCustomerResolved(bool wasServed)` — 손님 개별 처리 완료 (served/left angry)
- `OnGameEnd` — EarlyEnd 또는 시간 종료로 페이즈 마감

### 주요 액션
- `EndEarly()` — EarlyEndButton or Tutorial에서 강제 종료
- `TutorialSpawnOne()` — CookingTutorial 씬 전용 mock 스폰
- `AutoSpawnEnabled` — 튜토리얼에서 자동 스폰 억제
- `GetCurrentOrderingCustomer()` — 튜토리얼 target 등록용

### CustomerSpawner 배치 상수
[`CustomerSpawner.cs`](../../../Assets/Scripts/Unity/Cooking/CustomerSpawner.cs)

```
Ordering 손님 (카운터):  world position (3.02, -0.26, 0)
Waiting 손님 (5명 슬롯): base (-8, 0.78, 0) + offset (1.75, 0, 0) × index
  Waiting 타이머 캔버스: y=426, xOffset=-65 (Canvas 좌표), scale=0.6
Taking 손님 (퇴장):      exit (-9.89, -0.85, 0)
Scale: orderingTaking=0.45, waiting=0.297 (× CustomerData.displayScale)
```

### OrderTicketController 배치
[`OrderTicketController.cs`](../../../Assets/Scripts/Unity/Cooking/OrderTicketController.cs)

- 일반 ticket: base `(-8, 4.474, 0)` + slotOffset `(1.75, 0, 0)` × 인덱스 (손님 waitingIndex와 매핑)
- Delivery ticket: 6번째 슬롯 (`QuestSlotIndex=5`)에 세로 스택 (offset `(0, -1, 0)`)
- Spawn 애니메이션: `-2y`에서 위로 올라옴 (영수증 호버 시 내려오는 동작 포함)

## BentoPositionModel — 도시락 슬롯 3개

[`BentoPositionModel.cs`](../../../Assets/Scripts/Unity/Cooking/BentoPositionModel.cs)

각 "도시락 위치 1/2/3" GO에 부착.
```csharp
public bool isSet { get; set; }             // 현재 도시락 배치 여부
[SerializeField] float blinkSpeed = 2.5f;   // 하이라이트 깜빡임 (Hz)
```

## TimeManager — 시간 흐름

[`TimeManager.cs`](../../../Assets/Scripts/Unity/Common/TimeManager.cs)

```csharp
[SerializeField] float gameTimeScale = 120f;     // 게임시간 배속
[SerializeField] int startHour = 11, startMinute = 0;
[SerializeField] int endHour   = 15, endMinute   = 0;
[SerializeField] float defaultCustomerWaitTime = 90f;
[SerializeField] float closedLocalTimerScale = 2f;  // 영업 종료 후 대기 손님 인내심 가속
```

이벤트: `OnTimePaused`, `OnTimeResumed`, `OnTimeEnd`.

## CookingBackgroundController — 페이즈별 배경

[`CookingBackgroundController.cs`](../../../Assets/Scripts/Unity/Cooking/CookingBackgroundController.cs)

`Progress.OnPhaseChanged` 구독해 세 배경 GO를 토글:
- `bgMorning` (Preparation/Morning/Afternoon) 
- `bgEvening` (Evening)
- `bgNight`   (Night)

`bg_cuisine_afternoon` 배경은 씬에도 존재하지만 컨트롤러의 세 슬롯 중 하나에 wire.

## EarlyEndButton — 조기 종료

[`EarlyEndButton.cs`](../../../Assets/Scripts/Unity/Cooking/EarlyEndButton.cs)

- Canvas의 EarlyEndButton GO에 부착
- 클릭 → `ConfirmModal.Show("영업 조기 종료" / "다음 페이즈로 넘어갑니다." / "종료하기" / "취소")` → `CustomerManager.EndEarly()`

## 페이즈 종료 → Mall 복귀

`CustomerManager.OnGameEnd` → `SubSceneController.ReturnToIdle()` → `Progress.PassPhase()`.
`PassPhase()` 결과에 따라:
- Night → 자동 Settlement 씬 로드 (return-scene skip)
- 그 외 → Mall로 복귀

## 관련 시스템

- [Mall 씬](./scene-mall.md) — 진입/복귀
- [CookingTutorial 씬](./scene-cookingtutorial.md) — mock 격리 버전
- [Settlement 씬](./scene-settlement.md) — Night 페이즈 종료 자동 진입
- `MenuSelectionService`, `RecipeLookupService`, `InventoryService`
- `SearchDataUtil`: FoodData/RecipeData/CookingToolData 조회
- Tutorial: `CookingIntro`
- MEMORY (BaseStorage/CustomerManager 리팩터링 히스토리)
