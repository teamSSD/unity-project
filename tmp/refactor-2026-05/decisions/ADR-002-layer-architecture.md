# ADR-002: Layer Architecture — 3-Layer 유지 vs SO-Driven vs Hybrid

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Blocking Phase 2

## TL;DR (v2 결정 요약)

- 3-Layer 유지 (Schema → Domain → Unity 단방향)
- "Model = 순수 C#" → **"Model = MonoBehaviour 금지, Unity 값 타입(Vector3 등) 허용"** 로 완화
- **SO Event를 cross-cutting 알림의 기본 메커니즘으로 채택** (HUD 한정 → 도메인 이벤트 전반)
- SO Variables 비도입 (GameState로 충분)
- Schema asmdef 안에 Config(SO) / State(mutable POCO) 폴더 구분
- 컴파일 강제는 ADR-004의 asmdef 분리로 달성

## Context

DEV_PRINCIPLES.md v2.0의 선언:
- **3-Layer**: Behavior → Model → Schema (단방향)
- **Model은 순수 C# 클래스** (MonoBehaviour 금지)
- **Composition Root**

실제 코드:
- Entities/ 폴더 안에 MonoBehaviour와 순수 C# 혼재 (FoodModel 258라인은 MonoBehaviour, BentoModel은 MonoBehaviour, CookingToolModel 225라인은 MonoBehaviour)
- "Model = 순수 C#" 원칙이 실용성과 충돌 (Vector3, Sprite, Color 사용 필요)
- 강제 메커니즘 부재 (어셈블리 1개 → 위반이 컴파일 에러로 잡히지 않음)

본질적 질문: **3-Layer 원칙을 어떻게 강제할 것인가?** 강제 못 하면 원칙은 죽은 문서.

## Options

### A. DEV 원칙 v2.0 그대로 유지 (Model = MonoBehaviour 금지 + 순수 C#)
- "순수 C#" = UnityEngine.Object 의존 금지
- **장**: 가장 테스트 가능, 가장 명료
- **단**: Unity 타입(Vector3, Color, Sprite 참조) 못 씀 → 실제론 어김. 게임 데이터 모델에 비현실적

### B. 3-Layer 유지하되 "Model = MonoBehaviour 금지"만 강제 (Unity 타입 허용)
- Model 클래스가 Vector3/Color/Sprite 등은 써도 OK
- 단, MonoBehaviour/ScriptableObject/Object lifecycle에 의존 금지
- new로 인스턴스화 가능해야 함
- **장**: 실용적, 테스트 가능 (NewMonoBehaviourForTest 패턴 불필요), 게임 도메인에 적합
- **단**: DEV 원칙 v2.0 수정

### C. ScriptableObject-Driven Architecture (Ryan Hipple 스타일)
- 상태/이벤트/설정 모두 ScriptableObject 에셋
- 컴포넌트는 SO를 읽고 발행만 (전역 상태 분산)
- **장**: 디자이너 친화, 핫리로드, 씬 간 상태 공유 자동, 컴포넌트 간 결합 감소
- **단**: 패러다임 전환 비용, 작은 변수까지 에셋이 되는 부담, 상태 추적 어려움

### D. Hybrid: 3-Layer + SO를 cross-cutting에 활용 (★ 추천)
- 기본: B (3-Layer, Model에 MonoBehaviour 금지, Unity 타입 OK)
- ScriptableObject:
  - **데이터 정의**: CropData, CookingToolData, ShopConfig (이미 사용 중 — 확장)
  - **이벤트 채널**: 도메인 이벤트 발행/구독 (예: GameEvent SO)
  - **공유 변수**: 일부 글로벌 상태 (예: CurrentDaySO, CurrentPhaseSO) — `.Instance` 대체
- asmdef로 단방향 의존성 강제 (ADR-004와 직결)

## Recommendation: D (Hybrid)

이유:
- A는 비현실 (실제로 안 지켜지고 있음)
- B만으론 cross-cutting 통신 문제(.Instance 318회) 해결 못함
- C 전면 채택은 학습 곡선 크고 작은 변수까지 SO화하면 관리 폭증
- D는 B의 단순성 + C의 결합 감소를 부분 채택

### D 안의 구체화

#### 레이어 정의

| 레이어 | 허용 | 금지 |
|---|---|---|
| **Schema** | enum, struct, DTO, ScriptableObject 정의, 인터페이스 | MonoBehaviour, 비즈니스 로직, Unity 씬/리소스 접근 |
| **Domain** (구 Model) | 순수 C# 클래스, Unity 값 타입 (Vector3 등), Schema 참조 | MonoBehaviour, ScriptableObject 상속, Object lifecycle 의존 |
| **Unity** (구 Behavior) | MonoBehaviour, scene wiring, UI, Composition Root | Schema/Domain 역방향 의존 |

**핵심 변경**: "Model = 순수 C#" → "Model = MonoBehaviour 상속 금지, new 가능"

#### ScriptableObject 활용 패턴

**1. 데이터 정의 (이미 부분 사용)**
```csharp
[CreateAssetMenu(menuName = "Game/Cooking/FoodData")]
public class FoodData : ScriptableObject {
    public string id;
    public Sprite icon;
    public List<RecipeIngredient> ingredients;
    // ...
}
```

**2. 이벤트 채널 (신규)**
```csharp
[CreateAssetMenu(menuName = "Game/Events/GameEvent")]
public class GameEvent : ScriptableObject {
    private readonly List<Action> listeners = new();
    public void Raise() => listeners.ForEach(l => l.Invoke());
    public void Subscribe(Action l) => listeners.Add(l);
    public void Unsubscribe(Action l) => listeners.Remove(l);
}

[CreateAssetMenu(menuName = "Game/Events/IntEvent")]
public class IntEvent : ScriptableObject {
    public event Action<int> OnRaised;
    public void Raise(int value) => OnRaised?.Invoke(value);
}
```

사용:
```csharp
public class HUDMoneyDisplay : MonoBehaviour {
    [SerializeField] private IntEvent onMoneyChanged;  // 인스펙터로 SO 에셋 연결
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable() => onMoneyChanged.OnRaised += UpdateLabel;
    private void OnDisable() => onMoneyChanged.OnRaised -= UpdateLabel;
    private void UpdateLabel(int v) => label.text = $"${v}";
}
```

→ 매니저 `.Instance.GetMoney()` 폴링 대신 이벤트 구독, 결합 끊김

**3. 공유 변수 (선택적, 신규)**
```csharp
[CreateAssetMenu(menuName = "Game/Variables/IntVariable")]
public class IntVariable : ScriptableObject {
    [SerializeField] private int value;
    public int Value {
        get => value;
        set { if (this.value != value) { this.value = value; OnChanged?.Invoke(value); } }
    }
    public event Action<int> OnChanged;
}
```

- 사용 시점: `.Instance.GetX()` 패턴이 광범위해서 GameState로 흡수 어려운 일부 값
- 절제: 모든 변수를 SO화하지 말 것 (관리 부담)

#### asmdef에 의한 강제

ADR-004가 정의하는 3개 asmdef:
```
Game.Schema.asmdef    ← (다른 asmdef 참조 없음, 또는 표준 라이브러리만)
Game.Domain.asmdef    ← ref: Schema
Game.Unity.asmdef     ← ref: Schema, Domain
```

→ Domain 클래스가 MonoBehaviour 사용 시도하면 컴파일 에러 (UnityEngine 의존 자체는 Domain도 가능하나, MonoBehaviour 상속은 Game.Unity asmdef에서만)

→ Schema가 Domain을 참조하려 하면 컴파일 에러

→ **3-Layer 단방향이 컴파일러에 의해 강제됨**

#### 폴더 구조 (각 asmdef 내 — ADR-004 참조)
```
Assets/Scripts/
├── Schema/
│   ├── Schema.asmdef
│   ├── Cooking/  (FoodData, RecipeData, CookingToolData)
│   ├── Garden/   (CropData, FarmTileData)
│   ├── Shop/     (ShopConfig, UpgradeData)
│   ├── Common/   (PersistentData, GameState DTOs)
│   └── Events/   (GameEvent, IntEvent, ...)
├── Domain/
│   ├── Domain.asmdef  (ref Schema)
│   ├── Cooking/  (CookingService, FoodLogic, ...)
│   ├── Garden/   (FarmService, ...)
│   ├── Shop/     (UpgradeService, BuyService, ...)
│   └── Common/   (SaveLoadLogic, ...)
└── Unity/
    ├── Unity.asmdef  (ref Schema, Domain)
    ├── Cooking/  (CookingSceneController, RefrigeratorBehavior, ...)
    ├── Garden/   (GardenSceneController, ...)
    └── Common/   (GameSessionRoot, SceneCoordinator, ...)
```

## Consequences

### 긍정
- 3-Layer 위반이 컴파일러로 강제됨 (코드 리뷰에 의존하지 않음)
- 디자이너 친화 향상 (SO 데이터/이벤트 인스펙터 조작)
- 매니저 `.Instance` 폴링 → SO 이벤트 구독으로 결합 감소
- 테스트 가능성 향상 (Domain은 Unity 의존 거의 없음)
- DEV_PRINCIPLES.md v3.0 작성 명분 (현실 반영)

### 부정
- 모든 .cs 파일이 asmdef 폴더 중 하나로 이동 필요 → 큰 이동 작업
- using 문 일부 수정 (namespace 또는 reference 추가)
- 마이그레이션 중간 단계 빌드 깨질 위험 → 한 번에 PR로 처리해야 함
- ScriptableObject 에셋 관리 부담 증가 (특히 GameEvent들)

### 마이그레이션 전략
1. 어셈블리 구조 결정 (ADR-004) → 폴더 이동 PR 1개 (큰 PR)
2. 이동 후 컴파일 통과 → 첫 위반 잡힘 (Schema에서 Domain 참조 등)
3. 위반 해결 → DEV_PRINCIPLES.md v3.0 작성
4. ScriptableObject 패턴 도입은 점진적 (필요한 곳부터)

## Open Questions

1. **ScriptableObject Event를 도입할 영역 우선순위** — **RESOLVED**:
   - **SO Event를 처음부터 cross-cutting 알림의 기본 메커니즘으로 채택**
   - Phase 2-B에서 도메인 이벤트 카탈로그를 일괄 정의:
     - HUD: OnMoneyChanged, OnStaminaChanged, OnTimeChanged, OnDayChanged, OnPhaseChanged
     - 도메인: OnDayPassed, OnPhaseStarted, OnOrderCompleted, OnBentoAssembled, OnFoodPurchased, OnUpgradeApplied, ...
   - Service 분해 시 자연 사용 (매니저 알림 패턴이 잔존하지 않음)

2. **공유 변수 SO (IntVariable, BoolVariable 등) 도입 여부**?
   - 도입 시: 매니저 `.Instance.Get*()` 일부 대체 가능
   - 비도입 시: GameState POCO + 이벤트로 충분
   - **임시 추천**: 비도입. GameState로 시작, 명백한 필요 발생 시 도입.

3. **Schema에 ScriptableObject가 들어가도 되는가**?
   - Yes: 데이터 정의는 Schema에 두는 게 자연. 그러나 Schema asmdef가 UnityEngine 참조 → Domain도 결국 UnityEngine 의존.
   - 권장: Schema asmdef는 UnityEngine 참조 허용 (ScriptableObject 정의 위해), but MonoBehaviour 금지.

4. **Domain 클래스가 Unity 값 타입(Vector3, Color, Sprite reference) 사용 시 테스트 어려움**?
   - 대부분 값 타입 (Vector3, Color)은 new 가능 → 테스트 OK
   - Sprite reference는 mock 필요 → Domain에서 직접 사용하지 말고 ID/key로 다루는 게 좋음
   - **임시 추천**: Sprite는 Schema(SO)에서만, Domain은 ID 키로 참조

## Decision Required

- [ ] **D (Hybrid) 채택 동의** (또는 A/B/C 또는 수정)
- [ ] "Model = 순수 C#" → "Model = MonoBehaviour 금지, Unity 값 타입 허용"로 완화 동의
- [ ] ScriptableObject Event 도입 — 우선 영역 (Open Q1)
- [ ] 공유 변수 SO 도입 여부 (Open Q2)
- [ ] Schema에 SO 정의 허용 여부 (Open Q3)
- [ ] DEV_PRINCIPLES.md v3.0 작성 동의
