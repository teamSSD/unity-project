# ADR-005: Asynchronous Runtime

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Phase 2-C로 승격 (Progressive → 전면 마이그레이션)

## TL;DR (v2 결정 요약)

**UniTask 도입 + 코루틴 전면 마이그레이션 (Phase 2-C, 두 PR로 분할).**
- 신규 `IEnumerator` / `yield return null` 사용 금지 (분석기 또는 빌드 게이트)
- 점진 마이그레이션 폐기 (두 패러다임 영구 공존 위험)
- 시점: asmdef 마이그레이션(Phase 2-A) 직후

## v1과의 차이

| 항목 | v1 (보수) | v2 (수정) |
|---|---|---|
| 마이그레이션 방식 | 점진 (터치 시 마이그레이션) | **전면** (Phase 2-C 단일 작업) |
| 잔존 코루틴 | 영구 공존 허용 | **0개 목표 (Phase 2 내 달성)** |
| 신규 코루틴 | "권장하지 않음" | **금지 (빌드 게이트)** |

## Decision

### 패키지 도입
- `com.cysharp.unitask` 패키지 추가 (Apache-2.0)
- `Packages/manifest.json` 또는 Package Manager UI

### 마이그레이션 PR 분할

**Phase 2-C-1: 핫패스 마이그레이션 (1 PR)**
- 씬 전환: `SceneLoader`, `LoadingManager`, `BootLoader`
- 미니게임 진행: `FireMiniGame`, `SliceMiniGame`, `MixMiniGame`, `GriddleMinigame`
- 페이드/연출 코루틴 (LoadingManager 내부)
- 추정: 1-2일

**Phase 2-C-2: 잔여 마이그레이션 (1 PR)**
- 나머지 모든 IEnumerator 사용처
- `yield return null` 0건 달성
- 추정: 1-2일

### 패턴 표준화

**현재**:
```csharp
private IEnumerator Start() {
    var op = SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);
    while (!op.isDone)
        yield return null;
    // ...
}
```

**변환**:
```csharp
private async UniTaskVoid Start() {
    await SceneManager.LoadSceneAsync(SceneNames.Managers, LoadSceneMode.Additive);
    // ...
}
```

**취소 토큰**:
```csharp
public async UniTask LoadSceneSafelyAsync(string name, CancellationToken ct) {
    try {
        await SceneManager.LoadSceneAsync(name).ToUniTask(cancellationToken: ct);
    } catch (OperationCanceledException) {
        Debug.Log("[SceneCoordinator] Load cancelled");
    }
}
```

- MonoBehaviour: `destroyCancellationToken` (Unity 2022+) 활용
- 씬 전환, 네트워크 등 외부 API 호출은 CancellationToken 받기 권장

### 신규 코드 강제 (빌드 게이트)

선택지:
- A. **Roslyn Analyzer 추가** — `IEnumerator` 사용 감지 시 컴파일 에러
- B. **하네스 카운트 + CI 게이트** — `yield_return_null` 카운트 0 유지
- C. **Convention by review** — 코드 리뷰 강제

**채택**: B (하네스 카운트). A는 false positive 위험(Unity StartCoroutine API 자체 사용 등), C는 안 지켜질 위험.

하네스에 게이트 추가:
```bash
# CI 또는 pre-push hook에서
yield_null=$(kv_read antipatterns.tsv yield_return_null)
if [ "$yield_null" -gt 0 ]; then
    echo "ERROR: yield return null 잔존 ($yield_null건). Phase 2-C 완료 후 0 유지."
    exit 1
fi
```

## Consequences

### 긍정
- 비동기 코드 명료성 큰 향상 (try/catch, 반환값, CancellationToken)
- 테스트 가능 (PlayMode 외 EditMode에서도 일부 가능)
- DEV 원칙 회복 (`yield return null` 금지가 의미 가짐)
- 두 패러다임 공존 종료 → 인지 부담 제거
- 하네스 게이트로 회귀 방지

### 부정
- 외부 패키지 의존 (UniTask)
- 학습 곡선 (async/UniTaskVoid/CancellationToken)
- Phase 2-C 2개 PR (3-4일 작업)
- 마이그레이션 중 회귀 위험 (씬 전환, 미니게임)

## Resolved Questions

1. ~~CancellationToken을 어디까지 전파~~ → **씬 전환 + 외부 API + 미니게임 진행** (점진 확대)
2. ~~UniTask vs Coroutine 공존 기간~~ → **공존 없음. Phase 2-C 완료 시 0**
3. ~~UniRx 도입 여부~~ → **본 ADR 범위 외**. 필요 시 별도 ADR
4. ~~PlayMode 테스트 통합~~ → ADR-004의 Tests.PlayMode.asmdef에서 UniTask 사용

## Decision Required

- [x] UniTask 패키지 도입 동의
- [x] 전면 마이그레이션 (점진 폐기) 동의
- [x] Phase 2-C 두 PR 분할 동의 (핫패스 + 잔여)
- [x] 하네스 게이트 (`yield_return_null=0`) 동의
- [x] 시점: asmdef 마이그레이션 직후
