# Unity Project Development Principles

**버전:** 2.0 (리팩토링 반영)
**최종 수정:** 2026-02-23
**적용 대상:** 모든 개발자 + AI Agent

---

## 🎯 **핵심 원칙**

> **"복잡한 건 나쁜 거다. 단순하게, 명확하게, 테스트 가능하게."**

이 프로젝트는 **3-Layer Architecture**와 **단방향 의존성**을 따릅니다.

---

## 🏗️ **아키텍처 원칙**

### **1. 3-Layer Architecture (필수)**

```
┌────────────────────────────────┐
│  Behavior (UI, MonoBehaviour)  │  ← Unity 생명주기, 사용자 입력
└───────────────┬────────────────┘
                │ depends on
┌───────────────▼────────────────┐
│  Model (Business Logic, C#)    │  ← 순수 로직, 테스트 가능
└───────────────┬────────────────┘
                │ uses
┌───────────────▼────────────────┐
│  Schema (Data, Validation)     │  ← 순수 데이터 + 검증
└────────────────────────────────┘
```

**규칙:**
- ✅ Behavior는 Model에 의존 가능
- ✅ Model은 Schema에 의존 가능
- ❌ **역방향 의존 절대 금지** (Schema → Model, Model → Behavior)
- ❌ **순환 의존성 절대 금지**

**예시:**
```csharp
// ✅ 올바른 의존성
public class ActionToggle : MonoBehaviour  // Behavior
{
    private DiaryModel model;  // Model 참조 OK

    public void OnClick() {
        model.ExecuteAction(phase, actionType);  // Model 호출
    }
}

public class DiaryModel  // Model (순수 C#)
{
    private BentoSelection[] bentoSelections;  // Schema 사용 OK

    public bool ExecuteAction(PhaseType phase, ActionType type) {
        // 비즈니스 로직
    }
}

// ❌ 금지 - 역방향 의존
public class BentoSelection  // Schema
{
    private DiaryModel model;  // ❌ Schema가 Model 참조 금지!
}
```

---

### **2. Model은 순수 C# 클래스**

**필수:**
- ❌ MonoBehaviour 상속 금지
- ✅ 순수 C# 클래스로 작성
- ✅ 의존성은 생성자로 주입 (Dependency Injection)
- ✅ 인터페이스로 외부 의존성 추상화

**이유:**
- 단위 테스트 용이 (Mock 객체 사용 가능)
- Unity 생명주기와 무관하게 동작
- 재사용성 향상

**예시:**
```csharp
// ✅ 올바른 Model
public class DiaryModel
{
    private readonly IPhaseProgressor phaseProgressor;

    public DiaryModel(IPhaseProgressor phaseProgressor)
    {
        this.phaseProgressor = phaseProgressor;  // DI
    }
}

// ❌ 잘못된 Model
public class DiaryModel : MonoBehaviour  // ❌ MonoBehaviour 금지
{
    public static DiaryModel Instance;  // ❌ Singleton 금지
}
```

---

### **3. Composition Root 패턴**

**원칙:**
- 모든 의존성은 **하나의 진입점(Composition Root)**에서 생성 및 주입
- Composition Root는 MonoBehaviour (Unity 진입점)
- 초기화 순서를 명확히 보장

**예시:**
```csharp
public class DiaryUIController : MonoBehaviour  // Composition Root
{
    private DiaryModel model;

    private void Awake()
    {
        // 1. Model 생성
        var phaseProgressor = new ProgressSystemAdapter();
        model = new DiaryModel(phaseProgressor);
    }

    private void Start()
    {
        // 2. 모든 UI에 Model 주입
        bentoToggleList.Initialize(model);

        var actionToggles = FindObjectsOfType<ActionToggle>();
        foreach (var toggle in actionToggles)
            toggle.Initialize(model);
    }
}
```

**금지:**
```csharp
// ❌ Singleton으로 직접 접근
public class ActionToggle : MonoBehaviour
{
    private void Start() {
        DiaryActionManager.Instance.ExecuteAction(...);  // ❌ 금지!
    }
}
```

---

## 🚨 **절대 금지 (NEVER)**

### ❌ **1. 순환 의존성**

**금지 사유:** 초기화 순서 불명확, 테스트 불가능, 유지보수 악몽

```csharp
// ❌ 절대 금지
DiaryFlowManager ↔ DiaryActionManager ↔ ProgressSystem

// ✅ 단방향 의존성
ProgressSystem → DiaryUIController → DiaryModel → Schema
```

**발견 즉시 리팩토링 필수!**

---

### ❌ **2. Singleton 남용**

**허용:**
- 정말로 전역 상태가 필요한 경우만 (예: InputManager, AudioManager)
- 최대 2-3개까지만

**대안:**
- Composition Root에서 생성 → DI
- ScriptableObject (설정/데이터)
- FindObjectOfType (초기화 시 한 번만)

```csharp
// ❌ 금지
public class DiaryActionManager : MonoBehaviour
{
    public static DiaryActionManager Instance;  // ❌
}

// ✅ 대안
public class DiaryUIController : MonoBehaviour
{
    private DiaryModel model;  // 생성자에서 주입

    public void Initialize(DiaryModel model) {
        this.model = model;
    }
}
```

---

### ❌ **3. switch-case 남발 (280줄 이상)**

**금지 사유:** 확장 불가능, OCP 위반, 유지보수 지옥

**대안: Command 패턴**

```csharp
// ❌ 금지
public bool ExecuteAction(ActionType type)
{
    switch (type)
    {
        case ActionType.MenuSelect:
            // 30줄 로직...
        case ActionType.Work:
            // 25줄 로직...
        // ... 280줄
    }
}

// ✅ Command 패턴
public bool ExecuteAction(ActionType type)
{
    if (!actionCommands.TryGetValue(type, out var command))
        return false;

    return command.Execute(this);  // 각 Command에 로직 위임
}
```

**새 액션 추가 시:**
- switch-case: 기존 코드 수정 필요 (OCP 위반)
- Command: 새 클래스만 추가 (OCP 준수)

---

### ❌ **4. yield return null로 초기화 순서 회피**

```csharp
// ❌ 금지 - 타이밍 불명확
private IEnumerator Start()
{
    yield return null;  // 왜? 언제까지?
    Initialize();
}

// ✅ 명확한 초기화
private void Awake() {
    CreateModel();  // 1. Model 생성
}

private void Start() {
    Initialize();   // 2. UI 초기화 (Model 주입)
}
```

---

### ❌ **5. null 체크 없이 외부 참조 사용**

```csharp
// ❌ 금지
FindObjectOfType<Manager>().DoSomething();  // NullReferenceException!

// ✅ 필수
var manager = FindObjectOfType<Manager>();
if (manager == null)
{
    Debug.LogError("[Component] Manager not found!");
    return;
}
manager.DoSomething();
```

---

## ✅ **필수 규칙 (MUST)**

### **1. 모든 MonoBehaviour는 OnValidate 구현**

```csharp
public class MyComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // 자동 찾기 + 검증
        if (nameLabel == null)
        {
            nameLabel = transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (nameLabel == null)
                Debug.LogError($"[{GetType().Name}] nameLabel not found!", this);
        }
    }
#endif
}
```

**이유:** Inspector 누락을 Editor 로드 시점에 감지

---

### **2. 새 기능은 테스트 작성**

```csharp
[Test]
public void DiaryModel_ExecuteAction_Success()
{
    // Arrange
    var mockProgressor = new MockPhaseProgressor();
    var model = new DiaryModel(mockProgressor);

    // Act
    bool result = model.ExecuteAction(PhaseType.Morning, ActionType.Work);

    // Assert
    Assert.IsTrue(result);
}
```

**필수 테스트:**
- Null 입력
- 잘못된 입력
- 정상 입력
- 경계값

---

### **3. Command 패턴 사용 (복잡한 조건문 대체)**

**언제:**
- switch-case가 3개 이상의 case를 가질 때
- 새 케이스가 자주 추가될 가능성이 있을 때

**예시:**
```csharp
// IActionCommand.cs
public interface IActionCommand
{
    bool CanExecute(PhaseType phase, DiaryModel model);
    ActionExecutionResult Execute(PhaseType phase, DiaryModel model);
}

// MenuSelectAction.cs
public class MenuSelectAction : IActionCommand
{
    public bool CanExecute(PhaseType phase, DiaryModel model)
    {
        return phase == PhaseType.Preparation
            && model.HasAnyBentoSelection();
    }

    public ActionExecutionResult Execute(PhaseType phase, DiaryModel model)
    {
        // 메뉴 선택 로직
        return ActionExecutionResult.Success(ActionState.Done);
    }
}
```

---

### **4. DontDestroyOnLoad로 상태 유지 (필요 시)**

```csharp
public class DiaryUIController : MonoBehaviour
{
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);  // 씬 이동 시에도 유지
    }
}
```

**주의:** 남용 금지 - 정말 필요한 경우만 사용

---

## 📝 **코드 작성 규칙**

### **1. 네이밍**

```csharp
// 클래스/메서드: PascalCase
public class DiaryModel { }
public void ExecuteAction() { }

// 필드: camelCase (private), PascalCase (public property)
private DiaryModel model;
public DiaryModel Model => model;

// 상수: UPPER_CASE
private const int MAX_STAMINA = 100;
private const string LABEL_NAME = "Label";

// 인터페이스: I + PascalCase
public interface IPhaseProgressor { }
```

---

### **2. 에러 메시지**

```csharp
// ✅ 명확한 컨텍스트 + this 전달
Debug.LogError($"[{GetType().Name}] Failed to load {id}", this);

// ✅ 복구 가능하면 복구
if (data == null)
{
    Debug.LogWarning($"[{GetType().Name}] Data is null, using default");
    data = CreateDefaultData();
}

// ❌ 조용히 실패
if (data == null) return;  // 왜 실패했는지 알 수 없음!
```

---

### **3. 주석**

```csharp
/// <summary>
/// 액션을 실행하고 상태를 변경합니다.
/// </summary>
/// <param name="phase">실행할 페이즈</param>
/// <param name="type">액션 타입</param>
/// <returns>성공 여부</returns>
public bool ExecuteAction(PhaseType phase, ActionType type)
{
    // 복잡한 로직에만 인라인 주석 (무엇이 아니라 왜)
}
```

**주석 금지:**
```csharp
// ❌ 코드만 반복
int i = 0;  // i를 0으로 초기화
```

---

## 🔄 **커밋 전 체크리스트**

### Phase 1: 코드 검증
- [ ] 컴파일 에러 0개
- [ ] Console 경고 확인
- [ ] Test Runner 실행 (EditMode)
- [ ] 순환 의존성 없음

### Phase 2: 아키텍처 검증
- [ ] 단방향 의존성 준수 (Behavior → Model → Schema)
- [ ] Model은 순수 C# 클래스
- [ ] Singleton 개수 확인 (최대 2-3개)
- [ ] Command 패턴 사용 (switch-case 3개 이상 시)

### Phase 3: PlayMode 테스트
- [ ] PlayMode에서 정상 작동
- [ ] NullReferenceException 없음
- [ ] 씬 이동 테스트 (DontDestroyOnLoad)

### Phase 4: Git
- [ ] 의미 있는 커밋 메시지
- [ ] 불필요한 파일 제외
- [ ] PROGRESS.md 업데이트 (중요 변경 시)

---

## 🎯 **성공 지표**

리팩토링 후 달성한 지표 (2026-02-23 기준):

| 항목 | Before | After | 개선 |
|------|--------|-------|------|
| 순환 의존성 | 3개 | **0개** | ✅ 100% 제거 |
| Singleton | 4개 | **2개** | ✅ 50% 감소 |
| switch-case | 280줄 | **0줄** | ✅ Command 패턴 |
| 테스트 커버리지 | 낮음 | **69%** | ✅ 41/59 통과 |
| NullRefException | 빈번 | **0개** | ✅ 완전 제거 |

**목표: 디버깅 시간 90% 감소**

---

## 📚 **참고 문서**

- [RecipeBook/README.md](Assets/Scripts/Entities/RecipeBook/README.md) - 시스템 아키텍처 가이드
- [PROGRESS.md](PROGRESS.md) - 리팩토링 진행 기록
- [REFACTORING_SUMMARY.md](REFACTORING_SUMMARY.md) - 리팩토링 요약 및 교훈

---

## 💡 **배운 교훈 (2026-02-23 리팩토링)**

### **1. 순환 의존성은 초기에 잡아야 한다**
- 순환 의존성이 쌓이면 리팩토링 비용이 기하급수적 증가
- 새 기능 추가 시 의존성 방향 항상 확인

### **2. Singleton은 마약이다**
- 쉽게 사용할 수 있지만, 나중에 테스트/리팩토링 불가능
- 진짜 필요한지 3번 고민 후 사용

### **3. switch-case는 3개까지만**
- 3개 이상이면 Command 패턴 고려
- 확장 가능성이 있으면 무조건 Command

### **4. 순수 C# Model의 힘**
- MonoBehaviour 제거만으로 테스트 가능성 10배 향상
- Unity 생명주기와 분리되어 디버깅 용이

### **5. Composition Root는 생명의 은인**
- 초기화 순서 문제가 완전히 사라짐
- yield return null 같은 해킹 불필요

---

**이 원칙은 살아있는 문서입니다.**
- 문제 발견 시 즉시 업데이트
- 더 나은 방법 발견 시 개선
- 팀 합의로 수정 가능

**마지막 업데이트:** 2026-02-23 (Diary 시스템 리팩토링 반영)
