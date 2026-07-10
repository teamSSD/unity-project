# 입력 & 컨트롤

Aftertaste는 **마우스 + 키보드** 조합으로 조작한다. Legacy Input Manager 사용 (`activeInputHandler: 0`).

## 전역 키맵 (모든 씬 공통)

| 키 | 동작 | 관련 코드 |
|---|---|---|
| `Tab` | 레시피북 열기/닫기 (도시락 메뉴, 재료, 레시피 확인) | [`RecipeBookManager.Update`](../../../Assets/Scripts/Unity/Common/RecipeBookManager.cs) |
| `Escape` | 모달 닫기 · 종료 프롬프트 (씬별 컨텍스트 의존) | 씬별 처리 |
| 마우스 좌클릭 | 기본 인터랙션 (버튼 · 아이템 · NPC · 도구 · 재료 등) | 각 씬 |
| 마우스 이동 | 화면 좌우 끝 hover 시 카메라 이동 (씬별 지원 여부 상이) | [`HorizontalCameraMove`](../../../Assets/Scripts/Unity/Common/HorizontalCameraMove.cs) |

## 카메라 이동

파일: [`Assets/Scripts/Unity/Common/HorizontalCameraMove.cs`](../../../Assets/Scripts/Unity/Common/HorizontalCameraMove.cs)

Mall, Shop, Garden 등 사이드뷰 씬에서 카메라 좌우 이동.

| 입력 | 방향 |
|---|---|
| `A` 또는 `←` | 카메라 왼쪽 이동 |
| `D` 또는 `→` | 카메라 오른쪽 이동 |
| 마우스 좌/우 끝 hover | 방향에 맞춰 이동 |

**주의**: `W` / `S`는 **미사용**. 상하 카메라 이동 없음 (2D 사이드뷰).

`holdKey` (기본 `KeyCode.None`)가 설정되면 그 키를 누르는 동안만 이동 허용 (튜토리얼 등 제약용).

## 미니게임 입력 (Cooking 씬 전용)

각 조리도구 미니게임마다 다른 입력:

| 미니게임 | 키 | 코드 |
|---|---|---|
| **Fire (팬 T001 / 냄비 T002)** | `Space` (누르면 화력 상승, 떼면 하강) | [`FireMiniGame.cs`](../../../Assets/Scripts/Unity/Cooking/FireMiniGame.cs) |
| **Sauce (볼 T003 - 소스뿌리기)** | `↑` / `↓` 연타 (순서 맞추기) | [`SauceMiniGame.cs`](../../../Assets/Scripts/Unity/Cooking/SauceMiniGame.cs) |
| **Mix (볼 T003 - 섞기)** | `Space` 연타 | [`MixMiniGame.cs`](../../../Assets/Scripts/Unity/Cooking/MixMiniGame.cs) |
| **Griddle (도마 T004 - 자르기)** | `↑` `↓` `←` `→` (안내선 따라) | [`GriddleMinigame.cs`](../../../Assets/Scripts/Unity/Cooking/GriddleMinigame.cs) |
| **Click (철판 T005)** | `Space` 타이밍 클릭 | [`ClickMiniGame.cs`](../../../Assets/Scripts/Unity/Cooking/ClickMiniGame.cs) |

미니게임 세부는 [`systems/cooking-system.md`](../systems/cooking-system.md) 참조.

## 튜토리얼 말풍선

파일: [`Assets/Scripts/Unity/Tutorial/TutorialBubble.cs`](../../../Assets/Scripts/Unity/Tutorial/TutorialBubble.cs)

- 튜토리얼 파트별 `dismissKey` 세팅 (예: `Space`, `Tab`, `Escape`, `None`)
- `Space` 파트는 좌클릭도 대체 허용 (가이드성 안내)
- 레시피북 카드 열린 상태에선 Space/클릭 dismiss 차단
- `KeyCode.None`이면 스크립트가 다른 이벤트로 dismiss (예: 손님 서빙 완료)

## 씬별 인터랙션 요약

### GameStart 씬
- 마우스 클릭: New Game / Continue 버튼
- Continue 클릭 → 저장 로드 → Mall 진입

### Mall 씬 (준비/오후/저녁/밤)
- 마우스 클릭:
  - 상점 오브젝트 (Shop 진입)
  - Garden 경로 (Farm 씬)
  - NPC (대화 시작)
  - GoHomeButton (페이즈 마감)
- `Tab`: 레시피북
- `Escape`: 오픈된 모달/대화 취소
- Preparation 페이즈: BentoSelection UI가 자동 오버레이

### Shop 씬
- 마우스 클릭: 카테고리 탭, 아이템 선택, 구매 버튼
- `Escape`: Shop 종료 (Mall 복귀)

### Cooking 씬
- 마우스 좌클릭 & 드래그: 재료 드래그해서 조리도구에 배치
- 마우스 좌클릭: 조리도구 클릭 → 미니게임 시작
- 마우스 좌클릭: 완성 요리를 도시락에 배치
- 마우스 좌클릭: 도시락 완성 후 영수증 붙이기 → 손님 서빙
- 마우스 좌클릭: 손님 클릭 → 주문 받기
- `Space`, 화살표: 미니게임 조작
- `Tab`: 레시피북 (Cooking 중에도 열림)
- `Escape`: 열린 UI 닫기
- **EarlyEndButton (종료하기)**: 페이즈 조기 종료 버튼

### Cooking Tutorial 씬 (mock)
- Cooking과 거의 동일. `TutorialSpawnOne`으로 특정 시점에 손님 강제 스폰. 자동 스폰은 `AutoSpawnEnabled = false`로 비활성.

### Garden 씬
- 마우스 클릭: FarmTile 선택, 심기, 수확
- `Escape`: Mall 복귀

### Settlement 씬
- **아무 키 입력** (`Input.anyKeyDown`) → Mall 복귀 (Day+1 Preparation)
  - 파일: [`SettlementController.Update`](../../../Assets/Scripts/Unity/Mall/SettlementController.cs)

## 입력 잠금 (UILockManager)

파일: [`Assets/Scripts/Domain/Common/UILockManager.cs`](../../../Assets/Scripts/Domain/Common/UILockManager.cs)

특정 상황에서 다른 UI 오픈 차단. `Owner` enum으로 관리:

| Owner | 활성 시나리오 |
|---|---|
| `Minigame` | 요리 미니게임 진행 중 |
| `Dialogue` | 대화창 활성 |
| `RecipeBook` | 레시피북 열림 |
| `BentoSelection` | 도시락 선택 UI |
| `GameStart` | 시작 화면 |
| `Loading` | 씬 페이드 중 |
| `Shop` | 상점 UI |
| `Settings` | 설정 UI |
| `PhaseSelection` | 3택 액션 선택 UI |
| `Tutorial` | 튜토리얼 활성 |
| `CookingTutorial` | Cooking 튜토리얼 특수 락 |

`CanOpen(requester)` — 자기 이외 잠금이 없으면 true.  씬 전환 시 `Reset()`으로 초기화 ([`RuntimeInitializeOnLoadMethod`]).

## 접근성 & UX 노트

- **키보드 nav 비활성**: Mall에서 `EventSystem.sendNavigationEvents = false` — 카메라 이동키(A/D)가 UI 버튼을 잘못 selected 시키지 않도록.
- **Space 클릭 대체**: 튜토리얼 Space dismiss 파트는 좌클릭도 허용 (마우스 유저 편의).
- **Escape 컨벤션**: 모달 우선 취소 → 아무 모달 없으면 씬별 처리 (Cooking에선 무시, Mall에선 프롬프트 등).

## 미지원 입력

- `W` / `S` (상하 이동): 2D 사이드뷰라 상하 카메라 이동 없음
- 게임패드: 미지원
- 모바일 터치: 미지원 (WebGL 데스크톱 타겟)
- 우클릭: 미사용
- 마우스 휠: 미사용 (줌 없음)

## 참조

- 미니게임 파라미터: [`systems/cooking-system.md`](../systems/cooking-system.md)
- 튜토리얼 dismissKey 세팅: [`systems/tutorial-system.md`](../systems/tutorial-system.md)
- Mall 액션 선택 UI: [`systems/mall-system.md`](../systems/mall-system.md)
- 설정 UI: [`reference/settings-and-audio.md`](../reference/settings-and-audio.md)
