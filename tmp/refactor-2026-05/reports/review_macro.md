# Macro Review — 거시 코드 리뷰

_작성: 2026-05-28 / 베이스라인: baseline.md 기준_

> 미시 리뷰(`review_micro_*.md`)와 짝을 이루는 거시 리뷰. 부트스트랩, 매니저 와이어링, 레이어 준수, 데이터 흐름, 테스트 인프라를 다룬다.

---

## 1. 아키텍처 계층 — 선언 vs 실제

### 선언 (DEV 원칙 v2.0)
- **3-Layer 단방향**: Behavior → Model → Schema
- **Composition Root** 패턴
- **Singleton 최대 2-3개**
- **Model은 순수 C# 클래스** (MonoBehaviour 금지)

### 실제 (정적 분석 결과)
| 항목 | 선언 | 실제 | 비율 |
|---|---:|---:|---:|
| Singleton 매니저 수 | 2-3 | **26** | **8.6× 초과** |
| MonoBehaviour 상속 클래스 | 최소 | **99** (전체 47%) | 통제 안 됨 |
| `.Instance` 직접 호출 | 0 (Composition Root 외) | **318회** | 결합도 매우 높음 |
| Entities/ 폴더에서 `.Instance` 접근 | 0 | **25 파일** | Model이 Manager 끌어다 씀 |
| Awake/Start에서 `.Instance` | 0 | **17 메서드** | 초기화 race 가능 |

**결론**: DEV 원칙 v2.0과 실제 코드가 큰 폭으로 괴리. 원칙 문서는 살아있지만 강제 메커니즘이 부재.

---

## 2. 부트스트랩 시퀀스

### 구조 (좋음)
```
Boot.unity (BootLoader)
  ↓ additive load
Managers.unity ← ManagerBootstrap.EnsureAll() (19개 매니저 일괄 생성)
  ↓ additive load
GameStart.unity ← Continue/NewGame 버튼
  ↓ SceneLoader.LoadSceneWithInit
Mall.unity (게임플레이 시작)
```

장점:
- 3단계 분리가 명시적 ([BootLoader.cs](Assets/Scripts/Controllers/BootLoader.cs:10-31))
- "직접 Play" 대응 가드 (`if (StatsSystem.Instance == null) ManagerBootstrap.EnsureAll();`)
- `SceneNames` 상수, `SceneLoader` 중앙화 ✓

### 문제
- **GameStart.cs의 Init 시퀀스 중복**:
  - `ProcessContinue()` (line 46-71): Phase 1/2/3 = 25라인
  - `NewGame()` (line 72-107): Phase 1/2/3/4 = 35라인, Phase 1은 ProcessContinue와 동일
  - 차이는 단지 "Phase 2가 LoadAll이냐 초기값 세팅이냐". **InitPhase 추상화 부재**
- **ManagerBootstrap.EnsureAll의 하드코딩**: 19개 `Ensure<T>()` 호출이 일렬로 나열. 새 매니저 추가 시 항상 수정 필요. 등록 자동화/순서 보장 메커니즘 없음
- **부트 가드의 위치**: GameStart에 있음. BootLoader가 책임지는 게 더 명료 (현재는 GameStart 씬을 직접 Play했을 때 한정 가드)

---

## 3. 매니저 인벤토리 — 26개

### SingletonMonoBehaviour&lt;T&gt; 베이스 사용 (18개)
ActionSelectionManager, SoundManager, UISoundManager, GlobalButtonSfxManager, LoadingManager, HUDManager, UIManager, SettingsUIManager, RecipeDataManager, RecipeLookupService, RecipeBookManager, UnlockedFoodManager, InventoryManager, OrderManager, ProgressSystem, SettlementManager, StatsSystem, WeatherSystem

### 자체 Singleton (8개) — 통일성 위반
- MenuCardController (Entities/ 안에 있는 Singleton — 이중 위반)
- CropDataManager, FarmUpgradeManager
- StorageUpgradeManager, ToolUpgradeManager, UnifiedShopManager
- TimeManager (Systems/Commons)
- ValidationFeedbackUI (UI 한 컴포넌트가 Singleton?)

### 매니저 종류별 분류 (역할)
| 종류 | 매니저들 | 정당화 |
|---|---|---|
| **세이브 상태 보유** | Stats/Progress/Inventory/Order/UnlockedFood/RecipeData/3개 Upgrade/CropData | 데이터 + Save/Load 책임 — Singleton 정당화 가능 |
| **씬 라이프사이클** | Loading/UI/Settings/HUD | Composition Root에서 직접 주입할 수도 있음 |
| **사운드** | Sound/UISound/GlobalButtonSfx | 3개로 쪼개진 이유 불명 — 1개로 통합 가능 |
| **UI 한 컴포넌트** | ValidationFeedbackUI, MenuCardController | **Singleton 정당화 불가** — 씬 안의 인스턴스를 굳이 글로벌 접근 |
| **게임 룰** | Weather/Time/Settlement/UnifiedShop | Singleton 정당화 약함 |
| **자원 조회** | RecipeLookupService | static 클래스로 충분할 가능성 |
| **상호작용 진입점** | OrderManager (Entities/Mall/delivery에 위치 — 폴더 분류 어긋남) | |

**관찰**:
- 세이브 상태 보유 매니저는 Singleton 정당화 가능 (10개 정도)
- 나머지 16개 중 다수는 Composition Root 주입 또는 static class로 대체 가능
- **사운드 매니저 3개 분리**는 명확한 이유 없음
- **OrderManager가 Entities/Mall/delivery에 있는 것은 폴더 분류 오류**

---

## 4. SaveManager — 중심 god조립자

[SaveManager.cs](Assets/Scripts/Utilities/SaveManager.cs):147라인.
- `SaveAll()`: 9개 매니저의 `.Instance.GetSaveData()` 호출 → GameSaveData 조립
- `LoadAll()`: 같은 9개에 `ApplySaveData()` 분배
- piggyback 처리: RecipeDataManager / UnlockedFoodManager는 PhaseData에 얹어 저장 (`PrepareForSave` / `LoadFromProgress`)
- legacy 마이그레이션 코드 보존 (~25라인)

### 평가
- **정당화 가능**: Save/Load 책임상 모든 매니저를 알아야 함
- **그러나 확장성 한계**:
  - 새 매니저 = SaveManager 4곳 수정 (GameSaveData 필드 / SaveAll에 분배 / LoadAll에 분배 / 매니저별 null 가드)
  - **ISaveable 인터페이스 + 자동 등록 패턴**으로 정리 가능 → SaveManager 자체를 절반으로 줄일 수 있음
- **piggyback 처리는 안티패턴 신호**: 두 매니저가 다른 매니저의 데이터에 얹혀 있음 → 데이터 모델이 어긋남
- Legacy migration 코드는 일시적으로 보존해도 되나, 언제 제거할지 정해두는 게 좋음

---

## 5. Entities/ 폴더의 3-Layer 위반 패턴

### 전형 케이스: [Refrigerator.cs](Assets/Scripts/Entities/Cooking/cooking/Refrigerator.cs:9)
```csharp
private void Awake() => capacity = StorageUpgradeManager.Instance?.GetCurrentData("refrigerator")?.value ?? 7;
```
한 줄에 4가지 위반:
1. **Awake에서 다른 Singleton 접근** (초기화 race)
2. **Entities/ → Manager 호출** (레이어 위반)
3. **`"refrigerator"` 매직 스트링** (SerializeField 후보)
4. **fallback `7` 매직 넘버** (SerializeField 후보)

이 패턴이 25개 파일에 반복.

### 원인
- Refrigerator/UpperShelf/LowerShelf 같은 Behavior가 자기 자신의 capacity를 어떻게 알아낼지 모름 → 글로벌에서 찾는 패턴
- **Composition Root가 Mall/Cooking 씬 진입점에서 멈춤** — 씬 내 컴포넌트로는 주입이 확장되지 않음
- 결과: 씬 안의 모든 컴포넌트가 자기 의존성을 Singleton에서 끌어다 씀

### 해결 방향
- **씬별 Composition Root**: CookingSceneController가 씬 진입 시 모든 자식 Refrigerator/Shelf에 `Configure(capacity, sounds)` 주입
- 또는 **ScriptableObject 설정 객체**를 SerializeField로 노출 → 인스펙터에서 연결
- 또는 **이벤트 기반**: StorageUpgradeManager가 변경 이벤트 발행 → 컴포넌트가 구독 (단, 누수 관리 필요)

---

## 6. 어셈블리 / 모듈 분리

- `Game.Runtime.asmdef` **1개** (런타임 전체)
- `Tests.EditMode.asmdef` **1개** (테스트)

### 문제
- 컴파일 시간: 어떤 파일 수정해도 전체 리컴파일
- **레이어 강제 불가**: 3-Layer 원칙을 코드 컨벤션으로만 의존 → 위반이 컴파일러에 잡히지 않음
- 테스트가 게임 코드 전체에 의존 → 빌드 분리 어려움

### 개선 후보 (장기)
```
Game.Schema.asmdef       (ScriptableObject, DTO, enum)
Game.Model.asmdef        (순수 C# Model, ref → Schema)
Game.Runtime.asmdef      (MonoBehaviour, ref → Model)
Game.Editor.asmdef       (Editor 전용)
Tests.EditMode.asmdef    (ref → Model, Schema)
Tests.PlayMode.asmdef    (없음 — 신설 필요)
```

이렇게 분리하면:
- Schema → Model 역참조가 컴파일 오류로 잡힘
- Model 테스트가 Runtime 전체 빌드 없이 가능
- 단점: 초기 분리 비용, 의존성 정리 작업

---

## 7. 테스트 인프라

- EditMode 9 파일, 1016라인, 커버리지 프록시 **7.6%** (16/211 클래스)
- PlayMode 테스트 **없음**

### 테스트 커버 클래스 (16개)
ProgressSystem, StatsSystem, InventoryManager, WeatherSystem, MenuValidator, MenuSelection, MenuSchema, FoodData, FoodSchema, IngredientData, RecipeDataManager 관련, UpperShelf, Refrigerator, GameStart, UILockManager, ResourcePaths

→ Save/세이브 상태 보유 매니저 중심. **씬 라이프사이클, 사운드, UI, 상점, 농장은 미검증**.

### 한계
- **Mock 어려움**: 대부분 매니저가 MonoBehaviour + Singleton → 테스트에서 인스턴스화 불가
- **Composition Root 부재**: DI가 안 되어 있어서 fake 주입 불가
- 즉, **테스트 인프라 개선과 DI 도입은 같은 작업**

---

## 8. UI / 씬 흐름

11개 씬:
- 부트: Boot, Managers, GameStart
- 메인: Mall (메인 허브), Idle (휴식)
- 미니게임/액티비티: Cooking, Garden, Shop, Settlement
- 테스트: Scene_Cuisine_Test, Scene_Minigame_Test, Scene_RecipeBook_Test, Scene_Shop_Test, Scene_Stats_Test

### 좋음
- 명확한 분리, 명명 일관성, additive load 패턴, `SceneLoader` 중앙화
- 테스트 씬이 별도로 있어 미니게임 단위 작업 가능

### 검증 필요
- **씬 간 데이터 전달**이 어떻게 되는지 (예: Mall → Cooking에서 어떤 손님이 왔는지)
- 현재는 매니저 `.Instance`로 양쪽이 같은 상태에 접근 — 씬 분리의 장점 일부 무화

---

## 9. 데이터 흐름 — 명료도 평가

### 정리된 흐름
- **세이브**: 모든 매니저 → GameSaveData → gamedata.json (단일 파일) ✓
- **씬 전환**: SceneLoader → LoadingManager → 페이드 + initAction 실행 ✓
- **UI 잠금**: UILockManager (enum-based owner) ✓

### 불명확한 흐름
- **레시피 선택 → 미니게임 → 결과 적용**: MenuCardController(594라인, Singleton, Entities/ 위반) 가운데서 다중 책임 보유
- **손님 주문 → 요리 → 배달**: CustomerManager 분할이 진행됐지만 이벤트 누수 5건 (메시지 큐 vs 직접 콜백 혼재 가능성)
- **상점 흐름**: Unified라는 이름이지만 자체 Singleton 3개 분립 (`UnifiedShopManager`, `StorageUpgradeManager`, `ToolUpgradeManager`)

---

## 10. 종합 거시 평가

### 강점
- 부트스트랩 시퀀스 명료 (3씬 분리, additive load)
- SaveManager 단일 파일 일원화 (이전 5파일에서 정리됨)
- SingletonMonoBehaviour&lt;T&gt; 베이스 도입 (절반 매니저는 사용 중)
- `.meta` 무결성 깨끗
- 매직 스트링 회피 (`SceneNames`, `ResourcePaths`)
- UILockManager enum 기반 — 작고 명확한 패턴
- DEV 원칙 문서 v2.0 존재 (살아있음)

### 약점
- **선언과 실제의 8배 갭** (Singleton 26 vs 권장 3, 318회 `.Instance`)
- **Composition Root 패턴이 GameStart에서 멈춤** — 씬 내부로 확장 안 됨
- **어셈블리 1개** — 레이어 강제 메커니즘 부재
- **자체 Singleton 8개** — SingletonMonoBehaviour 베이스 미사용으로 라이프사이클 일관성 깨짐
- **사운드 매니저 3개** — 통합 가능
- **테스트 인프라 빈약** (커버리지 7.6%, PlayMode 없음, Mock 불가 구조)
- **GameStart Init 시퀀스 중복** (ProcessContinue/NewGame ~30라인 거의 동일)
- **piggyback 매니저** (RecipeDataManager/UnlockedFoodManager가 PhaseData에 얹힘) — 데이터 모델 이상 신호

### 기회 (리펙터링 유효 지점)
1. **ISaveable 인터페이스 + 자동 등록** → SaveManager 단순화
2. **씬 진입 Composition Root 확장** → Entities/ 위반 25건 정리
3. **자체 Singleton 8개 → SingletonMonoBehaviour 베이스 통일** (단순 마이그레이션)
4. **불필요 Singleton 제거** (사운드 통합, UI Singleton 인스턴스 노출, static 변환 후보 정리)
5. **InitPhase 추상화** → GameStart 중복 제거
6. **어셈블리 분리** (Schema/Model/Runtime) — 장기 작업이나 효과 큼
7. **PlayMode 테스트 도입** + 핵심 흐름(저장/로드, 씬 전환, 손님→요리→배달) 시나리오 테스트

---

_미시 리뷰 통합본은 `review_micro_*.md` 작성 후 `review_master.md`로 합본 예정._
