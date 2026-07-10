# GameStart.unity — 시작 화면

> 파일: `Assets/Scenes/ForReal/GameStart.unity`
> 로드: Boot의 두 번째 대상. 매니저 준비 완료 후 표시되는 첫 인터랙티브 화면.

## 역할 한 줄

**New Game / Continue / Settings** 3버튼 스타트 메뉴. 저장 데이터 유무에 따라 Continue 활성/비활성.

## GameObject 계층

```
GameStart.unity (root)
├── Main Camera
├── EventSystem
├── ActionSelectionManager    ─┐
├── ProgressSystem            ─┤ (레거시 placeholder GO — 실제 매니저는 Managers 씬)
├── RecipeDataManager         ─┤
├── StatsSystem               ─┘
└── Canvas                    ScreenSpaceOverlay
    ├── newGame (Button + Image + Text (TMP))
    ├── Continue (Button + Image + Text (TMP))
    ├── Settings (Button + Image + Text (TMP))
    └── (title/etc TextMeshProUGUI)
```

> 4개의 시스템 GO(ActionSelectionManager/ProgressSystem/RecipeDataManager/StatsSystem)는 리팩터링 이전의 잔재. 지금은 `GameSessionRoot` + Service로 통합됐다.

## 붙어있는 스크립트

- `Game.Runtime::GameStart` (Canvas 또는 root GO에 부착)
- Button / Image / TextMeshProUGUI / CanvasScaler / GraphicRaycaster
- EventSystem / StandaloneInputModule

## GameStart.cs 흐름

관련 스크립트: [`Assets/Scripts/Unity/Common/GameStart.cs`](../../../Assets/Scripts/Unity/Common/GameStart.cs)

### Start()
```csharp
// 1. Boot 미경유 대비 (Editor 직접 Play)
if (GameSessionRoot.Instance?.Stats == null)
    ManagerBootstrap.EnsureAll();

// 2. 유저 입력 잠금
UILockManager.Lock(UILockManager.Owner.GameStart);

// 3. Continue 버튼 상태 결정
bool hasSaveData = SaveManager.HasSaveData();
ContinueButton.interactable = hasSaveData;
if (!hasSaveData) ContinueButton.colors.disabledColor = (0.5,0.5,0.5,0.5);

// 4. 버튼 리스너 연결
NewGameButton  → NewGame()
ContinueButton → ProcessContinue()
Settings       → OpenSetting()
```

### ProcessContinue() — 저장 로드 흐름

```csharp
UILockManager.Unlock(GameStart);
SceneLoader.LoadSceneWithInit(Mall, () => {
    InitializeManagers();                      // Stats.Reset + Progress.Initialize (구조 초기화)
    GameSessionRoot.Instance?.Order.Clear();

    SaveManager.LoadAll();                     // gamedata.json 로드 → State 트리 복원

    // 시드 복원: immutableSeed(저장) + 매 세션 새 seed
    var saveData = session.Stats.GetSaveData();
    int day = session.Progress.PhaseData.Day;
    GameRandom.InitSession(saveData.immutableSeed, newSessionSeed);
    GameRandom.InitDay(day);
    session.Weather.UpdateWeather(day);

    HUDManager.Instance.Initialize();
});
```

### NewGame() — 신규 게임 흐름

```csharp
UILockManager.Unlock(GameStart);
SceneLoader.LoadSceneWithInit(Mall, () => {
    InitializeManagers();
    ApplyNewGameDefaults();                    // 초기값 설정 (아래)

    // 새 시드 생성 및 저장
    session.Stats.GetSaveData().immutableSeed = now;
    GameRandom.InitSession(now, newSessionSeed);
    GameRandom.InitDay(0);
    session.Weather.UpdateWeather(0);

    SaveManager.SaveAll();                     // 즉시 새 저장 생성
    HUDManager.Instance.Initialize();
});
```

### ApplyNewGameDefaults() — 신규 게임 초기값

| 항목 | 값 |
|---|---|
| 시각 | 05:00 |
| 소지금 | 12,000 G |
| 스태미나 | 100 |
| Day | 0 |
| 인벤토리 | `InventoryService.ResetToDefault()` |
| 언락 레시피 | `UnlockedFoodService.UnlockDefaultRecipes()` |
| NPC 대화 상태 | `DeliveryNpcDialogueInteraction.ResetAll()` |
| 밭 타일 | `garden.persistent.tiles` Array.Clear |

### OpenSetting()

`SettingsUIManager.Instance?.Open()` — 설정 창 오버레이.

## UI 배치 (인스펙터에서 wire)

`GameStart` 컴포넌트 노출 필드:
```csharp
[SerializeField] private Button ContinueButton;
[SerializeField] private Button NewGameButton;
[SerializeField] private Button Settings;
```

셋 다 필수 (OnValidate → `RequiredFieldValidator.Validate`).

## 관련 시스템

- [Boot 씬](./scene-boot.md) — GameStart를 로드하는 진입점
- [Mall 씬](./scene-mall.md) — 두 흐름 모두 Mall로 이어짐
- `SaveManager` — 단일 `gamedata.json` (레거시 5-file → 1-file 마이그레이션 포함)
- `GameRandom` — immutableSeed(저장) + sessionSeed(런타임) 이중 시드
