# Mall 시스템

## 개요

**Mall (상가) 씬**은 Aftertaste의 세계 허브. 도시락 가게 앞부터 층 전체를 아우르는 상가 복도로 구성되며, 페이즈별로 다른 UI가 자동 표시되어 유저가 다음 행동을 선택하게 한다. Cooking / Shop / Garden 씬 사이의 이동은 모두 이 씬을 경유한다.

씬 파일: `Assets/Scenes/ForReal/Mall.unity`
핵심 컨트롤러: [`MallSceneController.cs`](../../../Assets/Scripts/Unity/Common/MallSceneController.cs)

## 씬 구조

Mall 씬은 **상가 파노라마 배경 + 층 구조 + NPC 배치 + 상호작용 지점**으로 구성된다.

### 층 / 이동
- 상하 이동은 `StairsInteraction` (파일: [`Assets/Scripts/Unity/Mall/StairsInteraction.cs`](../../../Assets/Scripts/Unity/Mall/StairsInteraction.cs))로 처리
- 계단 진입 → `targetStair` 위치로 코루틴 이동 (2-phase: 현재 계단 → 목표 계단)
- 이동 중 `UILockManager.Lock(Loading)` — 플레이어 입력 잠금
- 계단 오르내림은 씬 전환 아님. Mall 씬 하나에 모든 층이 존재
- SpriteRenderer flipX로 좌우 방향 자동 회전

### 카메라
- `CameraFollow` ([`Assets/Scripts/Unity/Common/CameraFollow.cs`](../../../Assets/Scripts/Unity/Common/CameraFollow.cs))
- `boundsSource` (배경 SpriteRenderer)로 카메라 이동 범위 클램프 → 상가 전체를 좌우로 스크롤하는 **파노라마 카메라**
- Mall 진입 시 `SnapToPlayer()` 호출 — 첫 프레임 슬라이드 방지
- `SmoothDamp` 기반 부드러운 추적 (기본 `smoothTime = 0.15f`)

### 상호작용 지점 (Mall.unity의 주요 요소)

| 오브젝트 | 스크립트 | 기능 |
|---|---|---|
| `playerStore` (가게 앞) | `GoHomeInteraction` | Space → `MallSceneController.GoHome()` |
| `ExitToFarm` | `FarmPathInteraction` | Space → Garden 씬 로드 (return 위치 저장) |
| 상점 카운터들 | `UnifiedShopInteraction` | Space → `ShopUIAdapter.OpenShop(tab)` (씬 전환 없음, 오버레이 UI) |
| 계단들 | `StairsInteraction` | Space → 짝 계단으로 코루틴 이동 |
| Delivery NPC 8명 | `DeliveryNpcView` + `DeliveryNpcInteraction` | Space → 도시락 주문 or 일상 대화 |

## 페이즈별 UI 흐름

`MallSceneController.Start()`가 진입 조건을 분기 결정한다.

### Preparation 페이즈 (05:00 진입)

- `session.MenuSelection.ClearAllMenus()` — 어제 선택 리셋
- **UI 자동 표시 없음** — 유저가 자유롭게 상가를 돌아다닐 수 있음
- 튜토리얼 활성 상태면 `TutorialStepId.WelcomeAtSpawn` 표시
- `playerStore` 근처에서 Space → 메뉴 선택 모달 (`BentoSelectionController.Show`) → 확정 시:
  1. `Progress.PassPhase()` → Morning 진입
  2. `SceneLoader.LoadScene(Cooking)` (튜토리얼이면 `CookingTutorial`)

### Morning 페이즈

- Morning은 자동으로 Cooking 씬으로 전환되므로 Mall에 도달할 일이 없음
- 방어적으로 Start의 UI 자동 표시 로직에서 skip 처리

### Afternoon / Evening / Night 페이즈

- **`PhaseActionSelector` 자동 표시 조건** (`MallSceneController.Start`):
  ```
  currentPhase != Preparation && currentPhase != Morning && !cameFromSubScene
  ```
  `cameFromSubScene = SceneLoader.CurrentScene == Shop || Garden`
- 즉, **Cooking → Mall 복귀 시엔 자동 표시**, **Shop/Garden → Mall 복귀 시엔 자동 표시 안 함**
- 이 규칙은 최근 fix된 것으로, 유저가 Shopping 후 돌아와도 액션창이 다시 뜨지 않도록 함
- Mall 체류 중 페이즈 전환 시(`OnPhaseChangedInMall`)도 같은 규칙 적용

### PhaseActionSelector 3택

파일: [`Assets/Scripts/Unity/UI/PhaseActionSelector.cs`](../../../Assets/Scripts/Unity/UI/PhaseActionSelector.cs)

| 액션 | 이름 | 설명 | 실행 |
|---|---|---|---|
| Work | 영업 | 가게를 엽니다. | `SceneLoader.LoadScene(Cooking)` |
| Rest | 휴식 | 스태미너를 충전합니다. | `Stats.SetStamina(100)` + `Progress.PassPhase()`. Night면 즉시 Settlement 로드 (Blackout 우회) |
| Shopping | 상가 이동 | 상가로 이동합니다. | UI만 닫음 (Mall 씬 내 이동은 자유롭게 걸어감) |

- Rest는 Blackout 페이드를 감싸서 페이즈 전환 (Night는 예외 — LoadingManager 재귀 락 회피)
- 유저가 새 페이즈에 재도달하면 다시 표시 (`OnPhaseChangedInMall`)
- UI 표시 시 `UILockManager.Lock(PhaseSelection)`, Esc로 Hide

## GoHome 버튼 동작

`playerStore` 트리거에서 Space 입력하면 `GoHomeInteraction` → `MallSceneController.GoHome()`:

```
if (phase == Afternoon && Tutorial.CanShow(Closing))
    → Closing 튜토리얼 → Settlement 로드 (0일차 정산)
else if (phase == Preparation)
    → OpenMenuSelection() (BentoSelection 모달)
else
    → ConfirmPassPhase() — 확인 모달, PassPhase 시 Cooking or Settlement 자동 로드
```

- Night 확인 모달: "하루를 마치시겠습니까? / 잠들면 다음 날이 시작됩니다."
- 그 외: "이 페이즈를 마치시겠습니까? / 다음 페이즈로 넘어갑니다."
- `PhaseActionSelector.Work` 선택 후 Cooking에서 Mall 복귀 시 다시 이 자동 표시 로직으로 액션창 재출현

## NPC 배치 / 상호작용

Mall에는 두 종류 NPC가 배치된다.

### Delivery NPC (배달 주문 대상, 그룹)
- 정의: `Assets/Bundles/driveAssets/dataTables/deliveryNPC.csv`
- 데이터: `DeliveryNpcData` ScriptableObject (프리팹의 `DeliveryNpcView.npcData` 필드)
- 8명 등록: 자르, 게토로, 스라냐, 파자마, 모아이, 니모, 레데, 가브리엘

| npcId | 이름 | groupId | 도시락 |
|---|---|---|---|
| npc_jar | 자르 | power_room_pair | 전력실 도시락 (I044+I058) |
| npc_getoro | 게토로 | power_room_pair | 위와 공유 |
| npc_sranya | 스라냐 | sranya_solo | — (일상 대화만) |
| npc_pajama | 파자마 | pajama_solo | — |
| npc_moai | 모아이 | moai_solo | — |
| npc_nimo | 니모 | nimo_solo | 새벽국 도시락 (I034) |
| npc_lede | 레데 | lede_solo | 삼각밥 도시락 (I053) |
| npc_gabriel | 가브리엘 | (빈값) | 일상 대화만 |

- `DeliveryNpcView.Init` (Awake) → `groupId` 있으면 `DeliveryNpcDialogueInteraction`, 없으면 `CasualNpcInteraction`
- 상태 3종 (`DeliveryNpcState`):
  - `Orderable` (white) — 대화/주문 가능
  - `WaitingReceipt` (yellow) — 완성된 도시락 대기 중
  - `Completed` (gray) — 완료
- 자세한 배달 퀘스트 흐름은 [`delivery-quest-system.md`](delivery-quest-system.md) 참조

### 일상 대화 (CasualNpcInteraction + CasualDialogueProvider)

- 정의: `Assets/Bundles/driveAssets/dataTables/npcCasualDialogue.csv`
- 조건: `Any` / `Good` / `Bad` (날씨 조건, [`weather-system.md`](weather-system.md) 참조)
- 랜덤 대사 하나 선택 → 런타임에 `DialogueSO` 생성 → `DialogueManager.StartDialogue`
- `groupId` 없는 NPC(가브리엘)나 배달 완료 후 상태에서 사용

## 씬 진입/이탈 흐름

### 진입 시 (`MallSceneController.Start`)
1. EventSystem nav 이벤트 차단 (Space가 마지막 selected 버튼을 트리거하지 않도록)
2. `SceneLoader.MallReturnPosition`이 있으면 플레이어 위치 override → 카메라 스냅
3. `BentoSelection` prefab 인스턴스화 → close 상태로 대기
4. `PhaseActionSelector` prefab 인스턴스화 → Hide 상태로 대기
5. `Progress.OnPhaseChanged` 구독 (Mall 체류 중 페이즈 전환 대응)
6. 페이즈 조건에 따라 `OpenActionSelection()` 자동 호출 여부 결정
7. 튜토리얼 훅 (`WelcomeAtSpawn`, Afternoon이면 `PhaseSelectAfternoon`)

### 이탈 시
- 씬 전환은 Cooking / Shop / Garden / Settlement 4곳
- 이탈 전 `SceneLoader.SetMallReturnPosition(pos)`로 복귀 위치 저장
- 예외: `PhaseActionSelector.Rest` (Night)는 씬 전환 없이 `PassPhase()` → 내부에서 Night → Settlement 로드

## 관련 문서

- 페이즈 정의/시간표: [`../overview/core-loop.md`](../overview/core-loop.md)
- Shop 진입 이후: [`shop-system.md`](shop-system.md)
- Garden 진입 이후: [`garden-system.md`](garden-system.md)
- 날씨 조건 대사: [`weather-system.md`](weather-system.md)
- 정산 씬: [`settlement-system.md`](settlement-system.md)
