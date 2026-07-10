# CookingTutorial.unity — 조리 튜토리얼 mock 씬

> 파일: `Assets/Scenes/ForReal/CookingTutorial.unity`
> 진입: Mall Preparation에서 `Tutorial.IsActive`일 때 BentoSelection 확정 → `SceneLoader.LoadScene(CookingTutorial)`.

## 역할 한 줄

**Cooking.unity를 복사한 mock 씬**. 실제 요리 시스템을 격리해 손님 1명 흐름만 유도하고, 튜토리얼 안내와 함께 검증. 튜토리얼 종료 후 즉시 Mall 복귀.

## 왜 별도 씬인가

- 실 씬은 손님 자동 스폰, 시간 흐름, 자원 소모 등이 얽혀 있어 튜토리얼 안내 도중 예상외 상태 전환 위험
- 시간 정지, 손님 자동 스폰 억제, 재료 무한 refill 등 mock 조건을 안전하게 걸기 위해 **격리된 씬 인스턴스** 사용
- 튜토리얼 종료 시 인벤토리 원상복귀 → Day 1 정상 시작 보장

## GameObject 계층

Cooking.unity와 **동일한 구조**:
```
CookingTutorial.unity (root)
├── EventSystem
├── Canvas / TopUICanvas / WorldCanvas
├── Main Camera
├── SubSceneController                  (returnScene=Mall)
├── GameManager                         CookingSceneManager + CustomerManager + MiniGameManager
├── 배경 / bg_cuisine_morning/evening/night
├── 요리 도구 (인덕/팬/냄비/볼/도마/철판)
├── 냉장고 및 캐비넷 / 냉장고 / 냉장고 내부 / 캐비넷(상) / 캐비넷(하)
├── 도시락 관련 / 도시락 위치 1/2/3 / 도시락묶음
├── 쓰레기통
├── Canvas → EarlyEndButton
└── TutorialCookingController           ← 추가 컴포넌트 (mock orchestrator)
```

## 추가된 스크립트

| 클래스 | 위치 | 부착 대상 |
|---|---|---|
| `TutorialCookingController` | `Unity/Tutorial/TutorialCookingController.cs` | 자체 GO |
| `TutorialTarget`            | `Unity/Common/TutorialTarget.cs` | 각 앵커 (조리도구/도시락 슬롯 등) |

## TutorialCookingController — mock 오케스트레이터

[`TutorialCookingController.cs`](../../../Assets/Scripts/Unity/Tutorial/TutorialCookingController.cs)

### 인스펙터 필드
```csharp
[SerializeField] CustomerManager customerManager;
[SerializeField] CookingSceneManager cookingSceneManager;
[SerializeField] BaseStorage[] storages;                   // 3종 저장소 refill용

// Camera Force (튜토리얼 진입 시 강제 위치)
[SerializeField] Vector3 forceCameraPosition = (0, 0, -10);
[SerializeField] float forceCameraOrthoSize = -1;         // <=0이면 무시

// Camera Lerp (튜토리얼 target으로 SmoothDamp 이동)
[SerializeField] float cameraSmoothTime = 0.5f;
[SerializeField] float cameraMaxSpeed = 20f;
[SerializeField] Collider2D worldCollider;                // 카메라 클램프 경계
```

### Start() — mock 조건 걸기
1. **재료 무한 refill**: 각 `BaseStorage.OnFoodDestroyedForRefill += HandleFoodDestroyed` → 다음 프레임 `CookingSceneManager.TryTutorialRefill(storage, food)`
2. **시간 정지**: `TimeManager.Instance.PauseTime()` — 아침 페이즈 자동 종료 방지
3. **손님 자동 스폰 억제**: `customerManager.enabled = false`
4. **상호작용 차단**: `UILockManager.Lock(Owner.CookingTutorial)` — TutorialBubble의 per-bubble Lock/Unlock과 별개 owner
5. **EarlyEndButton 락**: "오늘은 손님이…" 파트 도달 전까지 클릭 불가

### 카메라 제어 (LateUpdate SmoothDamp)
- 파트마다 `TutorialTarget` 위치를 `_cameraTargetPos`로 설정 → SmoothDamp
- `worldCollider.bounds`로 클램프 (벽 밖 이동 방지)
- `part.freeCamera=true` 파트: 튜토리얼 카메라 off, `HorizontalCameraMove` 활성 (유저 조작)

### HandlePartShown — 파트별 mock 흐름 라우팅

메시지 prefix로 판정 (mock 씬 전용):

| 메시지 prefix | 동작 |
|---|---|
| "손님이 오면" | `TutorialSpawnOne()` → ordering 손님 스폰, target "customer" 등록 |
| "도시락을 원하는" | `BentoModel.OnBentoPlacedForTutorial` 구독 → 도시락 배치 시 다음 파트 |
| "완성한 요리" | 방금 놓은 도시락에 target "placed-bento" → `BentoModel.OnFoodAddedForTutorial` 구독 |
| "영수증" | ticket에 target "receipt" 등록. Taking 손님 스폰까지 대기 |
| "시간 안에" | Taking 손님 target 등록 (dynamic follow) |
| "오늘은 손님이" | EarlyEndButton 활성화 (스킵 안내 파트) |

### mock 흐름 유도 훅
- `_activeLifecycle.OnTutorialOrderPlaced` — 주문 완료 시. Waiting timer 정지(`StopTimer()` — 손님이 안 나감).
- `BentoModel.OnBentoPlacedForTutorial` — 도시락 배치.
- `BentoModel.OnFoodAddedForTutorial` — 음식 추가.
- `_activeLifecycle.OnTutorialTakingSpawned` — Taking 손님 스폰.

### ReleaseTutorialLocks() — 튜토리얼 완료 시
`CookingSceneManager.TryStartCookingTutorial()`의 `onDone` 콜백에서 호출:
1. `UILockManager.Unlock(CookingTutorial)`
2. `HorizontalCameraMove.enabled = true` (카메라 조작 복구)
3. `TimeManager.ResumeTime()`
4. `CustomerManager.enabled = true` + `EndEarly()` → 즉시 다음 페이즈 (mock 씬 벗어남)

### HandleGameEndDuringTutorial()
Skip 버튼(EarlyEndButton)으로 조기 종료 시:
1. `Tutorial.MarkShown(CookingIntro)`
2. `Inventory.ResetToDefault()` — refill 흔적 지움
3. `SaveManager.SaveAll()`

## 튜토리얼 스텝: CookingIntro

`TutorialStepCatalog`에 여러 파트로 정의. TutorialCookingController가 각 파트 message prefix를 감지해 mock 흐름을 진행. 마지막 파트 dismiss → `onDone` → `ReleaseTutorialLocks()`.

## 관련 시스템

- [Cooking 씬](./scene-cooking.md) — 실 씬 (튜토리얼 미활성 시 사용)
- [Mall 씬](./scene-mall.md) — 진입 트리거 + 복귀
- `TutorialController` (Managers 씬 singleton)
- `TutorialService.IsActive` / `MarkShown(stepId)` — 진행 상태
- Owner: `UILockManager.Owner.CookingTutorial` (Cooking과 별개)
