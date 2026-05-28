# Phase 2-B: SO Event 카탈로그 + 도메인 이벤트 도입

## 목표

ScriptableObject 기반 이벤트 시스템 도입 + 핵심 도메인 이벤트 카탈로그 12개 일괄 정의. 본 sub-phase에서는 **인프라만 도입**하고 호출자/구독자 연결은 Phase 3에서 점진.

## 가설

매니저 `.Instance` 직접 호출 318회 중 다수가 "X에게 알려야 한다" 패턴. SO Event를 cross-cutting 알림의 기본 메커니즘으로 채택하면:
- N×M 결합이 N+M으로 감소
- 디자이너가 인스펙터로 구독 연결 (코드 수정 없이)
- Phase 3 매니저 → Service 분해 시 새 호출 패턴이 처음부터 깔끔

## 측정 기준

| 지표 | 베이스라인 | 목표 (본 sub-phase 후) |
|---|---:|---:|
| `GameEventBase` 추상 클래스 | 0 | 1 |
| 구체 이벤트 베이스 (Void/Int/Float/String/...) | 0 | 5-7 |
| 도메인 이벤트 SO 에셋 수 | 0 | 12 |
| SO Event using 파일 | 0 | (구독자 미연결, Phase 3에서 증가) |
| `.Instance` 호출 | 318 | 318 (변화 없음) |

## 검증 방법

1. 컴파일 통과
2. **단위 테스트** (필수):
   - `GameEvent.Raise()` 호출 시 구독자 모두 호출됨
   - `Subscribe()` / `Unsubscribe()` 정상
   - Unsubscribe 후 GC 가능 (메모리 누수 없음)
   - `IntEvent`, `FloatEvent` 등 타입별 동작
3. **인스펙터 확인**: 카탈로그 12개 에셋이 인스펙터에 표시되고 description 필드 채워짐
4. **회귀**: 기존 게임 흐름 그대로 (구독자 미연결, 발행도 없음)

## 회귀 방지 체크리스트

- [ ] 기존 매니저 호출 패턴 변경 안 함
- [ ] 새 SO Event는 발행자 없이 컴파일 (잠재 사용)
- [ ] 메모리 누수 테스트 (Subscribe → Unsubscribe → WeakReference)
- [ ] Phase 3에서 점진 사용 (본 sub-phase에서 .Instance 제거 안 함)

## 추정 시간

| 단계 | 시간 |
|---|---|
| `GameEventBase` + 5-7개 구체 클래스 작성 | 1일 |
| 단위 테스트 작성 + GREEN | 0.5일 |
| 도메인 이벤트 카탈로그 12개 에셋 생성 | 0.5-1일 |
| 사용 가이드 docs | 0.5일 |
| 사용자 검토 + 조정 | 0.5일 |
| **합계** | **3-5일** |

## 선결 조건

- [x] Phase 2-A 완료 (Schema/Events/ 폴더 존재)
- [x] ADR-002 ACCEPTED
- [ ] 카탈로그 12개 사용자 검토 + 승인

## PR 분할

**1 PR**:
- (a) `Schema/Events/GameEventBase.cs` + 구체 클래스 5-7개
- (b) `Schema/Events/Catalog/` 안에 SO 에셋 12개
- (c) `Schema/Events/Editor/` 안에 GameEvent 인스펙터 (옵션 — Raise 버튼)
- (d) `Tests/EditMode/SoEventTests.cs`
- (e) `Schema/Events/README.md` (사용 가이드)

## 세부 작업

### 1. 베이스 클래스 (`Schema/Events/`)

```csharp
// Schema/Events/GameEventBase.cs
using UnityEngine;

namespace Game.Schema.Events {
    public abstract class GameEventBase : ScriptableObject {
        [TextArea(2, 4)]
        public string description;
    }
}

// Schema/Events/GameEvent.cs (void)
public class GameEvent : GameEventBase {
    private readonly List<Action> _listeners = new();
    public void Raise() {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i]?.Invoke();
    }
    public void Subscribe(Action l) {
        if (l != null && !_listeners.Contains(l)) _listeners.Add(l);
    }
    public void Unsubscribe(Action l) => _listeners.Remove(l);
}

// Schema/Events/IntEvent.cs
public class IntEvent : GameEventBase {
    public event Action<int> Raised;
    public void Raise(int v) => Raised?.Invoke(v);
}

// 동일 패턴: FloatEvent, StringEvent
```

### 2. 도메인별 이벤트 (struct payload)

```csharp
// Schema/Events/Payloads/OrderCompletedPayload.cs
public struct OrderCompletedPayload {
    public int customerId;
    public ValidationResult validation;
    public int reward;
}

// Schema/Events/OrderCompletedEvent.cs
public class OrderCompletedEvent : GameEventBase {
    public event Action<OrderCompletedPayload> Raised;
    public void Raise(OrderCompletedPayload p) => Raised?.Invoke(p);
}
```

### 3. 도메인 이벤트 카탈로그 (12개)

| 에셋 이름 | 타입 | 발행 시점 | 구독 후보 (Phase 3에서 연결) |
|---|---|---|---|
| **OnDayPassed** | GameEvent | PassDay() 후 | HUD DayDisplay, Save trigger, Tutorial |
| **OnPhaseStarted** | IntEvent (phase) | 새 Phase 진입 | HUD PhaseIndicator, BGM Switcher, Action UI |
| **OnMoneyChanged** | IntEvent (new money) | StatsSystem.SetMoney | HUD MoneyDisplay |
| **OnStaminaChanged** | IntEvent | StatsSystem.SetStamina | HUD StaminaGauge |
| **OnTimeChanged** | FloatEvent | TimeManager tick | HUD ClockUI |
| **OnOrderCompleted** | OrderCompletedEvent | CustomerLifecycle 완료 | Statistics, Settlement, Validation UI |
| **OnFoodPurchased** | FoodPurchasedEvent | BuyService 구매 | Inventory UI, Stats |
| **OnUpgradeApplied** | UpgradeAppliedEvent | UpgradeService.TryUpgrade 성공 | Storage/Tool/Farm UI |
| **OnSettlementComputed** | SettlementEvent | SettlementService.Compute | Settlement UI |
| **OnSceneTransitionRequested** | StringEvent (scene) | UI/NPC 상호작용 | SceneCoordinator |
| **OnBentoAssembled** | BentoEvent | CookingService 메인/사이드 변경 | Bento UI |
| **OnDeliveryAccepted** | DeliveryEvent | DeliveryNpc 상호작용 | OrderManager, Quest UI |

각 에셋은 `Schema/Events/Catalog/{Name}.asset`으로 생성. 인스펙터 description에 발행 시점 + 구독 가이드 명시.

### 4. 단위 테스트 (`Tests/EditMode/SoEventTests.cs`)

```csharp
[Test]
public void GameEvent_Raise_NotifiesAllListeners() {
    var evt = ScriptableObject.CreateInstance<GameEvent>();
    int callCount = 0;
    Action handler = () => callCount++;
    evt.Subscribe(handler);
    evt.Subscribe(handler);  // 중복 방지 테스트
    evt.Raise();
    Assert.AreEqual(1, callCount);  // 중복 방지로 1번만
}

[Test]
public void GameEvent_Unsubscribe_RemovesListener() {
    var evt = ScriptableObject.CreateInstance<GameEvent>();
    bool called = false;
    Action handler = () => called = true;
    evt.Subscribe(handler);
    evt.Unsubscribe(handler);
    evt.Raise();
    Assert.IsFalse(called);
}

[Test]
public void IntEvent_RaisesWithValue() {
    var evt = ScriptableObject.CreateInstance<IntEvent>();
    int received = -1;
    evt.Raised += v => received = v;
    evt.Raise(42);
    Assert.AreEqual(42, received);
}

[Test]
public void GameEvent_RaiseDuringIteration_Safe() {
    var evt = ScriptableObject.CreateInstance<GameEvent>();
    int callCount = 0;
    Action handler = null;
    handler = () => {
        callCount++;
        if (callCount < 3) evt.Unsubscribe(handler);
    };
    evt.Subscribe(handler);
    evt.Raise();
    Assert.AreEqual(1, callCount);  // Unsubscribe 후 재호출 안 됨
}
```

### 5. 인스펙터 (옵션, Editor 폴더)

```csharp
#if UNITY_EDITOR
[CustomEditor(typeof(GameEvent))]
public class GameEventEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        var evt = (GameEvent)target;
        if (GUILayout.Button("Raise")) evt.Raise();  // 테스트용
    }
}
#endif
```

### 6. 사용 가이드 (`Schema/Events/README.md`)

- 새 이벤트 추가 절차
- Subscribe / Unsubscribe 패턴 (OnEnable / OnDisable)
- 람다 사용 금지 (구독 해제 불가) — 인스턴스 메서드 권장
- 메모리 누수 방지
- 발행 책임은 Service (Domain) 측, 구독은 Unity Adapter
