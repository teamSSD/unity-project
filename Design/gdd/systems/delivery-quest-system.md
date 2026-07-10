# 배달 퀘스트 시스템 (Delivery Quest)

**배달 퀘스트**는 Mall 씬에서 특정 NPC(또는 NPC 그룹)와의 대화 → 주문 수락 → Cooking 씬에서 조리 → Mall에서 배달 완료 → 보상 수령의 5단계 흐름이다. 정규 손님(랜덤 대기열)과는 별개의 스토리 서사 축이며, 각 그룹은 고정 대사(FirstMeet / QuestStart / Ordering / OrderEnd)와 고정 메뉴 템플릿(`deliveryQuest.csv`)을 가진다.

## 핵심 컴포넌트 지도

| 계층 | 파일 | 책임 |
|---|---|---|
| Config Enum | `Assets/Scripts/Schema/Config/Mall/DeliveryQuestStage.cs` | `FirstMeet → QuestStart → Ordering → OrderEnd → Completed` 6단계 (Normal은 Completed 이후 일상 대화용). |
| Config SO | `Assets/Scripts/Schema/Config/Mall/DeliveryNpcData.cs` | NPC 인스펙터 데이터 (`id`, `characterName`, `sprite`, `state`, `groupId`, `prerequisiteGroupId`). |
| Config SO | `Assets/Scripts/Schema/Config/Mall/DeliveryDialogueConfig.cs` | groupId별 5개 DialogueSO 슬롯 (`firstMeet` / `normal` / `questStart` / `ordering` / `orderEnd`). |
| State | `Assets/Scripts/Schema/State/Mall/MallPersistent.cs` | 진행도 parallel List (`questGroupIds` / `questStages`). |
| State | `Assets/Scripts/Schema/State/Mall/DeliveryOrderData.cs` | 활성 주문 (`questId`, `menuSchema`, `state`, `npcId`, `cookedPrice`). |
| Service (POCO) | `Assets/Scripts/Domain/Mall/DeliveryQuestService.cs` | groupId의 stage GET/SET. 미등록 groupId는 `FirstMeet` 반환. |
| Service (POCO) | `Assets/Scripts/Domain/Mall/OrderService.cs` | 주문 CRUD, `MarkCookedWithPrice`, `ConsumeBento`. |
| Catalog | `Assets/Scripts/Domain/Mall/QuestMenuCatalog.cs` | groupId → `MenuSchema` 템플릿 (`deliveryQuest.csv` 파싱, 세션 시작 시 빌드). |
| Catalog SO | `Assets/Scripts/Schema/Catalog/DialogueConfigCatalogSO.cs` | groupId → `DeliveryDialogueConfig` 매핑. |
| Interaction | `Assets/Scripts/Unity/Mall/DeliveryNpcView.cs` | 인스펙터 데이터를 읽어 sprite/state 적용, state에 맞는 INpcInteraction 컴포넌트 동적 추가. |
| Interaction | `Assets/Scripts/Unity/Mall/DeliveryNpcDialogueInteraction.cs` (+`.Quest.cs` partial) | 스페이스 감지, 단계 판정, `DialogueManager`에 대화 전달, 단계 진행. |
| Interaction | `Assets/Scripts/Unity/Mall/DeliveryNpcOrderInteraction.cs` | (레거시) 즉시 주문 흐름. 현재 프로덕션 흐름은 Dialogue interaction 경유. |
| Interaction | `Assets/Scripts/Unity/Mall/DeliveryNpcReceiptInteraction.cs` | 요리 완성 후 NPC가 도시락을 수령하는 상태 (`WaitingReceipt` state 전용). |
| Cooking | `Assets/Scripts/Unity/Cooking/DeliveryTicketCoordinator.cs` | 요리대에 배달 티켓(주문서) 생성, `onTake` 시 `MenuValidator.CalculateReward`로 보상 계산. |
| Domain | `Assets/Scripts/Domain/Common/MenuValidator.cs` | `CalculateReward(order, main, sides)` 매출 공식. |

## 단계 상태머신 (DeliveryQuestStage)

```
FirstMeet ──(대화 종료)──▶ QuestStart
QuestStart ──(choice.resultTag="accept")──▶ Ordering [+ 주문 생성 + 레시피 해금]
QuestStart ──(choice.resultTag="defer")──▶ QuestStart 유지
Ordering ──(요리 완성 → OrderService.state=Cooked → 재대화)──▶ OrderEnd [+ ConsumeBento]
OrderEnd ──(대화 종료)──▶ Completed
Completed ──(재대화)──▶ Normal 사이클 (NpcNormalDialogueCatalog 기반, 그룹 종결)
Normal ──(재대화)──▶ Normal 사이클 유지 (일상 대화)
```

- **stage 저장**: `MallPersistent.questGroupIds[]` / `questStages[]` parallel list, `gamedata.json`에 직렬화.
- **stage 캐시 초기값**: 서비스가 미등록 groupId에 대해 `FirstMeet` 반환 → 새 NPC/새 플레이어는 자연스럽게 FirstMeet부터.
- **prerequisiteGroupId**: 잠금 조건. 지정된 그룹이 `Completed`가 아니면 `IsQuestUnlocked()==false` → 인사말 없이 일상 대화(Normal 사이클)만 재생.

### 단계별 진행 세부

`DeliveryNpcDialogueInteraction.Quest.cs`의 `AdvanceQuestStage(resultTag)`:

```csharp
FirstMeet    → QuestStart  (대화 끝나면 무조건 다음, Normal 단계 건너뜀)
Normal/Completed → AdvanceNormalCycle() (일상 사이클 인덱스만 진행, 다른 단계 전이 X)
QuestStart   → resultTag == "accept" ? CreateQuestOrder() + Ordering 로 이동 : 유지
Ordering     → (여기선 no-op; Cooked 판정은 StartDialogue()에서 처리)
OrderEnd     → Completed
```

`StartDialogue()`에서 Ordering 진입 시 훅:

```csharp
if (stage == Ordering) {
    var order = Order.GetOrder($"quest_{groupId}");
    if (order != null && order.state == Cooked) {
        Order.ConsumeBento(qId);            // 매출 지급
        DeliveryQuest.SetStage(groupId, OrderEnd);
        stage = OrderEnd;                    // 즉시 OrderEnd 대사 재생
    }
}
```

## 주문 생성 (CreateQuestOrder)

`DeliveryNpcDialogueInteraction.Quest.cs`:

```csharp
var template = GameSessionRoot.Instance?.QuestMenus.GetByGroupId(groupId); // deliveryQuest.csv 캐시
var menu = new MenuSchema(template.name, orderCount + 1, template.mainMenu, template.sideMenus);
string questId = $"quest_{groupId}";
orderSvc.GenerateOrder(menu, questId, npcId);
UnlockMenuRecipes(menu);   // menu의 모든 main/side 레시피 해금 (UnlockedFoodService)
Instantiate(receiptPrefab); // 씬에 영수증(주문 카드) 표시 → 유저가 조리대로 옮김
```

`questId` 포맷은 `quest_<groupId>` 고정 → 같은 그룹 재수주 방지(같은 키 덮어씀).

## Cooking 씬에서의 배달 처리 (DeliveryTicketCoordinator)

Cooking 씬 진입 시 `OrderService.GetOrders()`의 `Ordered` 상태 주문을 모두 티켓으로 변환.

```csharp
foreach (var order in orderSvc.GetOrders()) {
    if (order.state != Ordered) continue;
    var ticket = ticketController.CreateDeliveryTicket(order.menuSchema);
    ticket.IsDelivery = true;
    ticket.QuestId = order.questId;
    ticket.onTake += (main, sides, pos) => {
        int reward = MenuValidator.CalculateReward(order.menuSchema, main, sides);
        Order.MarkCookedWithPrice(order.questId, reward);   // 상태 Cooked, 실제 지급은 배달 시
    };
}
```

핸들러는 Dictionary에 캡처해 `OnDestroy`에서 `-=` 로 해제 (누수 방지).

## 매출 공식 (MenuValidator.CalculateReward) — production fix

```
총가격 = 제공된 메인.Price + Σ 제공된 사이드.Price      (개별 페널티 없음)
메인 배율 = 요청.mainMenu.id == 제공.foodData.id ? 1.0 : 0.7
사이드 배율 = 1.0 + (일치 사이드 개수 × 0.15)          (2026-07 튜닝: 5% → 15%)
보상 = round(총가격 × 메인 배율 × 사이드 배율)
```

**production fix 배경**: 이전 배달 티켓은 사이드 가격만 합산 → 단품 quest(사이드 0개)는 0G 배달, 절반 이상의 quest에서 무보상 버그. 지금은 정규 손님(`CustomerLifecycle`)과 동일한 공식을 사용해 `main + sides` 모두 반영.

## 배달 완료 흐름 (요약)

1. **Preparation/Afternoon** — 유저가 Mall에서 NPC와 대화 (`FirstMeet` → `QuestStart`).
2. **QuestStart accept 선택** — `Ordering` 진입, 씬에 영수증(주문서) 소환, 레시피 해금.
3. **유저가 영수증을 요리대로 이동** — 다음 Cooking 페이즈 진입 시 `DeliveryTicketCoordinator`가 티켓 변환.
4. **요리 완료 (bento에 담고 영수증 부착)** — `onTake` 발화 → `MarkCookedWithPrice`로 상태 Cooked + `cookedPrice` 저장.
5. **Mall로 돌아와 같은 NPC와 재대화** — `StartDialogue` 훅이 Cooked 감지 → `ConsumeBento(questId)` (실지급 + Delivered) → `OrderEnd` 대사.
6. **OrderEnd 대화 종료** — `Completed` 로 전이. 재대화 시 `Normal` 사이클만 표시.

## 재수주 정책

- 같은 groupId의 `questId`가 이미 있으면 `GenerateOrder`가 덮어쓰지 않고 List에 추가 (신규 인스턴스). 다만 `CreateQuestOrder`는 상태가 `QuestStart`일 때만 호출되므로 `Ordering→Completed` 순환이 끝나기 전에는 다시 트리거되지 않는다.
- `Completed` 상태 그룹은 `AdvanceQuestStage`가 Normal 사이클만 진행 → **재수주 없음** (현재 프로덕션 정책: 각 그룹은 1회 완료).
- 개발자 리셋: `DeliveryNpcDialogueInteraction.ResetAll()` → `DeliveryQuestService.Clear()` → 모든 groupId 상태 삭제 → 다음 대화 시 다시 FirstMeet부터.

## NPC 상태(DeliveryNpcState)와 인터랙션 스위칭

`Assets/Scripts/Schema/Config/Mall/DeliveryNpcState.cs`:

```csharp
public enum DeliveryNpcState { Orderable, WaitingReceipt, Completed }
```

`DeliveryNpcView.ApplyState(state)`가 state에 맞게 컴포넌트 교체:

| state | Sprite 색 | 추가되는 인터랙션 |
|---|---|---|
| `Orderable` | Color.white | `groupId`가 있으면 `DeliveryNpcDialogueInteraction`, 없으면 `CasualNpcInteraction`. |
| `WaitingReceipt` | Color.yellow | `DeliveryNpcReceiptInteraction` (Cooked 주문 수령 → 매출 지급 → NPC destroy). |
| `Completed` | Color.gray | 인터랙션 없음. |

현재 프로덕션 흐름에선 `Orderable` 상태 + `DeliveryNpcDialogueInteraction`가 대화 안에서 Cooked 감지·수령·매출 지급까지 수행 → `WaitingReceipt`는 실제로는 거의 사용되지 않음 (레거시 경로).

## 참고: DeliveryNpcOrderInteraction (레거시)

`Assets/Scripts/Unity/Mall/DeliveryNpcOrderInteraction.cs`는 이전 세대의 즉시 주문 흐름(대화 없이 클릭 한 번으로 주문 생성). `임시정식 A` 하드코딩 메뉴가 남아 있음. 현재 프로덕션은 `DeliveryNpcDialogueInteraction`가 대신하고 있으며, 이 파일은 시스템에 매달려 있지 않다 — Cleanup 후보.
