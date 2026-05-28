# Micro Review: MenuCardController.cs

_Subagent 결과 — 594라인, 자체 Singleton, depth 6, Entities/ 위반_

## 1. 책임 (실제로 무엇을 하는가)

**한 줄 요약:** 식재료/음식의 레시피 체인과 재료 목록을 UI 카드로 표시하는 통합 프레젠터 컴포넌트

**세부 책임:**
- Singleton 기반 메뉴 카드 생성 및 생명주기 관리 (Awake, CloseMenuCard)
- 계층구조 스캔을 통한 UI 컴포넌트 자동 바인딩 (BindReferences, CacheTemplates)
- 재귀적 레시피 체인 구축 및 DFS 순회 (BuildRecipeChain, CollectRecipes)
- 동적 UI 인스턴싱: 레시피 라인 생성 및 재료 항목 렌더링 (PopulateRecipeLines, PopulateIngredients)
- 복잡한 레이아웃 간격 계산 및 조절 (AdjustSpacing, EstimateInputWidthFromRecipeLine) — 62줄
- 식재료/중간재료/도구 타입 분기 및 이미지 선택 로직 (PopulateInputItem, PopulateToolItem, PopulateResultItem)
- 탭 UI 상태 관리 (OpenRecipe, OpenIngredient, 세션 상태 유지 via static lastTabWasIngredient)

## 2. 식별된 안티패턴

### 2.1 자체 Singleton 구현 (L8-12, 49, 514)
```csharp
private static MenuCardController instance;
public static MenuCardController Instance => instance;
```
- DEV 원칙 위반 — Entities/ 폴더의 Behavior가 매니저처럼 Singleton 보유
- 테스트 불가능 (Mock 주입 불가)
- 씬 전환 후 static 상태 유지 → 메모리 누수/예상치 못한 상태 잔류
- CloseMenuCard()에서 `instance = null` 수동 관리 → 더블-디스트로이 위험
- **해결**: RecipeBookManager(호출자)가 생성 후 Init/Populate 호출 (Composition Root)

### 2.2 RecipeDataManager.Instance 직접 접근 (L132, 212-213, 422-423) — 3-Layer 위반
- Entities/RecipeBook/ → Managers/ 직접 호출
- **해결**: `IRecipeProvider` 인터페이스 주입, 또는 PopulateRecipeLine에 toolId 파라미터로 전달

### 2.3 SerializeField 있으나 OnValidate 없음
- DEV 원칙 필수 규칙 위반
- BindReferences()의 Find()가 계층 변경 시 조용히 실패
- **해결**: #if UNITY_EDITOR OnValidate 블록에 Find 재수행 + 누락 시 LogError

### 2.4 Long Method
- `AdjustSpacing` 62라인 — 레이아웃 계산만으로
- `InitSlot` 52라인 — 헤더+이미지+레시피+탭 4가지 책임 혼재
- `PopulateRecipeLine` 51라인

### 2.5 Primitive Obsession: string toolId 반복 전달
- L132, 212, 422 (계산), L149-150, 257, 433 (재사용)
- null/empty 체크 반복
- **해결**: `CookingToolData` DTO로 캡슐화

### 2.6 Feature Envy: Transform 계층구조 스캔 (L220-257)
```csharp
Transform inputContainer = line.Find("Input");
Transform minigame = line.Find("Minigame");
Transform resultContainer = line.Find("Result");
```
- RecipeLine 프리팹 구조를 MenuCardController가 알고 있음
- **해결**: `RecipeLineView` 컴포넌트 도입, SerializeField로 자식 노출

## 3. 추가 코드 스멜

- **Long Parameter List**: PopulateRecipeLine 호출 체인 (3-4개 파라미터) → 컨텍스트 객체 도입
- **Shotgun Surgery**: 탭 상태가 static + 인스턴스 메서드에 분산
- **잠재 이벤트 누수**: MenuCardOverlay.Open()에서 `btn.onClick.AddListener(Close)` 후 RemoveListener 없음 (프리팹 재사용 시 중첩 가능)
- **Null Safety**: 일관성은 있으나 의도 주석 없음

## 4. 분리 가능한 책임

```
MenuCardController (Orchestrator)
├── MenuCardHeaderPresenter      (헤더 텍스트 + 이미지)
├── MenuCardImagePresenter       (상단 음식/도구 이미지)
├── RecipeLineRenderer           (레시피 라인 인스턴싱)
├── RecipeLineView               (프리팹 컴포넌트 — 자기 자식 관리)
├── MenuCardLayoutAdjuster       (AdjustSpacing 62라인 전담)
├── IngredientListRenderer       (재료 BFS + 표시)
└── MenuCardTabManager           (탭 상태 + static lastTabWasIngredient 흡수)
```

**참조 사례 (CustomerManager 분할과 유사)**:
- CustomerManager → CustomerSpawner + OrderTicketController + CustomerLifecycle 처럼
- MenuCardController → Presenter + 여러 Renderer + TabManager로 분할

## 5. 우선순위 권고

### Priority 1 (HIGH) — 아키텍처 오염
1. **RecipeDataManager.Instance 제거 (3 calls)**
   - DI로 주입, Composition Root에서 IRecipeDataProvider 전달
   - 비용: 중간 (시그니처 변경)
2. **MenuCardController Singleton 제거**
   - MenuCardOverlay/RecipeBookManager가 생성·주입
   - 비용: 높음 (호출자 다수)

### Priority 2 (MEDIUM) — 코드 복잡도
3. **AdjustSpacing → MenuCardLayoutAdjuster 추출** (낮은 비용)
4. **InitSlot 분해 → 4-5개 Presenter** (중간 비용)
5. **OnValidate 추가** (낮은 비용)

### Priority 3 (LOW) — 가독성
6. Primitive Obsession 완화 (toolId → DTO)
7. RecipeLineView 컴포넌트화

## 추정 작업
- Priority 1: 2-3일
- Priority 2: 2-3일
- Priority 3: 1일
- **합계**: 5-7일 (테스트 포함)
