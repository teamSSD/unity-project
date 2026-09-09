# 손님 시스템 (Customer System)

Cooking 씬 내 손님 생성, 대기, 주문, 서빙, 퇴장의 전 흐름을 관리한다. 페이즈별 스폰 빈도와 페이션스 타이머, 주문 정확도 판정과 보상 계산까지 포함.

관련 문서: [cooking-system.md](./cooking-system.md), [menu-compositions.md](./menu-compositions.md)

---

## 1. 아키텍처 개요

`CustomerManager.cs`가 스폰 루프와 lifecycle 조정을 담당하고, 세부 책임은 컴포넌트에 분산.

`Assets/Scripts/Unity/Cooking/CustomerManager.cs:11~13` — `[RequireComponent]`로 아래 컴포넌트를 강제.

| 컴포넌트 | 파일 | 책임 |
|---|---|---|
| `CustomerManager` | `CustomerManager.cs` | 스폰 루프, phase 설정, lifecycle 콜백, 영업 종료 |
| `CustomerSpawner` | `CustomerSpawner.cs` | 3종 손님 프리팹 인스턴스화 + 대기 위치 풀 관리 |
| `OrderTicketController` | `OrderTicketController.cs` | 영수증 티켓 5+1개 슬롯 배치 |
| `DeliveryTicketCoordinator` | `DeliveryTicketCoordinator.cs` | 배달 퀘스트 티켓 (6번 슬롯 세로 스택) |
| `CustomerLifecycle` | `CustomerLifecycle.cs` | 단일 손님의 Ordering→Waiting→Taking/Exiting 흐름 |
| `CustomerSessionStats` | `Domain/Cooking/CustomerSessionStats.cs` | 세션 누적 통계 (POCO) |

### 1.1 손님 상태 flow

```
[Spawn] → Ordering(카운터) ─(클릭)→ Waiting(대기열) ─(주문 서빙)→ Taking(수령 애니 3초) → Destroy
                                        └(90s 시한 초과)→ Exit(퇴장 애니 3초) → Destroy
```

각 단계는 별개의 프리팹 (`orderingCustomerPrefab`, `waitingCustomerPrefab`, `takingCustomerPrefab`)으로 스폰되며 상태 전환 시 이전 인스턴스를 destroy하고 다음 인스턴스를 스폰.

---

## 2. 스폰 파라미터 (페이즈별)

`CustomerManager.ApplyPhaseSettings()` (`CustomerManager.cs:88~98`):

| Phase | baseSpawnInterval | spawnIntervalVariance | 성격 |
|---|---:|---:|---|
| Morning | 35s | 8s | 손님 적음 |
| Afternoon | 30s | 8s | 보통 |
| Evening | 22s | 5s | 붐빔 |
| Night | 15s | 4s | 가장 붐빔 |

- 실제 다음 스폰 시간: `GameRandom.Normal(mean=baseSpawnInterval, σ=spawnIntervalVariance)` — 정규분포.
- **첫 손님**: 페이즈 시작 후 약 `Normal(3s, σ=1s)`에 스폰 (`CustomerManager.cs:82`).
- **스폰 게이트**: `currentOrderingCustomer != null` (이미 카운터에 대기 중) 또는 `spawner.IsWaitingQueueFull()` (대기석 3자리 모두 참) 시 다음 tick으로 skip.

### 2.1 대기 위치 풀

`CustomerSpawner.cs:40~49`:

- `MaxWaitingCustomers = 3` — 동시 대기 최대 3명.
- 물리적 자리는 총 5개 슬롯 (index 0~4), `waitingBasePosition = (-8, 0.78)`부터 `offset = (1.75, 0)` 씩 오른쪽.
- 각 스폰 시 사용 가능한 슬롯 중 랜덤 픽 (`GameRandom.Pick`) → 자리 확보. 서빙/퇴장 시 `ReleaseWaitingPosition()`으로 반환.
- Y/X에 `waitingPositionVariance = (0.3, 0.5)` 정도의 랜덤 오프셋 부여 (자연스럽게).

### 2.2 손님 종류 풀 (CustomerData)

`Assets/Bundles/ScriptableObjects/Customer/` 10종:

| Asset | type | npcId | displayScale |
|---|---|---|---|
| emotional | emotional | npc_getoro | 1 (default) |
| gabriel | glum | npc_gabriel | 1 |
| lede | warm | npc_lede | 1 |
| lin | warm | npc_lin | 0.5 |
| machine | machine | npc_jar | 1 |
| moai | grumbling | npc_moai | 1 |
| nimo | impatient | npc_nimo | 1 |
| pajama | emotional | npc_pajama | 1 |
| seraph | warm | npc_seraph | 1 |
| sranya | glum | npc_sranya | 1 |

각 `CustomerData` 필드 (`CustomerData.cs`):
- `type` — 성격 라벨 (현재 로직상 사용처는 시각적 다양성 위주).
- `npcId` — 배달 퀘스트 NPC와 매핑. 활성 손님 + 활성 배달 주문의 npcId는 다음 스폰 후보에서 제외 (`CustomerManager.CollectExcludedNpcIds`, 라인 204~231).
- `characterImage` — 스프라이트.
- `displayScale` — 시각 크기 보정 (스프라이트 원본 차이 흡수).
- `orderingMessage` / `satisfiedMessage` / `unsatisfiedMessage` / `escapeMessage` — 각 상태별 스피치 버블 텍스트.

### 2.3 손님 크기 세팅

`CustomerSpawner.cs:12~21`:

- Ordering/Taking 손님 world scale: `0.45 × customerData.displayScale`.
- Waiting 손님 world scale: `0.297 × customerData.displayScale`.
- Waiting 손님 머리 위 게이지 UI: 자체 canvas 좌표 y=426, x=(자동) - 65 오프셋, 스케일 0.6.

---

## 3. 손님 인내심 (Waiting Timer)

`Assets/Scripts/Unity/Common/TimeManager.cs:17~21`:

- `defaultCustomerWaitTime = 90f` — 대기 손님 기본 인내심 (초).
- `closedLocalTimerScale = 2f` — 영업 종료 후 잔여 대기 손님 인내심 2배 가속 (빨리 나가라).

`WaitingCustomer.Start()` (`WaitingCustomer.cs:60~75`):
1. `TimeManager.StartCustomerTimer(onTick, onComplete)` 호출.
2. onTick: 매 프레임 `GaugeUI.SetProgress(elapsed, duration)` — 머리 위 게이지 감소.
3. onComplete (90초 초과): `OnExit()` → `CustomerLifecycle.OnCustomerTimeout()` 발화 → exit 애니.

### 3.1 서빙 성공 시 타이머 정지

주문 티켓이 도시락에 부착되는 순간 (`OrderTicketModel.OnAttached` 이벤트) → `CustomerLifecycle.OnTicketAttached()` → `waitingCustomer.StopTimer()` (`CustomerLifecycle.cs:87~90`).

### 3.2 영업 종료 시 인내심 가속

`TimeManager.CheckBreakPoint()`가 endHour(기본 15:00) 도달 시 → `localTimerScale = closedLocalTimerScale = 2f` → 잔여 대기 손님 타이머가 2배 속도로 진행. 다 처리하지 못한 손님은 `waitingCustomer.OnExit()`으로 자연 timeout.

`CustomerManager.OnTimeEnd()` (`CustomerManager.cs:233`):
- Ordering 손님은 즉시 destroy (걷어나가는 애니 없음).
- 주문 전 상태(IsStuckPreOrder — waitingCustomer/orderTicket 모두 null)의 lifecycle 정리.
- 활성 티켓 소진 후 → `OnGameEnd` 발화 → SubSceneController가 Idle로 복귀.

---

## 4. 주문 티켓 (Receipt)

`OrderTicketController.cs`가 관리.

### 4.1 슬롯 배치

- `basePosition = (-8, 4.474)` (화면 상단, 대기 손님과 동일 X)
- `slotOffset = (1.75, 0)` — 손님 대기 슬롯과 동일 X 정렬.
- 일반 손님 티켓: `slotIndex = waitingPositionIndex` (손님과 1:1 매핑) → `basePosition + slotOffset * slotIndex`.
- 배달 퀘스트 티켓: 6번째 슬롯(index 5)에 세로 스택. 여러 배달 주문 대응.

### 4.2 티켓 생성 흐름

`CustomerLifecycle.OnOrderPlaced()`:
1. `spawner.SpawnWaitingCustomer(customerData)` → `positionIndex` 확보.
2. `ticketController.CreateTicket(menuSchema, positionIndex, onTicketTaken, onCustomerExit)`.
3. 티켓은 초기에 `basePosition - (0, 2)`에서 스폰되어 자기 자리로 상승하는 애니. (`OrderTicketController.cs:37`)
4. `orderTicket.OnAttached += OnTicketAttached` (waitingTimer 정지) + `orderTicket.onTake += OnOrderDelivered` (서빙 처리).

### 4.3 티켓 부착 규칙

`OrderTicketModel.AddToBento()` (`OrderTicketModel.cs:54~95`):
- 도시락 위에 티켓 드롭 시 `BentoModel`이 겹쳐 있으면 부착 시도.
- `IsDelivery == true`인 경우 `ValidateExactMatch()`: 도시락의 food 리스트가 menuSchema의 main+side와 정확히 일치해야 성공. 일반 손님 티켓은 이 검증 없음 (부분 일치도 부착 성공, 점수/보상만 감소).
- 부착 후 티켓은 도시락 자식으로 reparent, `attachedScale=0.7`로 축소, sortingOrder +100.
- 부착 후 0.5~1.5초 랜덤 대기 → `onTake` 이벤트로 서빙 확정 (`WaitAndTakeAsync`).

### 4.4 손님 이동 위치

`CustomerSpawner.cs:46, 52`:

- **Ordering 위치**: `(3.02, -0.26)` — 카운터 오른쪽 앞. 손님이 스폰되면 여기서 대기하다가 클릭 후 "말"하고 다시 클릭 시 떠남.
- **Exit 위치**: `(-9.89, -0.85)` — 화면 왼쪽 아래로 퇴장 (실패한 손님 전용).
- **Taking 위치**: caller가 넘긴 위치(도시락이 있던 곳)의 y를 `orderingPosition.y`(=-0.26)로 정렬하여 사용.

---

## 5. MenuValidator 점수 · 보상 공식

`Assets/Scripts/Domain/Common/MenuValidator.cs`

### 5.1 Validate() — 정확도 점수 (0.0 ~ 1.0)

`MenuValidator.CalculateAccuracyScore()` (`MenuValidator.cs:123~157`):

```
mainScore = mainCorrect ? 0.6 : 0.0

if (expectedSides > 0):
    if (correctSides == expectedSides && providedSides == expectedSides):
        sideScore = 0.4    # perfect
    else:
        correctRatio = correctSides / expectedSides
        extraPenalty = max(0, providedSides - expectedSides) × 0.1
        sideScore = clamp01(correctRatio × 0.4 - extraPenalty)
else:  # 사이드 요구 없음
    if (providedSides == 0): sideScore = 0.4
    else: sideScore = max(0, 0.4 - providedSides × 0.1)

accuracyScore = clamp01(mainScore + sideScore)
```

- **메인 매칭**: `foodData.id == order.mainMenu.id`.
- **사이드 매칭**: id 기반 multiset 매칭 (순서 무관, 주문에 요구된 개수까지만 일치로 계산).
- **잘못된 사이드 존재 시**: `providedSides > expectedSides` 각 1개당 0.1 감점.

### 5.2 등급 산출

`MenuValidator.GetGrade()` (`MenuValidator.cs:192~200`):

| 정확도 | 등급 |
|---|---|
| ≥ 1.0 | S |
| ≥ 0.9 | A |
| ≥ 0.8 | B |
| ≥ 0.7 | C |
| ≥ 0.6 | D |
| < 0.6 | F |

`ValidationFeedbackUI`에 등급 + 정확도 + 보상 + 피드백 메시지가 전달됨 (`CustomerManager.OnCustomerServed:132~144`).

### 5.3 CalculateReward() — 실제 지급 골드

`MenuValidator.CalculateReward()` (`MenuValidator.cs:203~234`), 기획 공식:

```
totalPrice = providedMain.Price + Σ providedSide.Price       # 페널티 없이 단순 합
mainMultiplier = (order.mainMenu.id == providedMain.foodData.id) ? 1.0 : 0.7
sideMultiplier = 1.0 + (matchingSidesCount × 0.15)          # 2026-07 튜닝: 5%→15%

reward = round(totalPrice × mainMultiplier × sideMultiplier)
```

- 메인이 틀리면 30% 감점.
- 사이드 매칭 개수당 15% 승수 가산. 3개 매칭 시 `1.0 + 3×0.15 = 1.45` 배.
- Food의 `Price`는 조리 시점의 `CookingToolSchema.Cook()`이 산출한 값 (재료 원가 × 미니게임 점수 기반, [cooking-system.md §2.4](./cooking-system.md#24-조리-결과-가격-계산)).

### 5.4 피드백 메시지

`MenuValidator.GenerateFeedback()` (`MenuValidator.cs:162~187`) 조합:

| 조건 | 메시지 |
|---|---|
| Main 틀림 | "메인 메뉴가 틀렸습니다!" |
| 완전 일치 | "완벽합니다!" |
| Side 개수 초과 | "사이드가 {N}개 더 들어있습니다." |
| Correct==Expected, 초과분 있음 | "정확하지만 {N}개가 더 들어있습니다." |
| Correct==0 (Expected>0) | "사이드 메뉴가 모두 틀렸습니다!" |
| 부분 매칭 | "사이드 {부족}개 부족" 또는 "사이드 {부족}개 부족, {잘못}개 잘못됨" |

---

## 6. 세션 통계 (CustomerSessionStats)

`Assets/Scripts/Domain/Cooking/CustomerSessionStats.cs`

| Field | 의미 |
|---|---|
| TotalOrders | 서빙 완료 주문 수 |
| PerfectOrders | 정확도 ≥ 1.0인 주문 수 |
| TotalAccuracyScore | 정확도 누계 (평균 계산용) |
| TotalEarnings | 세션 총 수익 |
| AverageAccuracy | 평균 정확도 |
| PerfectRatePercent | 완벽 주문 비율 (%) |

`CustomerManager.LogSessionSummary()`가 세션 종료 시 `Settlement.AddIncome(phaseLabel, TotalEarnings)`로 정산 서비스에 반영.

---

## 7. 배달 퀘스트 (Delivery)

`DeliveryTicketCoordinator.cs`가 담당. 자세한 배달 시스템은 별도 문서(TODO)에서 다루지만 요약:

- `DeliveryOrderService.GetOrders()`에서 `state == Ordered`인 주문만 티켓으로 변환.
- 각 티켓은 6번 슬롯에 세로 스택.
- `ValidateExactMatch()`가 도시락 조합 정확 일치 요구 (부분 매칭 불가).
- 서빙 성공 시 `MenuValidator.CalculateReward` 공식으로 보상 계산 (2026-07 이전엔 사이드만 합산해서 단품 quest 0G 버그 있었음).
- `DeliveryOrderService.MarkCookedWithPrice(questId, reward)` 호출.

### 7.1 활성 손님 제외 로직

`CustomerManager.CollectExcludedNpcIds()` (`CustomerManager.cs:204~231`):

1. 현재 활성 손님의 `npcId` 수집.
2. 활성 배달 주문(state != Delivered)의 `npcId` 수집.
3. 배달 questId가 `quest_<groupId>` 형식이면 그룹 전체 NPC 제외.

새 손님 스폰 시 이 집합에 포함된 npcId를 후보에서 제외 → 중복 손님 방지 + 퀘스트 진행 중인 NPC는 오지 않음.

---

## 8. 조기 종료 (EndEarly)

`CustomerManager.EndEarly()` (`CustomerManager.cs:298~325`):

- 스킵 버튼 등으로 영업 조기 종료.
- 현재 시점까지 벌어들인 금액은 그대로 반영.
- 모든 활성 손님/티켓 즉시 파괴 → `OnGameEnd` 즉시 발화.

---

## 9. 관련 파일

**핵심 스크립트**
- `Assets/Scripts/Unity/Cooking/CustomerManager.cs`
- `Assets/Scripts/Unity/Cooking/CustomerSpawner.cs`
- `Assets/Scripts/Unity/Cooking/CustomerLifecycle.cs`
- `Assets/Scripts/Unity/Cooking/OrderingCustomer.cs`
- `Assets/Scripts/Unity/Cooking/WaitingCustomer.cs`
- `Assets/Scripts/Unity/Cooking/TakingCustomer.cs`
- `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs`
- `Assets/Scripts/Unity/Cooking/OrderTicketController.cs`
- `Assets/Scripts/Unity/Cooking/DeliveryTicketCoordinator.cs`
- `Assets/Scripts/Unity/Common/TimeManager.cs`

**도메인/스키마**
- `Assets/Scripts/Domain/Common/MenuValidator.cs`
- `Assets/Scripts/Domain/Cooking/CustomerSessionStats.cs`
- `Assets/Scripts/Schema/State/Cooking/MenuSchema.cs`
- `Assets/Scripts/Schema/Config/Common/CustomerData.cs`

**데이터**
- `Assets/Bundles/ScriptableObjects/Customer/*.asset` (10 CustomerData)
- `Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv`
- `Assets/Bundles/driveAssets/dataTables/deliveryNPC.csv`
