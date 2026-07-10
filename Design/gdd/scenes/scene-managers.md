# Managers.unity — 매니저 하이브 씬

> 파일: `Assets/Scenes/ForReal/Managers.unity`
> 로드: Boot이 최초 additive 로드. 이후 게임 종료까지 상주.

## 역할 한 줄

**게임 세션 전역 상태**(Service POCO 트리, catalog, UI 오버레이)를 담는 씬. 씬 전환에 영향받지 않는 모든 것이 여기 산다.

## 씬 루트 GameObject 7개

Managers.unity에는 **디자인타임에 배치된 매니저 7개**가 있다. Boot의 `ManagerBootstrap.EnsureAll()`은 `LoadingManager/HUDManager/UIManager/ShopUIAdapter/SettingsUIManager/RecipeBookManager` 6종을 **런타임에 추가로** 생성(Ensure)한다.

```
Managers.unity (root, activeScene 상태로 EnsureAll 호출됨)
├── CatalogProvider          [DefaultExecutionOrder(-1000)]  Singleton
├── GameSessionRoot          [DefaultExecutionOrder(-999)]   Singleton, GameState POCO 보관
├── SoundManager             Singleton, DontDestroyOnLoad
├── TutorialController       Singleton, tutorial step orchestrator
├── TutorialOverlayCanvas    Canvas overlay (bubble/anchor 표시)
├── ConfirmModal             Singleton, 코드로 UI 동적 생성
└── TextureDiagnostics       주기 스캔으로 sprite null / 미가시 렌더러 검출
```

## 각 매니저 상세

### CatalogProvider — 카탈로그 진입점
[`Assets/Scripts/Unity/Common/CatalogProvider.cs`](../../../Assets/Scripts/Unity/Common/CatalogProvider.cs)

`SingletonMonoBehaviour<CatalogProvider>` — 모든 catalog SO에 대한 정적 접근.
`Resources.Load/LoadAll` 대체. 인스펙터에서 각 catalog 에셋 드래그 등록.

- **Cooking**: `FoodCatalogSO food`, `RecipeCatalogSO recipe`, `IngredientCatalogSO ingredient`, `CookingToolCatalogSO cookingTool`
- **Mall/Delivery**: `DeliveryNpcCatalogSO deliveryNpc`, `DialogueConfigCatalogSO dialogueConfig`, `NpcNormalDialogueCatalogSO npcNormalDialogue`
- **Prefabs**: `PrefabCatalogSO prefabs`
- **Shop**: `ShopConfigSO foodShopConfig`
- **Csv**: `CsvCatalogSO csvs` (deliveryQuest, cropData, farmUpgrade, storageUpgrade, toolUpgrade)
- **Sprites**: `CropSpriteCatalogSO cropSprites`
- **BGM**: `AudioClip bgmMall / bgmCooking / bgmNight`

**ExecutionOrder(-1000)** — 같은 씬의 다른 싱글톤보다 먼저 Awake. `GameSessionRoot.Awake()`가 안전하게 `CatalogProvider.X`에 접근 가능하도록 보장.

### GameSessionRoot — Composition Root (ADR-001 Option B)
[`Assets/Scripts/Unity/Common/GameSessionRoot.cs`](../../../Assets/Scripts/Unity/Common/GameSessionRoot.cs)

**`GameState State` POCO 보관 + 모든 Service의 wiring 진입점**.

`OnSingletonAwake()`에서 실행:
1. `State = new GameState()` — persistent/session state 트리 생성
2. `WireServices()` — 17개 Service 인스턴스화
   - `StatsService`, `ProgressService`
   - `CropCatalogService`, `FarmUpgradeService`, `StorageUpgradeService`, `ToolUpgradeService`
   - `InventoryService`, `PurchaseService`
   - `DeliveryQuestService`, `NpcNormalDialogueService`, `OrderService`, `QuestMenuCatalog`
   - `MenuSelectionService`, `UnlockedFoodService`, `RecipeLookupService`
   - `WeatherService`, `SettlementService`, `TutorialService`

**ExecutionOrder(-999)** — CatalogProvider(-1000) 다음. SoundManager 등 facade가 Awake 시 `GameSessionRoot.State` 접근 안전 보장.

### SoundManager — 오디오
[`Assets/Scripts/Unity/Common/SoundManager.cs`](../../../Assets/Scripts/Unity/Common/SoundManager.cs)

- `bgmSource`, `sfxSource`, `loopSfxSource` 3종 AudioSource
- 페이즈 변화 감지 → BGM 자동 전환 (`bgmMall`/`bgmCooking`/`bgmNight`)
- 씬 전환 시 AudioListener 유무 감지 → SFX 소스 활성/비활성
- 씬 로드 후 모든 Button에 클릭 SFX 자동 부착
- UI SFX: `uiBookSfx`, `buttonClickSfx`

### TutorialController — 스텝 오케스트레이터
[`Assets/Scripts/Unity/Tutorial/TutorialController.cs`](../../../Assets/Scripts/Unity/Tutorial/TutorialController.cs)

- `TutorialStepCatalog catalog` — 스텝 정의
- `TutorialBubble bubblePrefab` — 말풍선 프리팹
- `RectTransform overlayCanvas` — TutorialOverlayCanvas 참조
- `Show(stepId)`: 스텝 내 파트 순차 표시 (space dismiss로 진행)
- `RegisterTarget(key, TutorialTarget)`: 씬의 앵커 등록 → bubble이 world position 추적

### TutorialOverlayCanvas — 말풍선/앵커 오버레이
- Canvas (ScreenSpaceOverlay 예상, sortingOrder 높음)
- TutorialController가 bubblePrefab 자식으로 인스턴스화
- 씬 전환에도 살아남는 튜토리얼 표시 전용 캔버스

### ConfirmModal — Yes/No 모달
[`Assets/Scripts/Unity/Common/ConfirmModal.cs`](../../../Assets/Scripts/Unity/Common/ConfirmModal.cs)

- 최초 호출 시 Canvas + Panel을 **코드로 동적 생성**, 이후 재사용
- `ConfirmModal.Show(title, message, onConfirm, ...)` 한 번 호출로 사용
- `ConfirmModal.IsOpen` — 다른 UI(레시피북 등)의 ESC 가드용
- Escape 키 자동 → No

### TextureDiagnostics — 스프라이트 진단
[`Assets/Scripts/Unity/Common/TextureDiagnostics.cs`](../../../Assets/Scripts/Unity/Common/TextureDiagnostics.cs)

- `scanIntervalSec = 3f` — 주기적 SpriteRenderer 스캔
- 씬 진입 시 개수/누락 요약, null sprite/null material 로그
- 재현 시 GameObject 경로 콘솔 출력 → 어디서 스프라이트 사라졌는지 즉시 확인

## Singleton + DontDestroyOnLoad

모든 매니저는 `SingletonMonoBehaviour<T>` 상속.
`OnSingletonAwake()`에서 필요하면 `DontDestroyOnLoad(gameObject)` 호출 (예: `ShopUIAdapter`).
그러나 Managers 씬 자체가 언로드되지 않으므로 대부분은 DDoL 없이도 유지됨.

## 관련 시스템

- [Boot 씬](./scene-boot.md) — Managers를 최초 로드
- [GameStart 씬](./scene-gamestart.md) — Managers 로드 완료 후 이어짐
- ADR-001 (Composition Root), ADR-008 (Singleton 최소화) — `tmp/refactor-2026-05/decisions/`
