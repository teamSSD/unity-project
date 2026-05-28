# Phase 2-C: Coroutine → UniTask 전면

## 목표

모든 코루틴을 UniTask 기반 async/await로 마이그레이션. 5개 게이트를 0으로:
- `yield_return_null = 0`
- `yield_statements = 0`
- `ienumerator_methods = 0`
- `start_coroutine_calls = 0`
- (UniTask 채택 카운트 증가)

## 가설

코루틴은 MonoBehaviour 라이프사이클 결박 + 취소 불가 + 테스트 불가 + 예외 처리 약함. UniTask로 마이그레이션 시:
- async/await 표현으로 흐름 명료
- CancellationToken으로 안전한 중단
- try/catch 자연
- 반환값 가능
- PlayMode 외 일부 EditMode에서도 테스트 가능
- DEV 원칙 (yield return null 금지) 회복

## 측정 기준 (게이트)

| 지표 | 베이스라인 | 목표 |
|---|---:|---:|
| `yield_return_null` | 21 | **0** |
| `yield_statements` | 32 | **0** |
| `ienumerator_methods` | 21 | **0** |
| `start_coroutine_calls` | 22 | **0** |
| `unitask_methods` | 0 | 25+ |
| `await_statements` | 0 | 50+ |
| `unitask_using_files` | 0 | 25+ |

`bash gate.sh` 출력에서 위 5개가 모두 OK 상태.

## 검증 방법

1. **컴파일 통과** (UniTask 패키지 설치 후)
2. **핫패스 회귀** (필수, 매 PR 후):
   - **Boot 시퀀스**: Boot → Managers → GameStart → New Game → Mall (시간 ±10%)
   - **씬 전환**: Mall → Cooking → Mall → Garden → Mall → Shop → Mall (각 페이드 정상)
   - **미니게임** 5종 각 1회 플레이 (Fire/Slice/Mix/Griddle/...)
3. **잔여 코루틴 0**: `bash gate.sh yield_return_null`, `gate.sh ienumerator_methods`, `gate.sh start_coroutine_calls`
4. **메모리/CPU 회귀 없음**: Profiler로 씬 전환 + 미니게임 1회씩 측정, 베이스라인 대비 ±20%

## 회귀 방지 체크리스트

- [ ] UniTask 패키지 정상 설치 + 컴파일 통과
- [ ] StartCoroutine 호출자도 변환 (호출자가 await 또는 .Forget())
- [ ] CancellationToken 누락 없음 (씬 전환, 외부 API, 미니게임 진행)
- [ ] `async UniTaskVoid`는 fire-and-forget 의도일 때만 (예외 무시 위험)
- [ ] `async void` 절대 금지 (UniTask 권장 사항)
- [ ] 마이그레이션 중 신규 코루틴 추가 금지
- [ ] 각 PR 후 수동 회귀 (씬 전환 + 미니게임 ≥1회)

## 추정 시간

| 단계 | 시간 |
|---|---|
| UniTask 패키지 설치 | 0.5h |
| 2-C-1 핫패스 마이그레이션 | 1-2일 |
| 핫패스 회귀 검증 | 0.5일 |
| 2-C-2 잔여 마이그레이션 | 1-2일 |
| 전체 회귀 검증 | 0.5일 |
| **합계** | **3-4일 (2 PR)** |

## 선결 조건

- [x] Phase 2-A 완료 (asmdef 안정)
- [x] ADR-005 ACCEPTED
- [ ] UniTask 패키지 도입 최종 확인 (com.cysharp.unitask, Apache-2.0)

## PR 분할

### 2-C-1: 핫패스 마이그레이션 (1 PR)
- 씬 전환: `SceneLoader`, `LoadingManager`, `BootLoader`
- 로딩 페이드/연출 코루틴 (LoadingManager 내부)
- 미니게임 진행 코루틴 (Fire/Slice/Mix/Griddle 등)

### 2-C-2: 잔여 마이그레이션 (1 PR)
- 나머지 IEnumerator 사용처 전부
- `yield return null` 0건 달성
- 게이트 GREEN 확인

## 세부 작업

### 1. UniTask 패키지 설치 (0.5h)

`Packages/manifest.json` 수정:
```json
{
  "dependencies": {
    "com.cysharp.unitask": "2.5.0",
    ...
  }
}
```

또는 Package Manager UI → Add package by name → `com.cysharp.unitask`.

설치 후:
```bash
bash tmp/refactor-2026-05/harness/run_all.sh
# unitask_using_files 가 0 → 0 (사용 시작 전)
# 컴파일 통과 확인
```

### 2. 핫패스 매핑

| 현재 | 변환 후 | 비고 |
|---|---|---|
| `BootLoader.cs:10 IEnumerator Start()` | `async UniTaskVoid Start()` | `yield return null` 4회 제거 |
| `SceneLoader.cs LoadSceneDirectCoroutine` | `async UniTask LoadSceneDirectAsync(CancellationToken ct)` | static 메서드 |
| `LoadingManager.cs LoadSceneAdditiveCoroutine` (61라인) | `async UniTask LoadSceneAdditiveAsync(...)` | 최장 메서드 |
| `LoadingManager.cs` 페이드 코루틴들 | `async UniTask FadeInAsync(CancellationToken)` | |
| `FireMiniGame.cs OnUpdate` (51라인, Update 기반) | (분석 필요 — Update 그대로 vs async loop) | |
| `SliceMiniGame.cs` 코루틴 | `async UniTask` | |
| `MixMiniGame.cs` 코루틴 | `async UniTask` | |
| `GriddleMinigame.cs` 코루틴 | `async UniTask` | |

### 3. 변환 패턴 표준화

**Before (코루틴)**:
```csharp
private IEnumerator Start() {
    var op = SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);
    while (!op.isDone)
        yield return null;
    ManagerBootstrap.EnsureAll();
}
```

**After (UniTask)**:
```csharp
private async UniTaskVoid Start() {
    await SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);
    ManagerBootstrap.EnsureAll();
}
```

**호출자 변환**:
```csharp
// Before
StartCoroutine(Foo());

// After (fire-and-forget)
FooAsync().Forget();

// After (대기)
await FooAsync();

// After (취소 가능)
FooAsync(destroyCancellationToken).Forget();
```

**CancellationToken 패턴**:
```csharp
public async UniTask LoadSceneAsync(string name, CancellationToken ct = default) {
    try {
        await SceneManager.LoadSceneAsync(name).ToUniTask(cancellationToken: ct);
    } catch (OperationCanceledException) {
        Debug.Log("[SceneCoordinator] Load cancelled");
    }
}

// MonoBehaviour 호출자
public class SceneCoordinator : MonoBehaviour {
    public void LoadScene(string name) {
        LoadSceneAsync(name, destroyCancellationToken).Forget();
    }
}
```

**페이드 코루틴 → UniTask**:
```csharp
// Before
private IEnumerator FadeOut(float duration) {
    float t = 0;
    while (t < duration) {
        t += Time.deltaTime;
        canvasGroup.alpha = 1f - (t / duration);
        yield return null;
    }
    canvasGroup.alpha = 0;
}

// After
private async UniTask FadeOutAsync(float duration, CancellationToken ct) {
    float t = 0;
    while (t < duration) {
        t += Time.deltaTime;
        canvasGroup.alpha = 1f - (t / duration);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }
    canvasGroup.alpha = 0;
}
```

### 4. FireMiniGame.cs (OnUpdate 51라인) 분석

OnUpdate가 Update 기반 (코루틴 아님). UniTask 마이그레이션 대상 아님. 단, 내부에 코루틴/yield 패턴이 있는지 확인 필요. 만약 있다면 async loop로 변환:

```csharp
// Update 기반 (그대로 유지 가능)
private void Update() {
    // 게임 로직
}

// 또는 async loop로 통일하려면:
public async UniTask RunAsync(CancellationToken ct) {
    while (!ct.IsCancellationRequested && !IsGameOver) {
        Tick();
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }
}
```

### 5. 잔여 마이그레이션 (2-C-2)

`bash gate.sh ienumerator_methods` 가 FAIL 일 때까지 반복:
1. `xargs grep -l "IEnumerator" ...` 로 잔존 파일 식별
2. 각 메서드 변환
3. 게이트 재확인

### 6. 빌드 게이트 (가드)

pre-push hook 또는 CI 추가:
```bash
#!/bin/sh
bash tmp/refactor-2026-05/harness/run_all.sh > /dev/null 2>&1
bash tmp/refactor-2026-05/harness/gate.sh yield_return_null || exit 1
bash tmp/refactor-2026-05/harness/gate.sh ienumerator_methods || exit 1
bash tmp/refactor-2026-05/harness/gate.sh start_coroutine_calls || exit 1
```

이 게이트는 Phase 2-C 완료 후 활성화 (그 전엔 항상 FAIL이므로).

### 7. 위험 신호 (작업 중단/롤백 트리거)

- 씬 전환 시 화면 검은 화면 멈춤: CancellationToken 누락 또는 Forget() 누락
- 미니게임 진행 안 됨: async UniTask 반환 메서드를 await 안 함
- 컴파일 에러 다수: UniTask using 누락 또는 패키지 버전 불일치
