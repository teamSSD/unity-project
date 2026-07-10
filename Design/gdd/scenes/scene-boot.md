# Boot.unity — 부트 씬

> 파일: `Assets/Scenes/ForReal/Boot.unity`
> 진입점: 게임 실행 시 최초 로드되는 유일한 씬 (Build Settings 인덱스 0).

## 역할 한 줄

`Managers` → `GameStart` 순차 additive 로드 후 자기 자신은 unload — **매니저 부트스트랩 하네스**.

## GameObject 계층

```
Boot.unity (root)
└── BootLoader                 GameObject 1개뿐 (Camera/Light 없음)
    └── BootLoader.cs          (Game.Unity.BootLoader)
```

씬은 **로직 홀더**로만 존재. 렌더링 대상 없음 — 즉시 언로드되므로 시각 요소 불필요.

## BootLoader 실행 흐름

관련 스크립트: [`Assets/Scripts/Unity/Common/BootLoader.cs`](../../../Assets/Scripts/Unity/Common/BootLoader.cs)

```
Start (async UniTaskVoid)
 1. LoadSceneAsync(Managers, Additive)              await
 2. SetActiveScene(Managers)                        ← 매니저는 Boot이 아닌 Managers 소속
 3. ManagerBootstrap.EnsureAll()                    ← 싱글톤 매니저 6종 생성 (없을 때만)
 4. FontPreWarmer.WarmAll()                         ← SUIT 폰트 5종 pre-warm (runtime hitch 방지)
 5. LoadSceneAsync(GameStart, Additive)             await
 6. SceneLoader.SetCurrentScene(GameStart)
 7. SetActiveScene(GameStart)
 8. UnloadSceneAsync(Boot) fire-and-forget          ← 자기 자신 언로드
```

## ManagerBootstrap.EnsureAll — 생성되는 매니저

[`ManagerBootstrap.cs`](../../../Assets/Scripts/Unity/Common/ManagerBootstrap.cs)

`Ensure<T>()` 헬퍼로 `FindFirstObjectByType<T>()` 검색 → 없으면 새 GameObject에 `AddComponent<T>()`.
매니저 GO는 **Managers 씬이 activeScene일 때 생성**되므로 자동으로 그 씬에 귀속.

| 매니저 | 코드 위치 | 역할 |
|---|---|---|
| `LoadingManager`     | `Common/LoadingManager.cs`    | 씬 전환 페이드 캔버스 |
| `HUDManager`         | `Common/HUDManager.cs`        | 상단 HUD (시간/돈/체력) |
| `UIManager`          | `Common/UIManager.cs`         | 인벤토리·메뉴 등 오버레이 UI |
| `ShopUIAdapter`      | `Shop/ShopUIAdapter.cs`       | ShopBook prefab 인스턴스화 + 4탭 관리 |
| `SettingsUIManager`  | `Common/SettingsUIManager.cs` | 설정 창 |
| `RecipeBookManager`  | `Common/RecipeBookManager.cs` | 레시피북 (특수: PrefabCatalog에서 로드) |

## FontPreWarmer — 폰트 프리워밍

[`FontPreWarmer.cs`](../../../Assets/Scripts/Unity/Common/FontPreWarmer.cs)

**목적**: Dynamic 폰트 asset 5종에 게임 중 등장할 문자를 미리 add해 첫 표시 시 hitch 제거.

- 대상 폰트: `TextMesh Pro/Fonts/SUIT-{Regular, Medium, Bold, SemiBold, ExtraBold} SDF`
- 문자 소스: `Resources/font_prewarm_chars.txt` (Editor tool `_TempGenerateFontPrewarm`으로 생성)
- 재호출 안전: `_done` 플래그로 최초 1회만 실행

## GameStart 직접 진입 시 (개발 편의)

`GameStart.cs` `Start()` 내:
```csharp
if (GameSessionRoot.Instance?.Stats == null)
    ManagerBootstrap.EnsureAll();
```
Boot 씬을 안 거치고 Editor에서 GameStart를 직접 Play해도 매니저가 보장됨.

## 초기 상수

없음 — Boot 자체는 상수 없이 순차 로드만 담당. 씬 이름은 [`SceneNames.cs`](../../../Assets/Scripts/Unity/Common/SceneNames.cs)에서 관리.

## 관련 시스템

- [Managers 씬](./scene-managers.md) — Boot이 첫 번째로 로드하는 대상
- [GameStart 씬](./scene-gamestart.md) — Boot이 두 번째로 로드하는 대상
