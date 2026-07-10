# Time & Phase System

Aftertaste의 시간·페이즈 시스템. 하루 5개 페이즈 (Preparation → Morning → Afternoon → Evening → Night) 사이 진행, 각 페이즈 시작 시각을 스탯의 `time` 필드에 반영, 미니게임/영업/영업 종료 트리거 등을 이 서비스가 제어.

관련 코드:
- `ProgressService` — 페이즈/일 진행 POCO (`Assets/Scripts/Unity/Common/ProgressService.cs`).
- `TimeManager` — 실시간 → 게임 시간 변환 + 로컬 타이머 (`Assets/Scripts/Unity/Common/TimeManager.cs`).
- `PhaseType` — enum (`Assets/Scripts/Schema/Config/Common/PhaseType.cs`).
- `PhaseData` — 저장 상태 (`Assets/Scripts/Schema/State/Common/PhaseData.cs`).
- `ClockUI` — 시침 UI (`Assets/Scripts/Unity/UI/ClockUI.cs`).

## 1. Phase Enum

`Assets/Scripts/Schema/Config/Common/PhaseType.cs`.

```csharp
public enum PhaseType
{
    Preparation = 0,
    Morning     = 1,
    Afternoon   = 2,
    Evening     = 3,
    Night       = 4
}
```

Preparation은 실제 영업 없는 준비 페이즈 (Day 시작). Morning~Night 4개가 실제 영업 사이클.

## 2. Phase 시작 시각 & 종료 시각

`ProgressService.SetPhaseTime()` — `Assets/Scripts/Unity/Common/ProgressService.cs:96`.

phase 진입 시 `Stats.SetTime(hour, minute)` 로 게임 내 시각 스냅:

| Phase | 시작 시각 (Stats.time 반영) |
|-------|---------------------------|
| Preparation | 05:00 (300분) |
| Morning | 07:00 (420분) |
| Afternoon | 12:00 (720분) |
| Evening | 17:00 (1020분) |
| Night | 22:00 (1320분) |

### 2.1 PhaseStartMinutes / PhaseEndMinutes

`ProgressService.cs:110`.

| Phase | Start (min) | End (min) | duration (min) |
|-------|-------------|-----------|----------------|
| Preparation | 300 (05:00) | 420 (07:00) | 120 (2h) |
| Morning | 420 (07:00) | 720 (12:00) | 300 (5h) |
| Afternoon | 720 (12:00) | 1020 (17:00) | 300 (5h) |
| Evening | 1020 (17:00) | 1320 (22:00) | 300 (5h) |
| Night | 1320 (22:00) | 1740 (익일 04:00) | 420 (7h) |

Night의 End=1740 은 24h(1440분) 넘김 — 자정 넘어 새벽 4시. 게임 상 실제 시침은 phase 종료 시점 이전에 Settlement 씬으로 이탈하므로 값 자체는 계산용 상한.

`ClockUI.SetTargetTime()` — `Assets/Scripts/Unity/UI/ClockUI.cs:37` 에서 이 값을 fill amount 계산에 사용:
```csharp
targetFill = Mathf.Clamp01((end - current) / 720f);  // 720분 = 12h 기준 정규화
```

## 3. PhaseData (저장 상태)

`Assets/Scripts/Schema/State/Common/PhaseData.cs`.

```csharp
public int Day;
public PhaseType Phase;
public List<string> UnlockedRecipes;   // 레거시 — 신 슬롯은 unlockedRecipes
public List<string> SelectedMenus;     // 레거시 — 신 슬롯은 recipeBook
```

기본값: `Day=1, Phase=Preparation`. `GameStart.NewGame()` 이후엔 `Day=0` 로 세팅 (첫 Settlement에서 Day=1로).

## 4. ProgressService

`Assets/Scripts/Unity/Common/ProgressService.cs`.

### 4.1 필드 / 프로퍼티

```csharp
public event Action<PhaseType> OnPhaseChanged;

private readonly Func<PhaseData> _phaseAccessor;
private int _cumulativePhaseIndex;   // Day * TotalPhaseCount + (int)Phase

public PhaseData PhaseData => _phaseAccessor?.Invoke();
public int CurrentPhaseIndex => _cumulativePhaseIndex;
public int TotalPhaseCount => 5;    // Prep + 4 영업
public int PhaseStartMinutes => ...;
public int PhaseEndMinutes   => ...;
```

`TimePhaseProvider` 인터페이스 구현 — `Assets/Scripts/Schema/Interfaces/Garden/TimePhaseProvider.cs`. `NextPhase()` 명시 구현: `PassPhase()` 위임. Garden의 crop 진행 관측 용도.

### 4.2 Initialize (NewGame)

`ProgressService.cs:32`.
```csharp
session.State.phase = new PhaseData();
_cumulativePhaseIndex = 0;
```

### 4.3 ApplySaveData (Continue)

`ProgressService.cs:43`.
```csharp
session.State.phase = data;
_cumulativePhaseIndex = data.Day * TotalPhaseCount + (int)data.Phase;
OnPhaseChanged?.Invoke(data.Phase);
```

### 4.4 PassPhase — 페이즈 진행

`ProgressService.cs:53`.

```csharp
if (pd.Phase == PhaseType.Night)
{
    SceneLoader.LoadScene(SceneNames.Settlement);
    return true;    // Day 전환은 Settlement 씬의 SaveRoutineAsync가 PassDay 호출
}
pd.Phase++;
_cumulativePhaseIndex++;
SetPhaseTime(pd.Phase);
OnPhaseChanged?.Invoke(pd.Phase);
return false;
```

- Preparation → Morning: 메뉴 선택 modal 확인 시 `MallSceneController.OpenMenuSelection` 콜백 (`Assets/Scripts/Unity/Common/MallSceneController.cs:180`) → PassPhase + Cooking 씬 진입.
- Morning/Afternoon/Evening → 다음 phase: `PhaseActionSelector.[Work]` 또는 GoHome confirm modal.
- Night → Settlement 씬 로드.

### 4.5 PassDay — 하루 전환

`ProgressService.cs:69`.

```csharp
stats.SubMoney(SettlementService.ManagementFee);   // -1000G

pd.Day++;
pd.Phase = PhaseType.Preparation;
_cumulativePhaseIndex++;
SetPhaseTime(pd.Phase);   // 05:00
OnPhaseChanged?.Invoke(pd.Phase);

stats.SetStamina(100);
GameRandom.InitDay(pd.Day);
Inventory.AdvanceDay();
Weather.UpdateWeather(pd.Day);
Settlement.Reset(stats.GetMoney());
SaveManager.SaveAll();
```

호출 지점: `SettlementController.SaveRoutineAsync()` — 씬 진입 프레임 다음 tick.

### 4.6 Die

`ProgressService.cs:90`. 스태미나 0으로 → PassDay. 현재 명시적 게임오버 없음.

## 5. TimeManager (실시간 → 게임 시간)

`Assets/Scripts/Unity/Common/TimeManager.cs`. `SingletonMonoBehaviour<TimeManager>`.

### 5.1 SerializeField (인스펙터)

```csharp
[Header("Global Time Settings")]
[SerializeField] private float gameTimeScale = 120f;    // 실시간 1s = 게임 시간 120s
[SerializeField] private int startHour = 11;
[SerializeField] private int startMinute = 0;
[SerializeField] private int endHour = 15;
[SerializeField] private int endMinute = 0;

[Header("Audio")]
[SerializeField] private AudioClip tickingSfx;

[Header("Balancing - Local Timers")]
[SerializeField] private float defaultCustomerWaitTime = 90f;
[SerializeField] private float closedLocalTimerScale = 2f;  // 영업 종료 후 대기 손님 인내심 가속 배율
```

`StartTimeMinutes = 11 * 60 = 660`, `EndTimeMinutes = 15 * 60 = 900`. Cooking 씬에서 이 window로 영업.
(Preparation~Night 페이즈 시각은 `ProgressService.PhaseStartMinutes`가 SSOT — TimeManager 값은 Cooking 씬 자체 설정.)

### 5.2 이벤트

```csharp
public event Action OnTimePaused;
public event Action OnTimeResumed;
public event Action OnTimeEnd;
public bool IsPaused { get; private set; }
```

### 5.3 InitializeTime

`TimeManager.cs:57`.
```csharp
IsPaused = false;
breakAction = null;
breakTargetTime = endHour * 60 + endMinute;

stats.SetTime(startHour, startMinute);
stats.OnTimeChanged += PlayTickingSfx;
```

### 5.4 게임 시간 진행 (Update 1부)

`TimeManager.cs:178`.
```csharp
if (!IsPaused)
{
    gameTimer += Time.deltaTime;
    float secondsPerGameMinute = 60f / gameTimeScale;  // 120x면 0.5s = 게임 1분

    while (gameTimer >= secondsPerGameMinute)
    {
        stats.AddTime(0, 1);
        gameTimer -= secondsPerGameMinute;
        CheckBreakPoint(stats.GetHour() * 60 + stats.GetMinute());
    }
}
```

### 5.5 CheckBreakPoint (영업 종료 트리거)

`TimeManager.cs:110`.
```csharp
if (nowMinutes >= breakTargetTime && breakTargetTime >= 0)
{
    PauseTime();
    breakTargetTime = -1;               // 한 번만
    localTimerScale = closedLocalTimerScale;  // 대기 손님 인내심 2x

    OnTimeEnd?.Invoke();
    breakAction?.Invoke();
}
```

`CustomerManager.Start()` 에서 `time.OnTimeEnd += OnTimeEnd` 구독 → 손님 스폰 종료.

### 5.6 로컬 타이머 (손님 인내심 등)

`TimeManager.cs:39`. `CustomTimer { id, duration, elapsed, onTick, onComplete, isDone }`.

API:
- `StartCustomerTimer(onTick, onComplete)` → `StartLocalTimer(defaultCustomerWaitTime, ...)` = **90초**.
- `StartLocalTimer(duration, onTick, onComplete)` 범용.
- `CancelTimer(id)`.

**Update 2부** (`TimeManager.cs:194`): `IsPaused` 와 **무관하게** 로컬 타이머 갱신 — 영업 종료 후 대기 손님 인내심이 흘러야 timeout → 자연 퇴장 가능. `localTimerScale = 2f` 로 가속.

### 5.7 SFX

`PlayTickingSfx(hour, minute)` — `OnTimeChanged` 구독 (매 분 tick 시 재생). `SoundManager.Instance.Play2DSFX(tickingSfx, 0.5f)`.

## 6. 시간 표시 (ClockUI)

`Assets/Scripts/Unity/UI/ClockUI.cs`.

### 6.1 SerializeField

```csharp
[SerializeField] private RectTransform hourHand;
[SerializeField] private Image fillImage;
[SerializeField] private float rotationSpeed = 5f;
[SerializeField] private float hourHandOffset = 0f;
```

### 6.2 이벤트 구독

`OnEnable`: `stats.OnTimeChanged += SetTargetTime`.

### 6.3 SetTargetTime

```csharp
targetHourAngle = -((hour % 12) / 12f * 360f + (minute / 60f) * 30f) + hourHandOffset;

int current = hour * 60 + minute;
int start, end;
if (TimeManager.Instance != null)   // Cooking 씬
{
    start = TimeManager.Instance.StartTimeMinutes;  // 660
    end   = TimeManager.Instance.EndTimeMinutes;    // 900
}
else if (GameSessionRoot.Instance?.Progress != null)  // Mall 등
{
    start = progress.PhaseStartMinutes;
    end   = progress.PhaseEndMinutes;
}

targetFillAngle = -(current / 720f * 360f);
targetFill      = Mathf.Clamp01((end - current) / 720f);

if (cachedEnd != end)   // 페이즈 전환 시 fill 0 스냅 방지
{
    cachedEnd = end;
    currentFill = targetFill;
}
```

시침은 lerp 회전, fill 은 lerp 감소. TimeManager 있으면 Cooking 씬 영업 시간, 없으면 페이즈 range를 fill 기준으로.

## 7. cumulativePhaseIndex

`ProgressService._cumulativePhaseIndex` — Day 0 시작 이후 절대 phase 카운터.

계산: `Day * 5 + (int)Phase`.

용례:
- `TimePhaseProvider.CurrentPhaseIndex` — 작물 성장 완료 판정. `FarmTile` 이 심을 때 phase index 기록 → 현재 phase index - 기록 phase index >= `crop.growPhaseCount * (1 - timeReduction)` 이면 수확 가능.
- `FarmTileSaveData.plantedPhase` 로 저장.

## 8. 이벤트 다이어그램

```
GameStart (NewGame)
  ├─ Progress.Initialize()  → PhaseData{Day=0, Phase=Preparation}
  └─ Stats.SetTime(5,0)     → time=300

Mall (Preparation)
  └─ GoHome → OpenMenuSelection → Confirm
       └─ Progress.PassPhase()
            ├─ Phase++ (Morning)
            ├─ Stats.SetTime(7,0)
            └─ OnPhaseChanged?.Invoke(Morning)
                 └─ MallSceneController.OnPhaseChangedInMall (Morning skip)
       └─ SceneLoader.LoadScene(Cooking / CookingTutorial)

Cooking (Morning / Afternoon / Evening / Night)
  ├─ TimeManager.InitializeTime()
  │    └─ Stats.SetTime(11,0)   // 씬 자체 startHour=11 override
  ├─ TimeManager.Update: gameTimer→ Stats.AddTime(0,1) every 0.5s (@120x)
  ├─ Stats.OnTimeChanged → ClockUI, CustomerManager 등
  └─ CheckBreakPoint (endHour=15 도달)
       ├─ PauseTime()
       ├─ OnTimeEnd → CustomerManager.OnTimeEnd (스폰 종료)
       └─ (activeCustomers 소진 시) → PhaseActionSelector 표시
Cooking → Mall
  └─ PhaseActionSelector [Work] → Cooking (다음 phase 없이 즉시)
                       [Rest] → PassPhase() + Stamina 100
                       [Shopping] → Mall UI 유지

Night 종료 시
  Progress.PassPhase() → Settlement 씬 로드

Settlement
  └─ SettlementController.SaveRoutineAsync (씬 진입 프레임+1)
       └─ Progress.PassDay()
            ├─ Stats.SubMoney(1000)  // 관리비
            ├─ Day++, Phase = Preparation
            ├─ Stats.SetStamina(100), SetTime(5,0)
            ├─ Inventory.AdvanceDay()
            ├─ Weather.UpdateWeather()
            ├─ Settlement.Reset()
            └─ SaveManager.SaveAll()
       └─ (아무 키 대기) → Mall 씬
```

## 9. 요약: 시간 관련 상수 & 필드

| 항목 | 위치 | 값 |
|------|------|-----|
| `TotalPhaseCount` | `ProgressService.cs:26` | 5 |
| `Preparation` 시작 시각 | `ProgressService.cs:102` | 05:00 |
| `Morning` 시작 시각 | `ProgressService.cs:103` | 07:00 |
| `Afternoon` 시작 시각 | `ProgressService.cs:104` | 12:00 |
| `Evening` 시작 시각 | `ProgressService.cs:105` | 17:00 |
| `Night` 시작 시각 | `ProgressService.cs:106` | 22:00 |
| `gameTimeScale` | `TimeManager.cs:8` | 120x |
| Cooking `startHour/endHour` | `TimeManager.cs:9-12` | 11:00 ~ 15:00 (SerializeField, 씬별 override 가능) |
| `defaultCustomerWaitTime` | `TimeManager.cs:19` | 90초 (실시간) |
| `closedLocalTimerScale` | `TimeManager.cs:21` | 2.0x (영업 종료 후) |
| `ManagementFee` | `SettlementService.cs:11` | 1,000G (PassDay 시 자동 차감) |
| Stats overflow 방지 | `StatsService.cs:78` | `time %= 1440`, ErrorLog |
