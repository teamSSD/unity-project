# ADR-007: Asset Loading — Resources → Addressables 전면 마이그레이션

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Phase 2-D로 승격 (Progressive → 전면 수행)

## TL;DR (v2 결정 요약)

**Resources.Load 36회 → Addressables 전면 마이그레이션 (Phase 2-D).**
- `Assets/Resources/` 폴더 비움 (잔존 = 명백한 미사용)
- `resources_load = 0` 하드 타깃
- 시점: asmdef 마이그레이션(Phase 2-A) 직후
- 하네스 추가: `09_addressables_audit.sh` (그룹 등록 키 vs 코드 참조)

## v1과의 차이

| 항목 | v1 (보수) | v2 (수정) |
|---|---|---|
| 추천 | D (보류) | **C (전면 마이그레이션)** |
| 트리거 조건 | 빌드 사이즈/메모리 압박 시 | **즉시 수행** |
| Resources/ 폴더 | 유지 | **비움 (잔존 = 미사용 신호)** |
| 시점 | 미정 | Phase 2-D |

## v2 결정 근거

사용자 지적: "Resources.Load 없으면 고아 리소스가 자연히 정리됨"

정확한 메커니즘:
1. Resources.Load 호출 전부 제거 → Addressables로 이동
2. `Assets/Resources/` 폴더에 남은 에셋 = 명백히 미사용
3. 비우면 빌드 사이즈 + 메모리 + 의존성 추적 가능성 모두 동시 개선

또한:
- Resources.Load는 string-keyed hidden dependency (`.Instance` 호출과 같은 결의 문제)
- AssetReference는 SerializeField로 노출 → 인스펙터 연결 + 컴파일 타입 강제
- DI/.Instance 정리 작업의 일관성 측면에서 같이 처리하는 게 자연

## Decision

### 마이그레이션 작업

**Phase 2-D: Resources → Addressables (1 PR)**

#### 단계
1. **인벤토리화**: 현재 Resources.Load 호출 36회의 키 + Resources/ 폴더 에셋을 표로 정리
2. **그룹 설계**: feature별 그룹 (Cooking/Prefabs, Shop/Configs, RecipeBook/Prefabs 등)
3. **에셋 이동**: Resources/ → 일반 Assets 경로 (그룹별 폴더), Addressables 마킹
4. **호출 변환**:
   - `Resources.Load<GameObject>("Prefabs/RecipeBook")` → `AssetReference` SerializeField + `LoadAssetAsync<GameObject>()`
   - 또는 정적 키 사용: `Addressables.LoadAssetAsync<GameObject>("recipebook_prefab")`
5. **검증**: 모든 Resources.Load 호출 0건, Assets/Resources/ 폴더 비움
6. **하네스 게이트**: `resources_load = 0`, `assets_resources_folder_files = 0`

#### 패턴 표준화

**현재**:
```csharp
var prefab = Resources.Load<GameObject>(ResourcePaths.Prefab.RecipeBook);
Instantiate(prefab);
```

**변환 (AssetReference)**:
```csharp
[SerializeField] private AssetReferenceGameObject recipeBookRef;

private async UniTask SpawnAsync(CancellationToken ct) {
    var handle = recipeBookRef.InstantiateAsync();
    var instance = await handle.ToUniTask(cancellationToken: ct);
    // 사용
    // 해제 시 Addressables.ReleaseInstance(instance);
}
```

**또는 키 기반 (정적 키)**:
```csharp
public static class AddressableKeys {
    public const string RecipeBookPrefab = "recipebook_prefab";
}

var handle = Addressables.LoadAssetAsync<GameObject>(AddressableKeys.RecipeBookPrefab);
var prefab = await handle.ToUniTask();
```

### 하네스 추가

`tmp/refactor-2026-05/harness/checks/09_addressables_audit.sh`:
- Addressables 그룹 설정 파싱 (AddressableAssetSettings 에셋)
- 코드의 `LoadAssetAsync<T>`, `InstantiateAsync`, `AssetReference` 사용처 추출
- 교차 검사:
  - 그룹에 등록됐는데 코드 참조 없음 = 미사용 키
  - 코드 참조하는데 그룹 미등록 = 빌드 실패 위험
- `_inventory.kv` 출력

### Resources.Load 카운트 게이트

```bash
# Phase 2-D 완료 후 하네스 재실행 시
load_count=$(kv_read antipatterns.tsv resources_load)
if [ "$load_count" -gt 0 ]; then
    echo "ERROR: Resources.Load 잔존 ($load_count건)"
    exit 1
fi

resources_files=$(find Assets/Resources -type f -not -name "*.meta" 2>/dev/null | wc -l)
if [ "$resources_files" -gt 0 ]; then
    echo "ERROR: Assets/Resources/ 잔존 파일 $resources_files"
    exit 1
fi
```

## Consequences

### 긍정
- Resources/ 폴더 통째 빌드 포함 비용 제거
- string-keyed hidden dependency 제거 (AssetReference로 타입 안전)
- 고아 에셋 자동 식별 (그룹에도 코드에도 없으면 미사용)
- 비동기 로드 + 핸들 기반 메모리 관리
- 향후 DLC/원격 콘텐츠 기반 마련

### 부정
- Addressables 패키지 추가 의존
- 학습 곡선 (그룹 설계, 빌드 프로필, 라벨)
- 마이그레이션 1 PR 작업 (1-2주)
- 기존 사용자 빌드의 첫 실행 시 Addressables 카탈로그 빌드 1회 필요

### 시점 — Phase 2-D
- Phase 2-A (asmdef 분리) 완료 후
- Phase 2-C (UniTask) 완료 후 시작 권장 (Addressables의 비동기 API와 UniTask 시너지)
- Phase 3 (GameState 도입) 시작 전 완료

## Resolved Questions

1. ~~빌드 사이즈 측정~~ → 마이그레이션 전후 비교로 효과 정량화
2. ~~인벤토리화 도구를 하네스에 추가~~ → **추가** (`09_addressables_audit.sh`)
3. ~~트리거 조건~~ → **폐기** (지금 수행)

## Decision Required

- [x] 전면 마이그레이션 (D 폐기, C 채택)
- [x] Phase 2-D 시점 (asmdef + UniTask 후)
- [x] `Assets/Resources/` 폴더 비우기 목표
- [x] 하네스에 09_addressables_audit 추가
- [x] resources_load = 0, resources_folder_files = 0 하드 게이트
