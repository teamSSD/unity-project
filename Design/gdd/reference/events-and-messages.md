# Aftertaste — 이벤트 및 메시지

프로젝트 전체에서 사용되는 C# 이벤트, ScriptableObject 이벤트, 메시징 패턴 전수 조사.

## 0. 총계

| 카테고리 | 개수 | 비고 |
|---|---|---|
| `event Action<...>` (인스턴스) | 24 |  |
| `static event Action<...>` | 2 | BentoModel 튜토리얼 mock |
| `event System.Action<...>` | 5 | 코드 스타일 편차 (동일 의미) |
| `event EventHandler` | **0** | 미사용 |
| `UnityEvent` / `UnityEvent<T>` | **0** | 스크립트 노출 전무 |
| `public delegate` | **0** | 커스텀 delegate 없음 |
| ScriptableObject GameEvent | 4 클래스 + 8 asset | **코드 콜러 0건 (orphan)** |
| EventBus / MessageBus / EventQueue | **0** | 중앙집중형 없음 |

- **총 C# 이벤트 선언: 31건** (22 클래스)
- **Invoke 사이트: 74건**
- **`+= handler` 구독 사이트: 약 49건**

전 패턴이 **직접 참조 기반 pub/sub**. 발행자 인스턴스에 구독자가 `+= handler` 등록.

## 1. Domain 레이어 이벤트

### 1-1. StatsService (`Assets/Scripts/Domain/Common/StatsService.cs`)

| 이벤트 | 시그니처 | 라인 | 발행자 (같은 파일) | 주 구독자 |
|---|---|---|---|---|
| **OnTimeChanged** | `Action<int hour, int min>` | 14 | Initialize/Reset/AddMinutes/AddHours (line 47,58,69,83) | `Unity/UI/ClockUI.cs:24`, `Unity/Common/TimeManager.cs:68` (틱 SFX) |
| **OnStaminaChanged** | `Action<int>` | 15 | Initialize/Reset/AddStamina (46,57,92) | `Unity/UI/StaminaGauge.cs:28`, `Unity/UI/NumericStatsViewer.cs:40` |
| **OnMoneyChanged** | `Action<int>` | 16 | Initialize/Reset/AddMoney (45,56,103) | `Unity/UI/MoneyUI.cs:22`, `Unity/UI/NumericStatsViewer.cs:39`, `Unity/Common/StatsAudioAdapter.cs:19` (cashDrawer SFX) |
| **OnStaminaExhausted** | `Action` | 17 | AddStamina (line 93, stamina ≤ 0) | `Unity/Cooking/StatManager.cs:14` |

### 1-2. TutorialService (`Assets/Scripts/Domain/Common/TutorialService.cs`)

| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnStepShown** | `Action<int>` | 14 | ShowStep (line 58) | ⚠️ **없음 (dead event)** |
| **OnCompleted** | `Action` | 15 | Complete (line 66) | ⚠️ **없음 (dead event)** |

## 2. Unity/Common 이벤트

### 2-1. ProgressService (`Assets/Scripts/Unity/Common/ProgressService.cs`)

| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnPhaseChanged** | `Action<PhaseType>` | 14 | 내부 3곳 (line 49, 65, 80) | `SoundManager.cs:53,128` (BGM 전환), `MallSceneController.cs:86`, `Cooking/CookingBackgroundController.cs:13`, `Garden/Farm.cs:63` |

### 2-2. TimeManager (`Assets/Scripts/Unity/Common/TimeManager.cs`)

| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnTimePaused** | `Action` | 25 | Pause (line 85) | ⚠️ 구독자 없음 |
| **OnTimeResumed** | `Action` | 26 | Resume (line 94) | ⚠️ 구독자 없음 |
| **OnTimeEnd** | `Action` | 27 | Tick (line 119) | `Cooking/CustomerManager.cs:78` |

### 2-3. RecipeBookManager (`Assets/Scripts/Unity/Common/RecipeBookManager.cs`)

| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnRecipeBookClosed** | `System.Action` | 169 | Close (line 179) | `Tutorial/TutorialController.cs:128` |

## 3. Unity/UI 이벤트

### PhaseActionSelector (`Assets/Scripts/Unity/UI/PhaseActionSelector.cs`)
| 이벤트 | 시그니처 | 라인 | 구독자 |
|---|---|---|---|
| **OnActionExecuted** | `Action<ActionType>` | 33 | `MallSceneController.cs:71` |

### 설정 셀렉터
| 이벤트 | 시그니처 | 파일:라인 | 구독자 |
|---|---|---|---|
| **OnChanged** | `System.Action` | `ScreenResolutionSelector.cs:53` | `SettingsController.cs:37` |
| **OnChanged** | `System.Action` | `FullScreenSelector.cs:31` | `SettingsController.cs:38` |

## 4. Unity/Cooking 이벤트

### BaseStorage / FoodModel
| 이벤트 | 시그니처 | 파일:라인 | 구독자 |
|---|---|---|---|
| **OnFoodDestroyedForRefill** | `Action<BaseStorage, FoodData>` | `BaseStorage.cs:58` | `Tutorial/TutorialCookingController.cs:81` |
| **onDestroy** | `Action<FoodModel>` | `FoodModel.cs:21` | `BaseStorage.cs:42` |

### 손님 (Customer)
| 이벤트 | 시그니처 | 파일:라인 | 구독자 |
|---|---|---|---|
| **onExit** (Ordering) | `Action` | `OrderingCustomer.cs:12` | `CustomerSpawner.cs:66` (익명 람다) |
| **onExit** (Waiting) | `Action = ()=>{}` | `WaitingCustomer.cs:10` | `CustomerLifecycle.cs:78` |

### CustomerManager (`Assets/Scripts/Unity/Cooking/CustomerManager.cs`)
| 이벤트 | 시그니처 | 라인 | 구독자 |
|---|---|---|---|
| **OnGameEnd** | `Action` | 51 | 자기 자신 line 85(subScene.ReturnToIdle), `Tutorial/TutorialCookingController.cs:50` |
| **OnCustomerResolved** | `Action<bool>` | 53 | `Tutorial/TutorialCookingController.cs:48` |

### CustomerLifecycle (`Assets/Scripts/Unity/Cooking/CustomerLifecycle.cs`)
| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnCustomerServed** | `Action<CustomerLifecycle, ValidationResult, int>` | 21 | HandleOrderDelivered (line 145) | `CustomerManager.cs:123,283` (add), `154,252,315,331` (remove) |
| **OnCustomerLeft** | `Action<CustomerLifecycle>` | 22 | HandleTimeout (188) | `CustomerManager.cs:124,284` |
| **OnTutorialOrderPlaced** | `Action<OrderTicketModel>` | 24 | Setup (84) | `TutorialCookingController.cs:211` ⚠️ 발행이 등록보다 빠를 위험 |
| **OnTutorialTakingSpawned** | `Action<GameObject>` | 27 | HandleOrderDelivered (148) | `TutorialCookingController.cs:177` |

### OrderTicketModel (`Assets/Scripts/Unity/Cooking/OrderTicketModel.cs`)
| 이벤트 | 시그니처 | 라인 | 구독자 |
|---|---|---|---|
| **OnAttached** | `Action = ()=>{}` | 29 | `CustomerLifecycle.cs:81` |
| **onTake** | `Action<FoodSchema, List<FoodSchema>, Vector3>` | 31 | `CustomerLifecycle.cs:82`, `OrderTicketController.cs:41,62` (2회 등록), `DeliveryTicketCoordinator.cs:52` |

### 미니게임
| 이벤트 | 시그니처 | 파일:라인 | 구독자 |
|---|---|---|---|
| **OnGameFinished** | `Action<float>` | `MiniGameAbstract.cs:24` | `MiniGameManager.cs:51` (익명 람다) |

### BentoModel (`Assets/Scripts/Unity/Cooking/BentoModel.cs`) — **static event**
| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnFoodAddedForTutorial** | `static Action<BentoModel, FoodSchema>` | 16 | AddIngredient (96) | `TutorialCookingController.cs:167` |
| **OnBentoPlacedForTutorial** | `static Action<BentoModel>` | 19 | OnDock (118) | `TutorialCookingController.cs:160` |

## 5. Unity/Tutorial

### TutorialController (`Assets/Scripts/Unity/Tutorial/TutorialController.cs`)
| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnPartShown** | `System.Action<int, int, TutorialStepPart>` | 22 | ShowPart (131) | `TutorialCookingController.cs:57`, `MallSceneController.cs:215` |

## 6. Unity/Mall

### DialogueManager (`Assets/Scripts/Unity/Mall/DialogueManager.cs`)
| 이벤트 | 시그니처 | 라인 | 발행자 | 구독자 |
|---|---|---|---|---|
| **OnDialogueEnded** | `System.Action<string>` (resultTag) | 50 | End (line 298) | `Mall/NPCDialogue.cs:44`, `Mall/DeliveryNpcDialogueInteraction.cs:92,113` (2회: Delivery + Casual) |

## 7. ScriptableObject Event 인프라 (orphan)

### 정의 (`Assets/Scripts/Schema/Events/`, namespace `Game.Schema.Events`)

| 클래스 | 파일 | 페이로드 |
|---|---|---|
| `GameEventBase` (abstract) | `GameEventBase.cs` | `description` |
| `GameEvent` | `GameEvent.cs` | void (`Action`) |
| `IntEvent` | `IntEvent.cs` | `Action<int>` |
| `FloatEvent` | `FloatEvent.cs` | `Action<float>` |
| `StringEvent` | `StringEvent.cs` | `Action<string>` |

API: `Subscribe / Unsubscribe / Raise / ListenerCount`. 중복 등록 방지, 역순 순회로 재진입 안전.

### .asset 인스턴스 (`Assets/Scripts/Schema/Events/Catalog/`, 8건)

| .asset | 매핑 클래스 | 상태 |
|---|---|---|
| `OnDayPassed.asset` | `GameEvent` | **미사용** |
| `OnPhaseStarted.asset` | `IntEvent` | **미사용** |
| `OnMoneyChanged.asset` | `IntEvent` | **미사용** |
| `OnStaminaChanged.asset` | `IntEvent` | **미사용** |
| `OnTimeChanged.asset` | `FloatEvent` | **미사용** |
| `OnUpgradeApplied.asset` | `StringEvent` | **미사용** |
| `OnSceneTransitionRequested.asset` | `StringEvent` | **미사용** |
| `OnDeliveryAccepted.asset` | `StringEvent` | **미사용** |

**중요 발견**: `grep -rEn "GameEventBase|IntEvent|FloatEvent|StringEvent"` 결과 `Schema/Events/` 외부 참조 **0건**. `.Raise(` / `.Subscribe(` 호출 **0건**. SO Event 인프라는 완전히 orphan 상태. 서비스가 발행하는 `OnMoneyChanged` 등과 이름이 겹치지만 실제 코드는 여전히 C# event로 우회.

## 8. UnityEvent / Delegate / EventBus

- `grep -rEn "UnityEvent" Assets/Scripts/` → **0건**
- `grep -rEn "using UnityEngine.Events" Assets/Scripts/` → **0건**
- `grep -rEn "public delegate|private delegate|internal delegate"` → **0건**
- `grep -rEn "EventBus|MessageBus|EventQueue|EventChannel"` → **0건**

Button.onClick 같은 Inspector-wireable UnityAction은 프리팹 내부에서만 사용, 스크립트 필드로 노출된 UnityEvent 없음. 커스텀 delegate 타입 선언 없음 — 모두 `Action` 계열 제네릭.

## 9. 이벤트 밀도 hotspot

### 발행자 밀도 Top 5
| 파일 | 이벤트 수 | 성격 |
|---|---|---|
| `Cooking/CustomerLifecycle.cs` | 4 | 손님 상태 브로드캐스트 허브 |
| `Domain/Common/StatsService.cs` | 4 | 스탯 통보 채널 |
| `Common/TimeManager.cs` | 3 | 시간 흐름 signal |
| `Cooking/CustomerManager.cs` | 2 | 게임 종료 + 손님 해결 |
| `Cooking/BentoModel.cs` | 2 (static) | 튜토리얼 mock 훅 |

### 구독자 hotspot Top 3
| 파일 | 구독 수 | 성격 |
|---|---|---|
| `Tutorial/TutorialCookingController.cs` | 8+ | 게임플레이 crosscutting 관찰자 |
| `Cooking/CustomerManager.cs` | 4+ | Lifecycle × N 반복 등록/해제 |
| `Common/MallSceneController.cs` | 3 | Phase/Action/Tutorial 라우팅 허브 |

## 10. 이벤트 부채 / 정리 후보

### Dead events (구독자 0건)
- `TutorialService.OnStepShown`, `TutorialService.OnCompleted` (`Domain/Common/TutorialService.cs:14-15`) — Domain은 이벤트만 있고 Unity 측은 다른 채널(`OnPartShown`)로 UI 갱신
- `TimeManager.OnTimePaused`, `TimeManager.OnTimeResumed` — 발행되지만 구독자 grep 미검출

### 네이밍 컨벤션 편차
- `On*` (PascalCase) 지배적이지만 소문자 남아있음: `onExit`(Ordering/Waiting), `onDestroy`(FoodModel), `onTake`(OrderTicketModel) — 4곳

### 스타일 편차 (`event System.Action` vs `event Action`)
- 5개 파일 fully-qualified: `ScreenResolutionSelector`, `FullScreenSelector`, `RecipeBookManager`, `DialogueManager`, `TutorialController`
- `using System;` 통일로 청소 가능

### SO Event 인프라 orphan
- 8개 asset 존재, 실제 코드 사용 0건 — 채용/폐기 결정 필요

### 잠재 위험
- **static event (BentoModel)**: 씬 전환/도메인 리로드로 stale delegate 누적 위험. `TutorialCookingController.OnDisable(281-282)`에서 `-=` 처리는 있으나 순서 이슈 가능. `?.Invoke` null 안전으로 커버.
- **`CustomerManager.OnGameEnd += subScene.ReturnToIdle`**(85): Manager가 자기 이벤트를 자기가 등록하는 프록시 패턴 — 코드 리뷰 시 혼동 가능.
- **`OrderTicketController.CreateTicket`(41)** + **`RemoveTicket`(62)**: 각각 별 익명 람다 인스턴스라 `-=` 해제 불가. 티켓 재사용 시 누수 위험. `DeliveryTicketCoordinator.cs:52,59`는 handler를 딕셔너리에 저장 후 `-=` 하는 올바른 패턴.
- **`CustomerLifecycle.OnTutorialOrderPlaced`**(24): `Setup()`에서 즉시 발행(84). 구독 등록보다 발행이 빠르면 유실 위험 — 튜토리얼 시 재현 가능성 검증 필요.

## 11. 관련 파일 index

### Domain
- `Assets/Scripts/Domain/Common/StatsService.cs`
- `Assets/Scripts/Domain/Common/TutorialService.cs`

### Unity/Common
- `Assets/Scripts/Unity/Common/ProgressService.cs`
- `Assets/Scripts/Unity/Common/TimeManager.cs`
- `Assets/Scripts/Unity/Common/RecipeBookManager.cs`
- `Assets/Scripts/Unity/Common/MallSceneController.cs`
- `Assets/Scripts/Unity/Common/SoundManager.cs`
- `Assets/Scripts/Unity/Common/StatsAudioAdapter.cs`

### Unity/Cooking
- `Assets/Scripts/Unity/Cooking/CustomerManager.cs`
- `Assets/Scripts/Unity/Cooking/CustomerLifecycle.cs`
- `Assets/Scripts/Unity/Cooking/CustomerSpawner.cs`
- `Assets/Scripts/Unity/Cooking/OrderTicketController.cs`
- `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs`
- `Assets/Scripts/Unity/Cooking/DeliveryTicketCoordinator.cs`
- `Assets/Scripts/Unity/Cooking/BentoModel.cs`
- `Assets/Scripts/Unity/Cooking/FoodModel.cs`
- `Assets/Scripts/Unity/Cooking/BaseStorage.cs`
- `Assets/Scripts/Unity/Cooking/OrderingCustomer.cs`, `WaitingCustomer.cs`
- `Assets/Scripts/Unity/Cooking/MiniGameAbstract.cs`, `MiniGameManager.cs`
- `Assets/Scripts/Unity/Cooking/StatManager.cs`
- `Assets/Scripts/Unity/Cooking/CookingBackgroundController.cs`

### Unity/UI
- `Assets/Scripts/Unity/UI/PhaseActionSelector.cs`
- `Assets/Scripts/Unity/UI/ScreenResolutionSelector.cs`, `FullScreenSelector.cs`
- `Assets/Scripts/Unity/UI/SettingsController.cs`
- `Assets/Scripts/Unity/UI/NumericStatsViewer.cs`, `MoneyUI.cs`, `StaminaGauge.cs`, `ClockUI.cs`

### Unity/Tutorial / Mall / Garden
- `Assets/Scripts/Unity/Tutorial/TutorialController.cs`, `TutorialCookingController.cs`
- `Assets/Scripts/Unity/Mall/DialogueManager.cs`, `NPCDialogue.cs`, `DeliveryNpcDialogueInteraction.cs`
- `Assets/Scripts/Unity/Garden/Farm.cs`

### SO Event (orphan)
- `Assets/Scripts/Schema/Events/GameEventBase.cs`
- `Assets/Scripts/Schema/Events/GameEvent.cs`, `IntEvent.cs`, `FloatEvent.cs`, `StringEvent.cs`
- `Assets/Scripts/Schema/Events/Catalog/*.asset` (8건)
