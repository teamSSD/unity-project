# Mall.unity — 상가 뷰

> 파일: `Assets/Scenes/ForReal/Mall.unity`
> 진입 조건: New Game / Continue, Cooking/Shop/Garden/Settlement 씬 복귀.

## 역할 한 줄

**5층 상가 사이드-스크롤 뷰**. 플레이어가 층/가게/텃밭길을 오가며 페이즈별 액션(요리/텃밭/쇼핑/휴식)을 선택하는 허브.

## GameObject 계층 (root)

```
Mall.unity
├── EventSystem
├── Stores                            상가 컨테이너
│   ├── Floor1 ~ Floor5              층별 그룹 (스토어 프리팹 인스턴스 다수)
│   │   ├── store1..store14         일반 배경 스토어
│   │   ├── playerStore             ← Floor3 소속 (플레이어 가게)
│   │   │   └── TutorialAnchor_Door  튜토리얼 앵커 (문)
│   │   └── meat_store              ← Floor3 소속 (특수: 정육점)
│   └── dummy / 3floor              보조 오브젝트
├── DialogueManager                   (Game.Runtime::DialogueManager)
├── Canvas                            상단 HUD·UI 오버레이
├── NPCs                              NPC placement 컨테이너
├── Background
│   ├── Stairs                        층간 계단
│   │   ├── upper / lower / visual   각 계단 짝 (upper↔lower로 왕복)
│   │   │   └── Triangle x4~5        스텝 콜라이더 (5개까지)
│   ├── ground                       배경 레이어
│   │   └── layer 1..layer 6        패럴랙스 6개층
│   └── FarmPath                     텃밭 진입 인터랙션
├── MallController                    (Game.Runtime::MallSceneController)
├── TutorialAnchor_Head              (별도 root — 튜토리얼 카메라 앵커)
└── Main Camera
```

## 붙어있는 스크립트

| 클래스 | 위치 | 부착 대상 |
|---|---|---|
| `MallSceneController` | `Unity/Common/MallSceneController.cs` | MallController GO |
| `DialogueManager`     | `Unity/Mall/DialogueManager.cs`       | DialogueManager GO |
| `DynamicBoundaryWalls`| `Unity/Common/DynamicBoundaryWalls.cs`| 카메라 경계벽 (동적) |
| `StairsInteraction`   | `Unity/Mall/StairsInteraction.cs`     | 각 upper/lower Stair 노드 |
| `FarmPathInteraction` | `Unity/Mall/FarmPathInteraction.cs`   | FarmPath GO |
| `SceneTransitionInteraction` | `Unity/Mall/SceneTransitionInteraction.cs` | 씬 전환 트리거 (Shop 등) |
| `GoHomeInteraction`   | `Unity/Mall/GoHomeInteraction.cs`     | playerStore trigger |
| `TutorialTarget`      | `Unity/Common/TutorialTarget.cs`      | TutorialAnchor_Door / TutorialAnchor_Head |

## MallSceneController — 씬 오케스트레이터

[`MallSceneController.cs`](../../../Assets/Scripts/Unity/Common/MallSceneController.cs)

### 인스펙터 필드
```csharp
[SerializeField] private Button goHomeButton;               // (레거시) 상단 UI GoHome 버튼
[SerializeField] private GameObject bentoSelectionPrefab;   // 도시락 메뉴 선택 modal prefab
[SerializeField] private GameObject phaseSelectionPrefab;   // Work/Rest/Shopping 선택 UI prefab
```

### Start() 흐름
1. **키보드 nav 차단** — `EventSystem.sendNavigationEvents = false`. Space/Enter로 버튼 실수 트리거 방지 (플레이어 이동 키와 충돌).
2. **플레이어 위치 복원** — `SceneLoader.MallReturnPosition` 있으면 override. `CameraFollow.SnapToPlayer()` 즉시 스냅.
3. **Preparation 페이즈면 메뉴 초기화** — `MenuSelection.ClearAllMenus()`.
4. **BentoSelection 인스턴스화** — Prefab을 Instantiate → `Inject(unlockedFood, menuSelection)` → `Close()`.
5. **PhaseSelection 인스턴스화** — Overlay Canvas 찾거나 생성 후 자식으로 → `Hide()`. `PhaseActionSelector.OnActionExecuted += OnPhaseActionExecuted`.
6. **GoHomeButton 리스너** — `goHomeButton.onClick += GoHome`.
7. **자동 UI 결정** — 진입 시 현재 페이즈 + 이전 씬 조합으로:
   - Preparation/Morning: 자동 UI 없음
   - Shop/Garden 복귀: 유지 (다시 강요 X)
   - 그 외 (Cooking 복귀 등): `OpenActionSelection()` 자동 표시
8. **웰컴 튜토리얼** — Preparation에서 `TutorialStepId.WelcomeAtSpawn` 시도.

### GoHome() — playerStore 상호작용 또는 UI 버튼
- **Preparation**: `OpenMenuSelection()` — 도시락 4칸 선택 → 확인 시 PassPhase + Cooking(또는 CookingTutorial) 자동 로드
- **Afternoon + Closing 튜토리얼 활성**: Closing 스텝 표시 → 완료 후 Settlement 씬으로
- **그 외**: `ConfirmModal` "이 페이즈를 마치시겠습니까?" → 확인 시 `Progress.PassPhase()`

Night 페이즈일 때 문구는 "하루를 마치시겠습니까? / 잠들면 다음 날이 시작됩니다."로 변경.

### OnPhaseChangedInMall(newPhase)
Mall 체류 중 페이즈 전환(Rest 액션 등) 시 새 페이즈 UI 자동 표시. Preparation/Morning은 skip.

### 튜토리얼 훅
- **PhaseSelectAfternoon**: Afternoon 진입 시 Work/Rest 비활성, Part 0에선 Shopping도 비활성 (설명 중 실수 클릭 방지), Part 1 진입 시 Shopping 활성. Shopping 클릭 → MallCorridor 튜토리얼로 연결.
- **MallCorridor**: 복도/텃밭/상점/NPC/복귀 안내.

## StairsInteraction — 층간 이동

[`StairsInteraction.cs`](../../../Assets/Scripts/Unity/Mall/StairsInteraction.cs)

```csharp
[SerializeField] private Transform targetStair;   // 짝 계단 (upper↔lower)
[SerializeField] private string promptText = "press spacebar";
```

- Spacebar 상호작용, `isClimbing` 정적 락으로 중복 방지
- `PlayerMove.enabled = false` + `Rigidbody2D.simulated = false` — 이동 중 물리 차단
- 2단계 이동: 현재 계단 위치 → 짝 계단 위치 (모두 `PlayerMove.moveSpeed`)
- `Animator.SetBool("IsMoving", true)` + `SpriteRenderer.flipX` 자동 반영
- `UILockManager.Lock(Owner.Loading)` 계단 중 입력 차단

## FarmPathInteraction — 텃밭 진입

```csharp
Spacebar → SceneLoader.SetMallReturnPosition(transform.position)
        → SceneLoader.LoadScene(Garden)
```

Mall 복귀 시 정확히 원래 자리로 (`SceneTransitionInteraction`의 spawnX와 동일 패턴).

## SceneTransitionInteraction — 씬 전환 트리거

Shop 등 서브 씬 진입에 재사용되는 범용 컴포넌트.
```csharp
[SerializeField] private string targetScene;
[SerializeField] private float spawnX;   // Mall 복귀 X (0이면 transform.x)
```

Spacebar → `SceneLoader.SetMallReturnPosition(x, y, 0)` + `LoadScene(targetScene)`.

## GoHomeInteraction — playerStore trigger

playerStore 앞에 트리거 콜라이더. 플레이어 진입 후 Space → `MallSceneController.GoHome()` 호출.
`spawnX`로 다음 Mall 진입 시 복귀 위치 지정.

## DynamicBoundaryWalls

카메라 이동 범위 밖으로 플레이어가 나가지 않도록 **런타임에 벽 콜라이더를 동적 배치**. 카메라 뷰포트 좌우 경계 추적.

## PhaseActionSelector 위치

MallSceneController가 런타임에 `phaseSelectionPrefab`을 인스턴스화. Overlay Canvas의 자식으로 붙음. 화면 중앙 카드 3장(Work/Rest/Shopping) UI. 상세는 [`PhaseActionSelector.cs`](../../../Assets/Scripts/Unity/UI/PhaseActionSelector.cs).

## 관련 시스템

- [Shop 씬](./scene-shop.md) — playerStore 밖 SceneTransition으로 진입
- [Garden 씬](./scene-garden.md) — FarmPath로 진입
- [Cooking 씬](./scene-cooking.md) — Preparation 페이즈 도시락 선택 후 자동 진입
- [Settlement 씬](./scene-settlement.md) — Night PassPhase 또는 튜토리얼 Closing 시 진입
- TutorialSteps: `WelcomeAtSpawn`, `PhaseSelectAfternoon`, `MallCorridor`, `Closing`
