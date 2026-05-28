# Micro Review: Entities/ 3-Layer 위반 분석

## 1. 위반 패턴 분류 (25개 파일 종합)

### 1.1 매니저 의존성 분포

**음향 시스템 (6개 파일)**
- `SoundManager.Instance`: BentoModel, OrderTicketModel, RefrigeratorBehavior, CookingToolModel, TakingCustomer, PlayerMove, DialogueManager (7회)
  - 주로 UI/Entity 상호작용 시 음향 피드백
  - 특징: 거의 단순 재생 호출 (`Play2DSFX` 메서드)

**비즈니스 로직 매니저 (10개 파일)**
- `StorageUpgradeManager.Instance`: Refrigerator, UpperShelf, LowerShelf, InventoryPageController (4회)
  - 저장소 용량 조회/업그레이드 상태 확인
  - **핫스팟**: 생성자 대신 Awake에서 직접 접근하여 초기화 의존

- `FarmUpgradeManager.Instance`: Farm, FarmTile (4회)
  - 농장 타일 수, 수확량 데이터 조회
  - **핫스팟**: Farm.Start()에서 접근, depth 6 깊이

- `TimeManager.Instance`: WaitingCustomer (6회)
  - 타이머 시작/취소 (고객 대기 시간)
  - **핫스팟**: Start()에서 직접 접근, 콜백 등록

- `ProgressSystem.Instance`: Farm, CookingBackgroundController (7회)
  - 현재 시간대/날짜 조회, 이벤트 구독
  - **핫스팟**: Farm.Start()에서 초기화 의존, CookingBackgroundController는 null-safe

- `CropDataManager.Instance`: Farm, FarmTile (3회)
  - 작물 데이터 조회
  - Farm.Update()에서 동적 조회

**인벤토리/영수증 (4개 파일)**
- `InventoryManager.Instance`: ShopDetailPanel, InventoryPageController, Farm (8회)
  - 재료 추가/재고 확인/배치 조회
  - **핫스팟**: ShopDetailPanel은 Model이 아닌 UI지만 Entity 폴더에 위치

- `StatsSystem.Instance`: ShopDetailPanel, TakingCustomer (3회)
  - 돈 조회/증감
  - 게임 전역 상태 조회

**레시피/UI 컨트롤러 (5개 파일)**
- `RecipeDataManager.Instance`: MenuSlot, MenuCardController (7회)
- `RecipeBookManager.Instance`: MenuSlot (2회)
- `MenuCardController.Instance`: MenuCardOverlay (2회)
  - Entities 폴더 내 UI 컴포넌트 간 접근
  - **구조적 문제**: RecipeBook 전체가 Entity화되면서 발생

**특수 매니저 (3개 파일)**
- `UnlockedFoodManager.Instance`: OrderManager (4회)
- `UnifiedShopManager.Instance`: ShopDetailPanel, UnifiedShopInteraction (2회)
- `OrderManager.Instance`: DeliveryNpcDialogueInteraction, DeliveryNpcOrderInteraction, DeliveryNpcReceiptInteraction (7회)
  - Delivery 서브시스템 내부 참조

**시스템 제공자**
- `WeatherSystem.Instance`: CasualDialogueProvider (1회)
- `SettlementManager.Instance`: ShopDetailPanel (1회)
- `GlobalButtonSfxManager.Instance`: DialogueManager (1회)
- `UILockManager`: UnifiedShopInteraction (정적 클래스)

---

## 2. 핫스팟 분석 (Awake/Start + Entities 이중 위반)

### 2.1 **Storage 계층: Refrigerator, UpperShelf, LowerShelf** (Critical)

**위반 패턴:**
```csharp
// Refrigerator.cs, UpperShelf.cs, LowerShelf.cs (모두 동일)
private void Awake() => capacity = StorageUpgradeManager.Instance?.GetCurrentData("refrigerator")?.value ?? 7;
```

**문제점:**
- ✗ **Awake**에서 Manager.Instance 직접 접근 → 싱글톤 초기화 타이밍 의존성
- ✗ BaseStorage는 Model 계층인데 Manager 의존
- ✗ 3개 파일 모두 **동일한 중복 패턴** (BaseStorage로 추출하면 더 나음)
- ✗ Null-safe 연산자 사용했지만 기본값 fallback만으로는 불충분 (용량 0일 수도)

**해결책:**
1. **즉시 (Phase 0)**: Composition Root에서 생성 후 세터로 주입
   ```csharp
   // Refrigerator.cs
   private int _capacity;
   public void InjectCapacity(int capacity) => _capacity = capacity;
   // Awake는 제거하고 OnEnable에서만 초기화
   ```

2. **BaseStorage 레벨로 제거**:
   - `protected abstract void InitializeCapacity()` 메서드 추가
   - 각 subclass에서 최소 기본값만 설정하고 외부 주입 대기

3. **ManagerBootstrap 개선**:
   - StorageUpgradeManager 초기화 전에 Storage 객체 생성하지 않도록 명시적 순서 지정

---

### 2.2 **Farm** (High Priority, Depth 6)

**위반 패턴:**
```csharp
// Farm.cs Start()
if (ProgressSystem.Instance == null)
    ManagerBootstrap.EnsureAll();
phaseProvider = ProgressSystem.Instance;
// 이후 Farm.Update()에서
int harvestCount = (int)(FarmUpgradeManager.Instance?.GetCurrentData("harvestCount")?.value ?? 5);
CropData randomCrop = CropDataManager.Instance.GetRandomCropByWeight();
InventoryManager.Instance?.AddHarvestedCrop(id, crops);
```

**문제점:**
- ✗ **Start()** 직접 접근: ManagerBootstrap.EnsureAll() 안전-호출로 초기화 타이밍을 자체 제어
- ✗ 4개 Manager 직접 의존 (ProgressSystem, FarmUpgradeManager, CropDataManager, InventoryManager)
- ✗ **Composition Root 우회**: 씬 로드 시점에 이미 Farm이 존재하면 Manager들이 준비되지 않을 수 있음
- ✗ TimePhaseProvider 인터페이스를 받아 사용하지만, ProgressSystem.Instance로도 접근

**권고:**
1. **즉시**: Start에서 ManagerBootstrap.EnsureAll() 호출 → 불필요 (Composition Root가 해야 함)
   - 대신 시작 씬의 Bootstrap GameObject에서 EnsureAll()을 FIRST로 호출하도록 강제

2. **Phase 1**: Farm 생성자 또는 inject() 메서드로 의존성 받기:
   ```csharp
   public void Inject(
       TimePhaseProvider phaseProvider,
       IFarmUpgradeProvider upgradeProvider,
       ICropProvider cropProvider,
       IInventoryProvider inventoryProvider)
   ```

3. **시각적 구조**: 장면에서 "Bootstrap" GameObject를 첫 번째로 배치하고, Farm은 그 자식에 배치하여 순서 강제

---

### 2.3 **WaitingCustomer** (High Priority, Depth 6)

**위반 패턴:**
```csharp
// WaitingCustomer.cs Start()
if (TimeManager.Instance != null)
{
    managerTimerId = TimeManager.Instance.StartCustomerTimer(...);
}
```

**문제점:**
- ✗ **Start()** 직접 접근: TimeManager에 강한 의존
- ✗ 콜백을 TimeManager에 등록 → 생명주기 관리 분산
- ✗ 재사용성 낮음: TimeManager 없으면 타이머 안 됨

**권고:**
1. **Phase 1**: ITimerProvider 인터페이스 도입
   ```csharp
   public interface ITimerProvider
   {
       int StartTimer(Action<float, float> onTick, Action onComplete, float duration);
       void CancelTimer(int id);
   }
   ```

2. **WaitingCustomer.Inject(ITimerProvider timerProvider)**로 변경
3. **CustomerManager** (또는 Composition Root)에서 WaitingCustomer 생성 시 주입

---

### 2.4 **RefrigeratorBehavior + SoundManager** (Medium Priority)

**위반 패턴:**
```csharp
// RefrigeratorBehavior.cs Update()
SoundManager.Instance.Play2DSFX(openSound, 0.4f);
```

**문제점:**
- ✗ **Behavior 계층**이 Manager 호출 (Model-Behavior 분리 위반)
- ✗ Refrigerator.cs는 BaseStorage에서 Manager 접근 안 하는데, 동생인 RefrigeratorBehavior는 접근
- 이유: 음향은 Behavior 관심사이지만, Manager 의존을 피할 수 없음

**평가:**
- Sound는 "presentation" 계층이므로 경미하지만, 일관성 문제
- Model-Behavior 분리는 제대로 되어 있음 (BentoModel ↔ BentoBehavior와 유사)

**권고:**
1. **Phase 2 (낮은 우선순위)**: ISoundPlayer 인터페이스 도입
   ```csharp
   public interface ISoundPlayer
   {
       void PlaySFX(AudioClip clip, float volume);
   }
   ```

2. **RefrigeratorBehavior.Inject(ISoundPlayer soundPlayer)**

---

## 3. Model vs Behavior 분리 상태 평가

### 3.1 **패턴 적용 현황**

**잘 분리된 쌍:**
1. Refrigerator (Model) ↔ RefrigeratorBehavior (Behavior)
   - Refrigerator: 용량, 식재료 저장소 로직 (Model)
   - RefrigeratorBehavior: 마우스 호버 시 스프라이트 숨기기, 음향 (Behavior)
   - ✓ 분리 잘 됨, 하지만 둘 다 Manager 의존성 존재

2. BentoModel ↔ BentoBehavior
   - BentoModel: 음식 슬롯 관리, 위치 계산 (Model)
   - BentoBehavior: 텍스처 표시, 드래그 애니메이션 (Behavior)
   - ✓ Model이 SoundManager.Instance 접근 (음식 추가 SFX)

3. CookingToolModel ↔ CookingToolBehavior
   - CookingToolModel: 식재료 조리 로직, 요리 가능 판정 (Model)
   - CookingToolBehavior: 아이콘 표시 (Behavior)
   - ✓ Model이 SoundManager.Instance 접근 (음식 버림 SFX)
   - ✓ **Inject 패턴 사용** (PlayMinigameUsecase, SearchRecipeUsecase)

4. OrderTicketModel ↔ OrderTicketBehavior
   - OrderTicketModel: 주문 첨부 로직 (Model)
   - OrderTicketBehavior: 스프라이트 표시 (Behavior)
   - ✓ Model이 SoundManager.Instance 접근

**분리 미흡:**
5. FoodModel + 기타 컴포넌트
   - FoodModel 자체의 Model-Behavior 분리는 확인 불가 (파일 미조회)
   - 하지만 BaseStorage와의 관계는 명확함

6. Farm (모든 걸 다 함)
   - Farm.cs: Model 로직 + UI 업데이트 + Behavior 모두 혼합
   - FarmTile: 데이터 모델
   - ✗ 분리 필요 (FarmBehavior 추출)

7. WaitingCustomer, TakingCustomer
   - Customer 계층 전체가 Model + Behavior 혼합
   - ✗ CustomerModel + CustomerBehavior로 분리 고려

---

### 3.2 **Inject 패턴 평가**

**이미 적용된 사례 (Good):**
- CookingToolModel.Inject(PlayMinigameUsecase, SearchRecipeUsecase)
  - **구조적 모범**: 유스케이스 인터페이스로 의존성 역전

**미적용 (Bad):**
- Refrigerator, UpperShelf, LowerShelf: Awake에서 Manager 접근
- WaitingCustomer: Start에서 Manager 접근
- BentoModel, CookingToolModel, OrderTicketModel: SoundManager.Instance 직접 접근

---

## 4. BaseStorage 패턴 평가

### 4.1 **현재 상태 (Good)**

```csharp
// BaseStorage.cs 구조
public abstract class BaseStorage : MonoBehaviour
{
    protected int capacity = 99;
    protected List<FoodModel> foodModels = new List<FoodModel>();
    
    public virtual bool AddIngredients(FoodModel ingredient) { ... }
    protected abstract Vector3 CalculatePositionForIndex(int index);
}

// 서브클래스들 (Refrigerator, UpperShelf, LowerShelf)
// CalculatePositionForIndex만 구현
```

**평가:**
- ✓ 음식 저장소의 공통 로직을 추상화함
- ✓ 위치 계산 전략 패턴이 적절함
- ✓ 초기 리펙터링이 중복을 제거함

### 4.2 **문제점**

**용량 초기화 위반:**
```csharp
// 현재 (나쁜 사례)
private void Awake() => capacity = StorageUpgradeManager.Instance?.GetCurrentData("refrigerator")?.value ?? 7;

// BaseStorage로 올리면
protected virtual void InitializeCapacity() { }
// 각 subclass에서
protected override void InitializeCapacity() 
{
    // capacity = StorageUpgradeManager.Instance?....  ← 여전히 Manager 의존
}
```

**해결책:**
- BaseStorage에 `public void SetCapacity(int value)` 추가
- Composition Root에서 각 Storage 객체 생성 후, StorageUpgradeManager 초기화 후 명시적으로 SetCapacity() 호출

### 4.3 **다른 영역에 적용 가능성**

**Farm / FarmTile 구조:**
- FarmTile: 작물 데이터 (Model)
- Farm: FarmTile + UI + 인벤토리 상호작용 (혼합)
- **권고**: BaseCrop, BaseTile 패턴 고려 (아직은 1개 구현체뿐이라 서두르지 말 것)

**Customer 계층:**
- WaitingCustomer, TakingCustomer: 중복 가능성
- **권고**: BaseCustomer 패턴 검토 (2개 구현체)

---

## 5. 의존성 주입 전략 권고

### 5.1 **인터페이스 도입 (필수)**

```csharp
// 음향 (Sound)
public interface ISoundPlayer
{
    void PlaySFX(AudioClip clip, float volume = 1.0f);
    void PlayBGM(AudioClip clip, float volume = 1.0f);
}

// 타이머 (Time)
public interface ITimerProvider
{
    int StartTimer(float duration, Action<float, float> onTick, Action onComplete);
    void CancelTimer(int id);
}

// 업그레이드 (Storage/Farm/Tool)
public interface IStorageUpgradeProvider
{
    int GetCapacity(string storageType);
    bool TryUpgrade(string storageType);
}

public interface IFarmUpgradeProvider
{
    int GetTileCount();
    int GetHarvestCount();
    float GetTimeReduction();
}

// 작물 (Crop)
public interface ICropProvider
{
    CropData GetRandomCrop();
    CropData GetCropById(string id);
    void RegisterCrop(CropData data);
}

// 인벤토리 (Inventory)
public interface IInventoryProvider
{
    void AddHarvestedCrop(string cropId, int count);
    int CheckStockAmount(FoodData food);
    void AddFood(FoodData food, int count);
}

// 진행도 (Progress)
public interface IProgressProvider
{
    PhaseType CurrentPhase { get; }
    int CurrentDay { get; }
    event System.Action<PhaseType> OnPhaseChanged;
}

// 주문 (Order)
public interface IOrderProvider
{
    void GenerateOrder(MenuSchema menu, string questId, string npcId);
    List<DeliveryOrderData> GetOrders();
}

// UI (UI Manager)
public interface IShopUIProvider
{
    void OpenShop(UnifiedShopManager.Tab tab);
    void NotifyItemPurchased(FoodData food, int qty);
}
```

### 5.2 **Composition Root 확장**

```csharp
// 기존: GameStart 씬의 Bootstrap GameObject에서
ManagerBootstrap.EnsureAll();

// 개선: 명시적 DI 컨테이너
public class GameCompositionRoot : MonoBehaviour
{
    private void Awake()
    {
        // 1. 시스템 초기화 순서 엄격하게
        var statsSystem = GetOrCreate<StatsSystem>();
        var progressSystem = GetOrCreate<ProgressSystem>();
        var timeManager = GetOrCreate<TimeManager>();
        
        // 2. 이들을 의존하는 매니저들 초기화
        var soundManager = GetOrCreate<SoundManager>();
        var inventoryManager = GetOrCreate<InventoryManager>();
        var storageUpgradeManager = GetOrCreate<StorageUpgradeManager>();
        
        // 3. Entity 객체들에 의존성 주입
        InjectStorageCapacities(storageUpgradeManager);
        InjectFarmDependencies(progressSystem, storageUpgradeManager);
        InjectCustomerDependencies(timeManager, soundManager);
    }
    
    private void InjectStorageCapacities(StorageUpgradeManager upgradeManager)
    {
        foreach (var storage in FindObjectsOfType<BaseStorage>())
        {
            string storageType = storage.GetType().Name.ToLower();
            int capacity = upgradeManager.GetCurrentData(storageType)?.value ?? 7;
            storage.SetCapacity(capacity);
        }
    }
}
```

### 5.3 **점진적 도입 로드맵**

**Phase 0 (Immediate - 1-2주)**
1. BaseStorage.SetCapacity() 추가
2. Refrigerator/UpperShelf/LowerShelf의 Awake 제거, SetCapacity 호출로 변경
3. GameStart 또는 CookingSceneManager에서 명시적 호출

**Phase 1 (Near-term - 2-4주)**
1. ISoundPlayer 인터페이스 도입
2. BentoModel, CookingToolModel, OrderTicketModel의 SoundManager.Instance 제거
3. Composition Root에서 ISoundPlayer 주입

4. ITimerProvider 인터페이스 도입
5. WaitingCustomer에 주입
6. TimeManager를 ITimerProvider 래퍼로 구현

**Phase 2 (Mid-term - 4-8주)**
1. IFarmUpgradeProvider, ICropProvider 도입
2. Farm.cs 리펙터링 (HEAVY)
3. Farm ↔ FarmBehavior 분리

4. IProgressProvider 도입
5. CookingBackgroundController, Farm에 주입
6. ProgressSystem 래퍼 작성

**Phase 3 (Long-term - 8주 이후)**
1. 복잡한 UI 계층 (RecipeBook, Shop) 리펙터링
2. MenuCardController ↔ MenuCardOverlay 의존성 제거
3. OrderManager 내 UnlockedFoodManager 의존성 제거

---

## 6. 우선순위 권고

### 6.1 **Critical (필수, 1-2주 내)**

| 파일 | 문제 | 해결 방법 | 예상 작업량 |
|------|------|---------|-----------|
| **Refrigerator.cs** | Awake에서 StorageUpgradeManager.Instance | SetCapacity() 도입 | 30분 |
| **UpperShelf.cs** | 동상 | 동상 | 15분 |
| **LowerShelf.cs** | 동상 | 동상 | 15분 |
| **BaseStorage.cs** | 용량 초기화 전략 부재 | public SetCapacity() 메서드 추가 | 30분 |

**목표:** Awake에서의 Manager.Instance 접근 제거

### 6.2 **High (높음, 2-4주 내)**

| 파일 | 문제 | 해결 방법 | 예상 작업량 |
|------|------|---------|-----------|
| **Farm.cs** | Start에서 4개 Manager 직접 접근, ManagerBootstrap.EnsureAll() 호출 | IFarmUpgradeProvider, ICropProvider, IProgressProvider, IInventoryProvider 주입 | 4-5시간 |
| **WaitingCustomer.cs** | Start에서 TimeManager.Instance 접근 | ITimerProvider 도입, 주입 | 2시간 |
| **ManagerBootstrap.cs** | Composition Root 패턴 부재 | GameCompositionRoot 신규 생성, 명시적 DI 컨테이너 | 3시간 |

**목표:** Start에서의 Manager.Instance 접근 제거, Composition Root 강화

### 6.3 **Medium (중간, 4-8주 내)**

| 파일 | 문제 | 해결 방법 | 예상 작업량 |
|------|------|---------|-----------|
| **BentoModel.cs** | SoundManager.Instance 직접 접근 | ISoundPlayer 도입, 주입 | 1.5시간 |
| **CookingToolModel.cs** | SoundManager.Instance 직접 접근 | 동상 | 1시간 |
| **OrderTicketModel.cs** | SoundManager.Instance 직접 접근 | 동상 | 1시간 |
| **RefrigeratorBehavior.cs** | SoundManager.Instance 직접 접근 | 동상 | 1시간 |
| **TakingCustomer.cs** | StatsSystem.Instance + SoundManager.Instance | ISoundPlayer + IMoneyProvider 도입 | 1.5시간 |
| **ShopDetailPanel.cs** | 5개 Manager 직접 접근 | 각 인터페이스 주입 | 3시간 |

**목표:** Model-Behavior 계층 내 Manager 의존성 제거

### 6.4 **Low (낮음, 8주 이후 또는 필요시)**

| 파일 | 문제 | 해결 방법 | 예상 작업량 |
|------|------|---------|-----------|
| **MenuCardController.cs** | RecipeDataManager.Instance 접근 | IRecipeProvider 도입 | 2시간 |
| **MenuCardOverlay.cs** | MenuCardController.Instance 접근 | 이벤트 기반으로 변경 | 2시간 |
| **MenuSlot.cs** | RecipeDataManager.Instance + RecipeBookManager.Instance | 의존성 제거, 부모 이벤트 구독 | 1.5시간 |
| **InventoryPageController.cs** | 3개 Manager 접근 | IInventoryProvider, IStorageUpgradeProvider 도입 | 2시간 |
| **OrderManager.cs** | Entities 폴더에 위치하면서 UnlockedFoodManager.Instance 접근 | 도메인 재정리 (Manager 폴더로 이동 가능) | 2시간 |
| **DialogueManager.cs** | 3개 SoundManager 변종 접근 | 일관된 ISoundPlayer로 통합 | 1시간 |
| **CasualDialogueProvider.cs** | WeatherSystem.Instance 접근 | IWeatherProvider 도입 | 30분 |
| **DeliveryNpc* (3개 파일)** | OrderManager.Instance 접근 | IOrderProvider 주입 | 2시간 |

**목표:** UI/대사 계층 의존성 정리, 장기 아키텍처 개선

---

## 7. 기존 리펙터링 결과물 평가

### 7.1 **BaseStorage 도입 (Good)**
- ✓ Refrigerator/UpperShelf/LowerShelf의 AddIngredients, RefreshPosition 중복 제거
- ✓ CalculatePositionForIndex 전략 패턴 적용으로 레이아웃 확장 용이
- **개선점**: 용량 초기화를 Builder 패턴이나 Factory 패턴으로 분리하면 더 강화 가능

### 7.2 **CookingToolModel Inject 패턴 (Excellent)**
- ✓ PlayMinigameUsecase, SearchRecipeUsecase를 인터페이스로 의존성 역전
- ✓ Awake가 아닌 Start 후 Inject() 호출 패턴은 좋지만, Composition Root에서 자동화되지 않음
- **권고**: GameCompositionRoot에서 모든 Inject() 호출 자동화

### 7.3 **Model-Behavior 분리 (Good but Inconsistent)**
- ✓ Refrigerator + RefrigeratorBehavior
- ✓ BentoModel + BentoBehavior
- ✓ CookingToolModel + CookingToolBehavior
- ✗ OrderTicketModel + OrderTicketBehavior (존재하나 확인 불가)
- ✗ Farm (분리 안 됨)
- ✗ Customer 계층 (분리 안 됨)

**권고**: Farm, Customer 계층도 Model-Behavior 분리 (이중 리펙터링 전에 우선 Manager 의존성 제거)

---

## 8. 결론 및 최종 권고

### 8.1 **근본 원인**

Entities/ 폴더의 25개 파일이 Manager.Instance를 접근하는 이유:
1. **Composition Root 부재**: ManagerBootstrap.EnsureAll()은 순서만 보장하고, 명시적 DI 컨테이너가 없음
2. **Awake/Start 타이밍 의존**: Manager 준비 상태를 자체 확인하고 초기화 → 강한 결합
3. **기존 인터페이스 부재**: Manager를 직접 참조할 수밖에 없음
4. **UI와 Entity 혼재**: RecipeBook, Shop이 Entities/ 폴더에 위치하면서 자체 Manager 직접 접근

### 8.2 **최종 체크리스트**

- [ ] **Phase 0 (Week 1-2)**
  - [ ] BaseStorage.SetCapacity() 도입 (30분)
  - [ ] 3개 Storage 파일의 Awake 제거 (1시간)
  - [ ] ManagerBootstrap 호출 위치 명시적으로 변경 (30분)

- [ ] **Phase 1 (Week 2-4)**
  - [ ] GameCompositionRoot 신규 생성 (3시간)
  - [ ] ISoundPlayer, ITimerProvider 인터페이스 도입 (2시간)
  - [ ] Farm.cs 리펙터링 (4-5시간, **HEAVY**)
  - [ ] WaitingCustomer 리펙터링 (2시간)

- [ ] **Phase 2+ (Week 4-8+)**
  - [ ] 나머지 Sound 의존성 제거 (5-6시간)
  - [ ] ShopDetailPanel 리펙터링 (3시간)
  - [ ] UI 계층 (RecipeBook, Dialogue) 의존성 정리 (6-8시간)

### 8.3 **예상 전체 작업량**

- **Critical**: 2시간
- **High**: 10시간 (Farm.cs가 대부분)
- **Medium**: 12시간
- **Low**: 15시간
- **총 예상**: **39시간** (~1 full-time week or 2 part-time weeks)

### 8.4 **핵심 성공 요소**

1. **ManagerBootstrap → GameCompositionRoot** 전환: 명시적 DI 컨테이너로 타이밍 제어
2. **Awake/Start에서 Manager 접근 금지**: 대신 Inject() 메서드 또는 세터 사용
3. **인터페이스 먼저**: Manager 직접 참조 → 인터페이스 주입으로 전환
4. **점진적 도입**: Critical → High → Medium 순서로, 테스트 병행
5. **문서화**: 각 Entities/ 파일에 "주입 가능 의존성" 코멘트 추가

---

## Appendix: 파일별 Instance 접근 현황 (상세)

| 파일 | Manager | 횟수 | 위치 | 심각도 |
|------|---------|------|------|--------|
| Refrigerator.cs | StorageUpgradeManager | 1 | Awake | **Critical** |
| UpperShelf.cs | StorageUpgradeManager | 1 | Awake | **Critical** |
| LowerShelf.cs | StorageUpgradeManager | 1 | Awake | **Critical** |
| BaseStorage.cs | - | 0 | - | OK |
| RefrigeratorBehavior.cs | SoundManager | 2 | Update | Medium |
| BentoModel.cs | SoundManager | 2 | Methods | Medium |
| OrderTicketModel.cs | SoundManager | 2 | Start/Method | Medium |
| CookingToolModel.cs | SoundManager | 1 | Method | Medium |
| Farm.cs | ProgressSystem, FarmUpgradeManager, CropDataManager, InventoryManager | 10 | Start/Update | **High** |
| FarmTile.cs | FarmUpgradeManager, CropDataManager | 2 | Method | Medium |
| WaitingCustomer.cs | TimeManager | 6 | Start/OnDestroy | **High** |
| TakingCustomer.cs | StatsSystem, SoundManager | 3 | Method | Medium |
| CookingBackgroundController.cs | ProgressSystem | 5 | Start | Low |
| ShopDetailPanel.cs | StatsSystem, InventoryManager, UnifiedShopManager, ToolUpgradeManager, StorageUpgradeManager, SettlementManager, FarmUpgradeManager | 15 | Methods | **High** (UI인데 Entity 폴더) |
| InventoryPageController.cs | InventoryManager, StorageUpgradeManager | 6 | Methods | Medium |
| MenuCardController.cs | RecipeDataManager | 5 | Methods | Low (UI 내부) |
| MenuCardOverlay.cs | MenuCardController | 2 | Methods | Low (UI 내부) |
| MenuSlot.cs | RecipeDataManager, RecipeBookManager | 4 | Methods | Low (UI 내부) |
| PlayerMove.cs | SoundManager | 1 | Update | Low |
| UnifiedShopInteraction.cs | UnifiedShopManager | 1 | Update | Low |
| CasualDialogueProvider.cs | WeatherSystem | 1 | Static method | Low |
| DialogueManager.cs | SoundManager, UISoundManager, GlobalButtonSfxManager | 3 | Methods | Medium |
| DeliveryNpcDialogueInteraction.cs | OrderManager | 4 | Methods | Medium |
| DeliveryNpcOrderInteraction.cs | OrderManager | 1 | Method | Medium |
| DeliveryNpcReceiptInteraction.cs | OrderManager | 2 | Methods | Medium |
| OrderManager.cs | UnlockedFoodManager | 4 | Methods | Medium (Entities 폴더라 문제) |

**총합**: 25개 파일, ~110회 Instance 접근, 그 중 5개 Critical + High 파일이 ~30회 차지

