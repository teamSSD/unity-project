# Phase 2-D: Resources → Addressables 전면

## 목표

모든 동적 에셋 로딩을 Addressables로 통일.
- `resources_load_calls = 0`
- `resources_folder_files = 0`
- 717 파일 인벤토리화 후 미사용 식별 + 사용 파일 그룹 분류

## 가설

Resources는:
- 폴더 통째로 빌드에 포함 (메모리/사이즈 낭비)
- string 키 기반 (타입 불안전, 의존 추적 불가)
- 비동기 API 약함

Addressables는:
- 그룹 단위 빌드 (선택 포함, 향후 DLC 가능)
- AssetReference SerializeField (타입 안전, 인스펙터 노출)
- 비동기 + 핸들 기반 메모리 관리
- 그룹/라벨로 의존 명시

717 파일 인벤토리화로 미사용 파일 식별 가능. 사용 파일은 그룹 설계로 정리.

## 측정 기준 (게이트)

| 지표 | 베이스라인 | 목표 |
|---|---:|---:|
| `resources_load_calls` | 36 | **0** |
| `resources_folder_files` | 717 | **0** |
| `asset_reference_decls` | 0 | 30+ |
| `load_asset_async_calls` | 0 | 30+ |
| `instantiate_async_calls` | 0 | 5+ |
| `addressables_using_files` | 0 | 30+ |
| 빌드 사이즈 | (측정) | 감소 또는 동등 |
| 첫 실행 시간 | (측정) | ±10% |

`bash gate.sh` 의 `resources_load_calls`, `resources_folder_files` 모두 OK.

## 검증 방법

1. **Addressables 패키지 설치 + 컴파일 통과**
2. **Addressables Groups Window** (`Window → Asset Management → Addressables → Groups`): 모든 사용 에셋이 그룹 등록
3. **동적 로드 회귀**:
   - RecipeBook 프리팹 로드/표시 (메인 UI)
   - 모든 SO 데이터 (FoodData, CropData, ShopConfig 등) 로드
   - 모든 미니게임 프리팹 로드
   - 손님/배달 NPC 프리팹 로드
4. **빌드**:
   - Addressables Build → Default Build Script (성공)
   - Player Build (성공)
   - 첫 실행 시 Addressables 카탈로그 로드 정상
5. **하네스 게이트**: `bash gate.sh resources_load_calls`, `gate.sh resources_folder_files`

## 회귀 방지 체크리스트

- [ ] 사전 인벤토리화 (2-D-0)로 717 파일 사용/미사용 분류
- [ ] 미사용 파일 백업 후 삭제 (git 보존)
- [ ] 사용 파일은 그룹별 새 폴더로 이동
- [ ] 인스펙터 참조 깨짐 0 (.unity, .prefab, .asset 모든 GUID 보존)
- [ ] Addressables 그룹 빌드 성공
- [ ] Player 빌드 성공
- [ ] 모든 씬 1회 열람 (참조 깨짐 시각 확인)

## 추정 시간

| 단계 | 시간 |
|---|---|
| 2-D-0 인벤토리화 + 미사용 분류 | 1-2일 |
| 사용자 검토 (미사용 확정) | 0.5일 |
| Addressables 패키지 설치 | 0.5h |
| 그룹 설계 | 0.5일 |
| 미사용 파일 삭제 + 사용 파일 이동 | 1일 |
| Resources.Load 36회 → Addressables 변환 | 1-2일 |
| 회귀 검증 (씬, 빌드) | 1일 |
| **합계** | **5-7일 (2 PR)** |

## 선결 조건

- [x] Phase 2-A 완료 (asmdef 안정)
- [x] Phase 2-C 완료 권장 (Addressables 비동기 API와 UniTask 시너지)
- [x] ADR-007 ACCEPTED
- [ ] Addressables 패키지 도입 최종 확인 (com.unity.addressables)
- [ ] 717 파일 인벤토리 결과 사용자 검토

## PR 분할

### 2-D-0: 사전 인벤토리화 (1 PR)
- `harness/checks/12_resources_audit.sh` 신설
- Resources/ 전체 파일 목록 + Resources.Load 호출 키 + .unity/.prefab/.asset 의 GUID 참조 추출
- 교차 검사: `reports/resources_inventory.md`
- 사용/미사용 분류
- 자동 삭제는 안 함 (사용자 검토 후 다음 PR에서 삭제)

### 2-D-1: 메인 마이그레이션 (1 PR)
- Addressables 패키지 설치
- 그룹 설계 + 사용 파일 이동
- 미사용 파일 삭제 (검토 통과한 것만)
- Resources.Load 36회 → AssetReference / LoadAssetAsync
- Resources/ 폴더 비움

## 세부 작업

### 1. 사전 인벤토리화 (2-D-0)

`harness/checks/12_resources_audit.sh`:
```bash
#!/usr/bin/env bash
# Resources 폴더의 모든 에셋 vs 코드 Resources.Load 키 + 씬/프리팹 GUID 참조 매칭
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT_INV="$DATA_DIR/resources_inventory.tsv"
OUT_UNUSED="$DATA_DIR/resources_unused.list"
OUT_USED="$DATA_DIR/resources_used.list"
: > "$OUT_INV"; : > "$OUT_UNUSED"; : > "$OUT_USED"

RES_DIR="$ROOT/Assets/Resources"
[ ! -d "$RES_DIR" ] && exit 0

# 1. Resources/ 모든 에셋 (.meta 제외)
find "$RES_DIR" -type f -not -name "*.meta" | while IFS= read -r f; do
    rel=${f#$RES_DIR/}
    # Resources.Load 키는 확장자 없는 상대 경로
    key="${rel%.*}"
    meta="${f}.meta"
    guid=""
    [ -f "$meta" ] && guid=$(awk '/^guid:/{print $2; exit}' "$meta")
    printf "%s\t%s\t%s\n" "$key" "$guid" "$f" >> "$OUT_INV"
done

# 2. 코드의 Resources.Load 키 추출
CODE_KEYS=$(xargs0 "$RUNTIME_LIST" grep -hoE 'Resources\.Load[<(]?[^"]*"[^"]*"' 2>/dev/null \
    | grep -oE '"[^"]*"' | tr -d '"' | sort -u)

# 3. 씬/프리팹/에셋의 GUID 참조 추출
GUID_REFS=$(find "$ROOT/Assets" \( -name "*.unity" -o -name "*.prefab" -o -name "*.asset" \) \
    -exec grep -hoE 'guid: [a-f0-9]{32}' {} \; 2>/dev/null \
    | awk '{print $2}' | sort -u)

# 4. 각 Resources 파일이 사용되는지 판정
while IFS=$'\t' read -r key guid path; do
    used=0
    # 코드에서 key 참조?
    echo "$CODE_KEYS" | grep -qxF "$key" && used=1
    # 씬/프리팹에서 GUID 참조?
    [ -n "$guid" ] && echo "$GUID_REFS" | grep -qxF "$guid" && used=1
    if [ "$used" -eq 1 ]; then
        echo "$path" >> "$OUT_USED"
    else
        echo "$path" >> "$OUT_UNUSED"
    fi
done < "$OUT_INV"

echo "resources_audit: total=$(wc -l < $OUT_INV), used=$(wc -l < $OUT_USED), unused=$(wc -l < $OUT_UNUSED)" >&2
```

리포트 생성: `reports/resources_inventory.md`:
- 사용 파일 N개 (요약 + 상위 20개 경로)
- 미사용 파일 M개 (요약 + 카테고리별 분류)
- Resources.Load 키 vs Resources/ 파일 매칭표

### 2. 그룹 설계 (2-D-1 시작)

| 그룹 이름 | 포함 에셋 카테고리 | 로드 방식 | 라벨 |
|---|---|---|---|
| Cooking.Prefabs | 손님, 영수증, 도시락 프리팹 | LazyLoad | cooking |
| Cooking.Data | FoodData, RecipeData, CookingToolData (SO) | Preload | cooking, data |
| Shop.Data | ShopConfig, UpgradeTable (SO) | Preload | shop, data |
| Garden.Data | CropData, FarmTileData (SO) | LazyLoad | garden, data |
| Mall.Prefabs | NPC 프리팹 | LazyLoad | mall |
| UI.Prefabs | HUD, Settings, Dialogue, RecipeBook 프리팹 | Preload | ui |
| Audio.SFX | 효과음 클립 | LazyLoad | audio |
| Audio.BGM | 배경음 클립 | LazyLoad | audio |

그룹 설계 후 `AddressableAssetSettings` 에셋에서 그룹 생성.

### 3. 변환 패턴 표준화

**Before**:
```csharp
var prefab = Resources.Load<GameObject>(ResourcePaths.Prefab.RecipeBook);
var instance = Instantiate(prefab);
```

**After A (AssetReference, 인스펙터 연결)**:
```csharp
[SerializeField] private AssetReferenceGameObject recipeBookRef;

public async UniTask<GameObject> SpawnAsync(CancellationToken ct) {
    var instance = await recipeBookRef.InstantiateAsync().ToUniTask(cancellationToken: ct);
    return instance;
}

// 해제
public void Despawn(GameObject instance) {
    Addressables.ReleaseInstance(instance);
}
```

**After B (정적 키 기반)**:
```csharp
public static class AddressableKeys {
    public const string RecipeBookPrefab = "recipebook_prefab";
}

public async UniTask<GameObject> SpawnAsync(CancellationToken ct) {
    var handle = Addressables.LoadAssetAsync<GameObject>(AddressableKeys.RecipeBookPrefab);
    var prefab = await handle.ToUniTask(cancellationToken: ct);
    return Instantiate(prefab);
}
```

권장: AssetReference 우선 (인스펙터 연결 명시), 동적 키가 꼭 필요한 곳만 B.

### 4. Resources/ 파일 이동

```
Before:
  Assets/Resources/Prefabs/RecipeBook.prefab

After:
  Assets/Bundles/UI/RecipeBook.prefab  ← Addressables 그룹 UI.Prefabs 에 등록
```

`git mv` 사용. .meta GUID 보존 → 씬/프리팹 참조 깨지지 않음.

### 5. 빌드 검증

```bash
# Unity Editor에서:
# Window → Asset Management → Addressables → Groups → Build → Default Build Script
# (성공 확인, 카탈로그 파일 생성 확인)

# Player Build
# File → Build Settings → Build (성공 확인)

# 첫 실행 시 Addressables 카탈로그 정상 로드 (Player Log)
```

### 6. 미사용 파일 삭제 검증

```bash
# 인벤토리 결과 검토 후
while read -r f; do
    git rm "$f" "${f}.meta"
done < tmp/refactor-2026-05/data/resources_unused.list

# 모든 씬/프리팹 1회 열람 → 누락 참조 시각 확인
```

### 7. 위험 신호 (작업 중단/롤백 트리거)

- Addressables 빌드 실패: 그룹 설정 검토
- 게임 실행 시 NRE (에셋 null): AssetReference 인스펙터 연결 누락
- 씬에서 분홍색 머티리얼: 에셋 참조 깨짐 (.meta GUID 보존 실패)
- 첫 실행이 매우 느림: 카탈로그 다운로드 또는 큰 그룹 Preload — 그룹 재설계
