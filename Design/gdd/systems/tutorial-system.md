# 튜토리얼 시스템 (Tutorial)

Aftertaste의 튜토리얼은 **최초 세션 1회**만 재생되는 스크립트형 안내다. 각 스텝은 `TutorialStepData` SO로 정의되며, 하나의 스텝은 여러 개의 파트(bubble)로 나뉘어 순차 표시된다. 진행 상태는 `TutorialService`가 저장·조회하고, `TutorialController`가 UI 스폰과 dismiss 로직을 오케스트레이트한다.

## 컴포넌트 지도

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Unity/Tutorial/TutorialStepId.cs` | 스텝 ID 상수 (1~16). |
| `Assets/Scripts/Unity/Tutorial/TutorialStepData.cs` | SO — `stepId` + `List<TutorialStepPart> parts`. |
| `Assets/Scripts/Unity/Tutorial/TutorialStepCatalog.cs` | SO — 전체 스텝 List. `Get(stepId)`. |
| `Assets/Scripts/Unity/Tutorial/TutorialController.cs` | Managers 씬 싱글턴. `Show(stepId, onDone)`, 파트 전환, dismiss. |
| `Assets/Scripts/Unity/Tutorial/TutorialBubble.cs` | 개별 말풍선 (VLG+CSF, tail 방향). |
| `Assets/Scripts/Unity/Tutorial/TutorialTarget.cs` | 씬 오브젝트에 붙여 key 등록 (`GetScreenPosition`). |
| `Assets/Scripts/Unity/Tutorial/TutorialCookingController.cs` | `CookingTutorial` 격리 씬 전용 mock. 재료 무한 리필, 손님 1명, 시간 정지. |
| `Assets/Scripts/Domain/Common/TutorialService.cs` | POCO Service. `HasShown(stepId)`, `MarkShown`, `Complete`, save data. |

## 스텝 카탈로그 (실제 asset)

**위치**: `Assets/Bundles/TutorialSteps/`

| stepId | 상수명 | asset 파일 | 트리거 위치 |
|---:|---|---|---|
| 1 | `WelcomeAtSpawn` | `01_WelcomeAtSpawn.asset` | `MallSceneController.TryStartWelcomeTutorial` (Preparation 첫 진입) |
| 2 | `MenuSelection` | `02_MenuSelection.asset` | `BentoSelectionController.Show`에서 `CanShow` 시 |
| 3 | `CookingIntro` | `03_CookingSequence.asset` | `CookingSceneManager.Start`에서 `CanShow` 시 |
| 10 | `PhaseSelectAfternoon` | `10_PhaseSelectAfternoon.asset` | `MallSceneController.TryStartPhaseSelectTutorial` (Afternoon 진입) |
| 11 | `MallCorridor` | `11_MallCorridor.asset` | Step 10 완료 후 자동 (`TryStartMallCorridorTutorial`) |
| 16 | `Closing` | `16_Closing.asset` | Afternoon `GoHome` 시도 시 (마지막 → `Complete()` + Settlement) |

**Catalog SO**: `Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset` — 위 6개 asset 참조.

**미배치 상수 (TutorialStepId 헤더에는 있지만 asset 미존재)**: `ToolGriddle=4`, `ToolFire=5`, `ToolSauce=6`, `ToolMix=7`, `ToolSlice=8`, `RecipeTab=9`, `MallStairs=12`, `MallStore=13`, `MallNPC=14`, `MallReturnToStore=15`. 현재 프로덕션은 도구/도구별 안내를 `03_CookingSequence` 스텝 안 여러 파트로 통합함.

## 스텝 콘텐츠 (전 파트 dump)

### 1. `01_WelcomeAtSpawn` (Welcome, 6 파트)

모든 파트 `targetKey=player` (마지막만 `playerStore`), `dismissKey=Space`.

1. "Aftertaste 상가에 오신걸 환영합니다!"
2. "게임 시작에 앞서 게임 방식에 대해 간단히 안내드리겠습니다."
3. "이동은 A/D 또는 ←/→ 키로 할 수 있습니다."
4. "ESC를 누르면 띄워둔 창을 닫거나 설정을 열 수 있습니다."
5. "스페이스바를 통해 상호작용이 가능합니다."
6. "가게 문쪽에서 스페이스바를 눌러 하루 영업을 시작해봐요." (targetKey=`playerStore`)

### 2. `02_MenuSelection` (5 파트)

`BentoSelectionController` UI 위에서 진행.

1. targetKey=`bento-slots`, offset(-661,205): "도시락 조합은 하루에 1개에서 3개까지 가능합니다."
2. targetKey=`bento-slots`, offset(-678,277): "도시락 하나당 메인 메뉴 / 사이드 메뉴 각 1개에서 3개까지 선택이 가능합니다."
3. targetKey=`menu-items`, offset(-639,102): "메뉴를 클릭하여 선택/취소 할 수 있습니다."
4. targetKey=`confirm-button`, dir=Up, offset(83,-43), fraction=0.7: "조합이 끝났다면 확인을 눌러 넘어갈 수 있습니다."
5. targetKey=`back-button`, dir=Up, offset(-84,-43), fraction=-0.7: "다시 상가로 돌아가 영업준비를 계속 할 수 있습니다."

### 3. `03_CookingSequence` (15 파트 — 가장 큰 스텝)

`CookingTutorial` 격리 씬에서 실행. `TutorialCookingController`가 mock 손님 흐름과 카메라를 관리.

| # | targetKey | dismissKey | 옵션 | message |
|---:|---|---|---|---|
| 1 | refrigerator | Space | `blockRecipeBookOpen` | "원하는 재료들을 조리도구에 드래그 드랍 한 후, 조리도구를 클릭하면 요리 미니게임이 시작됩니다!" |
| 2 | tool-T005 | Space | `blockRecipeBookOpen` | "보여지는 방향에 맞추어 방향키를 입력하세요!" |
| 3 | tool-T001 | Space | `blockRecipeBookOpen` | "스페이스바로 화력을 조절하세요. 삼각형이 초록 게이지에 유지 될수록 점수가 올라갑니다!" |
| 4 | tool-T002 | Space | `blockRecipeBookOpen` | (동일 메시지) |
| 5 | tool-T003 | Space | `blockRecipeBookOpen` | "소스 뿌리기: 위/아래 연타 / 섞기: 스페이스바 연타" |
| 6 | tool-T004 | Space | `blockRecipeBookOpen` | "안내선을 따라 재료를 정확하게 자르세요!" |
| 7 | tool-T004 | **Tab** | (block 해제) | "탭(Tab)을 눌러 레시피북을 열어보세요." |
| 8 | (없음, offset(-406,316)) | Space | `forceRecipeBookOpen`, `blockRecipeBookOpen` | "메뉴를 클릭하여 상세 레시피를 확인 할 수 있습니다." |
| 9 | (없음, offset(590,315), dir=Up, fraction=0.5) | **ESC** | `dismissOnRecipeBookClose`, `blockRecipeBookOpen` | "ESC나 X 버튼으로 레시피북을 닫을 수 있어요." |
| 10 | customer | None | `allowSceneInteraction`, `autoFlipByScreenSide` | "손님이 오면 클릭해서 주문을 받아요." |
| 11 | bento-pile | None | `allowSceneInteraction`, `freeCamera` | "도시락을 원하는 자리에 놓아주세요." |
| 12 | placed-bento | None | `allowSceneInteraction`, `freeCamera` | "완성한 요리를 도시락에 담아주세요." |
| 13 | receipt (dir=Up) | None | `allowSceneInteraction`, `freeCamera`, `autoFlipByScreenSide` | "영수증을 도시락에 붙이면 손님이 가져갑니다." |
| 14 | taking-customer | None | `allowSceneInteraction`, `freeCamera` | "시간 안에 처리하지 못하면 손님이 그냥 나갈 수 있어요." |
| 15 | skip (dir=Up, fraction=0.5) | None | `allowSceneInteraction`, `freeCamera` | "오늘은 손님이 더 오지 않을 것 같네요. 이 버튼을 눌러 다음으로 넘어가요." |

파트 10-15는 `dismissKey=None` → 유저 키 입력이 아닌 **씬 이벤트로만 dismiss**. `TutorialCookingController.HandlePartShown`이 message prefix로 라우팅하고, mock 이벤트 발생 시 `DismissActivePart()`.

### 10. `10_PhaseSelectAfternoon` (2 파트)

1. targetKey="" (화면 중앙), Space: "점심, 저녁, 밤 각 페이즈 마다 영업 여부를 선택할 수 있습니다."
2. targetKey=`phase-shopping`, dismissKey=None (Shopping 버튼 클릭으로만 dismiss), `allowSceneInteraction`: "이번 점심에는 상가 이동을 선택해 봅시다."

`MallSceneController`는 이 스텝 재생 중 Work/Rest 버튼을 비활성, Shopping은 파트1 진입까지 비활성 (실수 클릭 방지).

### 11. `11_MallCorridor` (4 파트)

모든 파트 targetKey="" (중앙), Space.

1. "재료 수급을 선택하게 되면 상가 복도로 이동하게 됩니다."
2. "옥상 층에는 특정 재료를 기르고 수확할 수 있는 텃밭이 존재하며, 가게의 좌측편에는 재료를 재화로 구매하거나 텃밭, 도구, 창고 업그레이드가 가능한 상점이 존재합니다."
3. "또한 영업 준비시간이나 재료 수급 시간에는 상가를 돌아다니며 만나게 되는 인물로 부터 밤 시간에 진행될 배달 주문들을 받을 수 있습니다."
4. "영업 준비를 마친 후 다시 가게로 돌아가면 다음 영업 페이즈를 선택 할 수 있습니다."

### 16. `16_Closing` (1 파트)

targetKey="" (중앙), Space: "이제 진짜 시작이에요."

Closing dismiss 후 `TutorialController.Complete()` → Settlement 씬 로드 → Day 1 시작.

## TutorialStepPart 옵션

`Assets/Scripts/Unity/Tutorial/TutorialStepData.cs`:

| 필드 | 의미 |
|---|---|
| `message` | 안내 텍스트 (짧게 한 줄 권장). |
| `optionalImage` | 미니게임 스냅샷 등. |
| `targetKey` | `TutorialTarget` 등록 key. 비우면 화면 중앙. |
| `tailDirection` | `Down`(bubble이 target 위) / `Up`(아래). |
| `screenOffset` | 파트별 offset (px). 미세 조정. |
| `tailHorizontalFraction` | -1(왼쪽 코너) ~ +1(오른쪽 코너). |
| `dismissKey` | 기본 Space. Tab/ESC/None(씬 이벤트 전용) 설정 가능. |
| `autoFlipByScreenSide` | target 스크린 X 위치에 따라 fraction 부호 자동 반전. |
| `dismissOnRecipeBookClose` | 레시피북 실제 닫힘 이벤트로만 dismiss. |
| `allowSceneInteraction` | UILockManager 해제(Tutorial owner + CookingTutorial owner). 유저가 손님 클릭·도시락 드래그 등을 해야 하는 파트. |
| `freeCamera` | HorizontalCameraMove 활성 (유저 카메라 조작 허용). 미체크시 튜토리얼이 target으로 강제 이동. |
| `forceRecipeBookOpen` | Tab/ESC로 안 닫힘. "메뉴 클릭" 안내 파트용. |
| `blockRecipeBookOpen` | Tab으로 레시피북 여는 것 차단. |

## TutorialController (오케스트레이션)

### Show(stepId, onDone)

1. Catalog에서 `TutorialStepData` 조회.
2. `ShowPart(step, 0, stepId, onDone)` — 재귀적으로 각 파트 표시.

### ShowPart 흐름

```csharp
if (idx >= step.parts.Count) {
    RecipeBookManager.TutorialForceOpen = false;
    RecipeBookManager.TutorialBlockOpen = false;
    GameSessionRoot.Instance?.Tutorial?.MarkShown(stepId);
    SaveManager.SaveAll();
    onDone?.Invoke();
    return;
}
DestroyActive();
// 파트 flag 적용
RecipeBookManager.TutorialForceOpen = part.forceRecipeBookOpen;
RecipeBookManager.TutorialBlockOpen = part.blockRecipeBookOpen;
// Bubble spawn
_active = Instantiate(bubblePrefab, overlayCanvas);
_active.SetContents(part.message);
_active.SetImage(part.optionalImage);
// Position — target이 이동해도 매 프레임 재계산 (EnableDynamicFollow)
Vector2 initialScreenPos = ResolveScreenPos(targetKey) + offset;
_active.PlaceAtScreenPoint(initialScreenPos, dir, effectiveFraction(...));
_active.SetDismissKey(part.dismissOnRecipeBookClose ? KeyCode.None : part.dismissKey);
if (part.dismissOnRecipeBookClose)
    RecipeBookManager.Instance.OnRecipeBookClosed += bookClosedHandler;
OnPartShown?.Invoke(stepId, idx, part);
_active.EnableInteractiveDismiss(next, lockInput: !part.allowSceneInteraction);
```

### DismissActivePart()

외부 조건(mock 이벤트 등)이 현재 파트 강제 dismiss. `TutorialCookingController`가 이 API로 손님 흐름 → 튜토리얼 진행을 연결.

## TutorialBubble 렌더

**구조**:

```
Root (pivot 0.5 0) — position = target screen point
├─ Body (VLG+CSF 자동 크기)
│  ├─ OptionalImage
│  └─ Contents (TMP)
└─ Tail (pivot 0 0, tip at root origin)
```

- `tailBodyOverlap=25px` — body와 tail base 사이 seamless overlap.
- `tailTipVisualPadding=8px` — sprite bottom 투명 padding 보정.
- `EnableDynamicFollow` — 매 프레임 `screenPosGetter()` + `fractionGetter()` 재호출 → 카메라 이동/target 이동 대응.
- Space dismiss 파트는 **좌클릭도 대체 허용**. 단 레시피북 카드 활성 시 무시 (`MenuCardController.IsMenuCardActive`).
- `_spawnFrameSkipped` — 스폰 프레임의 keydown 재소비 방지.

## TutorialCookingController (mock 씬 전용)

**씬**: `Assets/Scenes/ForReal/CookingTutorial.unity` (`SceneNames.CookingTutorial`)

`MallSceneController.OpenMenuSelection`에서 튜토리얼 활성 시 정규 `Cooking` 대신 이 씬 로드.

### 특수 셋업

- `TimeManager.PauseTime()` — 아침 페이즈 자동 종료 방지.
- `customerManager.enabled = false` — 정규 손님 스폰 억제.
- `UILockManager.Lock(CookingTutorial)` — 별도 owner (Tutorial owner와 독립).
- 재료 무한 리필: 각 `BaseStorage`의 `OnFoodDestroyedForRefill` → 다음 프레임에 `CookingSceneManager.TryTutorialRefill`.
- `EarlyEndButton` 초기 비활성 (스킵 안내 파트 도달 전까지 클릭 불가).

### 카메라 강제 제어

- `forceCameraPosition` (기본 (0,0,-10)) + `forceCameraOrthoSize`로 초기 위치·줌 강제.
- 파트별 `HandlePartShown`에서 target `SpriteRenderer.bounds.center` 로 `_cameraTargetPos` 갱신.
- `LateUpdate`에서 `Vector3.SmoothDamp` (`cameraSmoothTime=0.5s`, `cameraMaxSpeed=20 u/s`).
- `worldCollider.bounds`로 카메라 target clamp (벽 밖 이동 방지).
- `freeCamera` 파트는 `_tutorialControlsCamera=false` → `HorizontalCameraMove`가 유저 조작 담당.

### 손님 mock 흐름 (message prefix 라우팅)

| message prefix | 동작 |
|---|---|
| "손님이 오면" | `TutorialSpawnOne()` — 1명 스폰, target key `customer` 부여, `OnTutorialOrderPlaced` 구독. |
| "도시락을 원하는" | `BentoModel.OnBentoPlacedForTutorial` 구독. |
| "완성한 요리" | 놓인 도시락에 target key `placed-bento` 부여, `OnFoodAddedForTutorial` 구독. |
| "영수증" | 활성 티켓에 target key `receipt` (verticalAnchor=0) 부여, Taking 손님 스폰 대기. |
| "시간 안에" | dynamic follow가 taking 손님 이동을 자동 추적. |
| "오늘은 손님이" | `EarlyEndButton.interactable = true` (스킵 활성). |

각 훅 발화 시 `TutorialController.Instance.DismissActivePart()` → 다음 파트로.

### 종료

- 손님 1명 처리 완료(`OnCustomerResolved(true)`) → "시간 안내" 파트에서 "스킵 버튼" 파트로 자동 진행. 스킵 파트는 유저가 실제로 버튼을 눌러야 함.
- `EarlyEndButton` 클릭 → `EndEarly()` → `OnGameEnd` → `HandleGameEndDuringTutorial`:
  - `MarkShown(CookingIntro)`.
  - `Inventory.ResetToDefault()` — refill로 오염된 인벤토리 초기화.
  - `SaveAll()`.
- `ReleaseTutorialLocks()` (마지막 파트 dismiss 후): `Unlock(CookingTutorial)`, `HorizontalCameraMove` 복구, `TimeManager.ResumeTime()`, `EndEarly()` 재호출.

## 진행 상태 (TutorialService)

`Assets/Scripts/Domain/Common/TutorialService.cs`:

```csharp
public bool IsCompleted;
public bool IsActive => !IsCompleted;
public bool HasShown(int stepId);   // IsCompleted면 항상 true
public void MarkShown(int stepId);  // Set 기반, 중복 방지
public void Complete();             // 이후 모든 hook 비활성
```

- 저장: `MallPersistent`나 `Cooking` 슬롯이 아닌 **별도 `TutorialState` 슬롯** (`GameSaveData.tutorial`).
- `shownSteps` (List<int>) + `completed` (bool).
- `TutorialController.CanShow(stepId) = !IsCompleted && !HasShown(stepId)`.

## RecipeBook 연동 (`RecipeBookManager.TutorialForceOpen` / `TutorialBlockOpen`)

- `TutorialForceOpen=true` 파트 동안 `Tab`/`ESC`로 레시피북 안 닫힘. 카드 ESC 닫기는 허용.
- `TutorialBlockOpen=true` 파트 동안 `Tab`으로 레시피북 여는 것 차단 (레시피북 안내 이전 파트에서 방해 방지).
- 파트 전환 시 자동 override, 스텝 종료(`ShowPart`의 idx>=Count 분기) 시 둘 다 false로 리셋.

## 저장/로드

- 각 스텝 dismiss 마지막에 `SaveManager.SaveAll()` 호출 → `gamedata.json` 즉시 반영.
- Closing 스텝 dismiss + `Complete()` 후 Settlement 씬 → 완료 상태 저장 → 다음 세션엔 튜토리얼 완전 비활성.
