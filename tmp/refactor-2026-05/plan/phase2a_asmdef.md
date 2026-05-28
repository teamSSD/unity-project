# Phase 2-A: asmdef 분리

## 목표

193 .cs 파일을 `Game.Schema` / `Game.Domain` / `Game.Unity` asmdef로 분할 + 폴더 재구성. 3-Layer 의존 단방향을 컴파일러로 강제.

## 가설

레이어 위반(Entities/ → Manager 25 파일, MonoBehaviour와 순수 C# 혼재)의 근본 원인은 **컴파일러가 위반을 잡지 못함**. asmdef로 의존 방향 강제하면:
- 모든 기존 위반이 컴파일 에러로 즉시 발견됨
- 신규 위반 추가가 컴파일 불가
- 부분 리컴파일 (Unity 레이어 수정 시 Schema/Domain 리빌드 안 함)

## 측정 기준

| 지표 | 베이스라인 | 목표 |
|---|---:|---:|
| 어셈블리 수 | 2 (Runtime, EditMode) | 6 (Schema, Domain, Unity, Editor, EditMode, PlayMode) |
| 컴파일 에러 | 0 | 0 |
| 모든 .cs 파일이 한 asmdef에 속함 | n/a | 100% (193/193) |
| 함수/라인 수 분포 | (베이스라인) | **변화 없음** (단순 이동) |
| `.Instance` 호출, 매니저 수, 이벤트 누수 등 | (베이스라인) | **변화 없음** |

## 검증 방법

1. **컴파일**: `Logs/AssetImportWorker0.log` 에러 0
2. **테스트**: EditMode 9개 모두 GREEN (`Window → Test Runner → Run All`)
3. **수동 회귀** (필수):
   - Boot → GameStart → New Game → Mall 진입
   - Mall → Cooking, 손님 1명 응대 → Settlement
   - Mall → Shop, 재료 1개 구매
   - Mall → Garden, 작물 1개 심기/수확
   - 세이브/로드 (게임 종료 후 Continue)
4. **베이스라인 재측정**: `bash run_all.sh` 후 history/ 비교 → 라인/함수/지표 변동 ±5% 이내

## 회귀 방지 체크리스트

- [ ] 마이그레이션 PR 안에서 **코드 내용 수정 금지** (이동만)
- [ ] using 문 추가는 OK (namespace 변경)
- [ ] namespace 추가는 OK (asmdef별 rootNamespace)
- [ ] 클래스 분리/합치기 금지 (Phase 3 작업)
- [ ] 다른 모든 작업 동결 (충돌 방지)
- [ ] `.meta` GUID 보존 (`git mv` 사용, 새로 만들지 말 것)
- [ ] 마이그레이션 전 베이스라인 자동 백업됨 (`history/`)

## 추정 시간

| 단계 | 시간 |
|---|---|
| 매핑 표 작성 + 사용자 검토 | 1-2일 |
| `migration/2a_move.sh` 작성 | 0.5일 |
| asmdef 파일 6개 생성 | 0.5일 |
| 매핑 실행 + 컴파일 에러 해결 | 2-5일 |
| 회귀 검증 | 1일 |
| **합계** | **1-2주** |

## 선결 조건

- [x] ADR-002 ACCEPTED (Layer Architecture)
- [x] ADR-004 ACCEPTED (Asmdef 구조)
- [ ] 매핑 표 사용자 검토 + 승인
- [ ] 다른 PR 동결 합의

## PR 분할

**단일 PR** (분할 시 중간 단계 빌드 깨짐 위험 큼)
- 사전 작업으로 매핑 표는 Git 외부 (이 plan 문서 + tsv)

## 세부 작업

### 1. 새 asmdef 파일 6개 생성

```
Assets/Scripts/
├── Schema/
│   ├── Game.Schema.asmdef           (ref: 없음, UnityEngine 허용)
│   ├── Config/                      (SO 데이터)
│   ├── State/                       (mutable POCO)
│   └── Events/                      (SO Event 베이스 — Phase 2-B에서 채움)
├── Domain/
│   ├── Game.Domain.asmdef           (ref: Schema, UnityEngine 허용)
│   └── (sub-folder: Common, Cooking, Shop, Garden, Mall)
├── Unity/
│   ├── Game.Unity.asmdef            (ref: Schema, Domain, UnityEngine.UI)
│   └── (sub-folder: Common, Cooking, Shop, Garden, Mall, UI)
├── Editor/
│   └── Game.Editor.asmdef           (ref: Schema, Domain, Unity)
└── Tests/
    ├── EditMode/Tests.EditMode.asmdef  (수정: ref Schema, Domain)
    └── PlayMode/Tests.PlayMode.asmdef  (신설: ref Schema, Domain, Unity)
```

asmdef 예시 (Game.Domain):
```json
{
  "name": "Game.Domain",
  "rootNamespace": "Game.Domain",
  "references": ["GUID:..."],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

### 2. 매핑 표 (산출물: `tmp/refactor-2026-05/plan/phase2a_mapping.tsv`)

분류 카테고리:
- **Schema/Config**: ScriptableObject 데이터 정의 (FoodData, CropData, ShopConfig 등)
- **Schema/State**: mutable POCO 직렬화 대상 (PhaseData, BasicStats, InventorySaveData 등)
- **Schema/Events**: GameEvent 베이스 (Phase 2-B에서 채움)
- **Schema/Interfaces**: 인터페이스 (Interfaces/ 폴더 흡수)
- **Domain/Common**: SaveLoad, GameRandom 등 도메인 공통 로직 (순수 C#)
- **Domain/{Feature}**: 도메인별 로직 (현재는 거의 비어있음, Phase 3에서 채움)
- **Unity/Common**: BootLoader, GameSessionRoot 후보, SceneCoordinator 후보
- **Unity/{Feature}/{Scene}**: 씬 컨트롤러
- **Unity/{Feature}/{Behavior}**: MonoBehaviour 컴포넌트 (현재 Entities/ 내용물 대부분)
- **Unity/UI**: HUD, Settings, RecipeBook 등
- **Editor**: 기존 Editor/ 폴더 유지

대표 매핑 예시:
| 현재 경로 | 새 경로 | 분류 |
|---|---|---|
| `Entities/Cooking/cooking/dataType/FoodData.cs` | `Schema/Config/Cooking/FoodData.cs` | Schema/Config |
| `Entities/Cooking/cooking/Food/FoodModel.cs` (258라인 MonoBehaviour) | `Unity/Cooking/Food/FoodBehavior.cs` (rename) | Unity Behavior |
| `Systems/Commons/StatsSystem.cs` (Singleton MonoBehaviour) | `Unity/Common/StatsSystem.cs` (잠정 — Phase 3 분해) | Unity Adapter (잠정) |
| `Utilities/SaveManager.cs` (static class) | `Domain/Common/PersistenceLogic.cs` + `Unity/Common/PersistenceBootstrap.cs` | 분리는 Phase 3, 일단 이동만 |
| `Utilities/SingletonMonoBehaviour.cs` | `Unity/Common/SingletonMonoBehaviour.cs` | Unity Util |
| `Interfaces/Cooking/*.cs` | `Schema/Interfaces/Cooking/*.cs` | Schema/Interfaces |
| `Entities/Mall/delivery/OrderManager.cs` | `Unity/Mall/OrderManager.cs` (잠정) | Unity Adapter (잠정) |

**중요**: Phase 2-A에서는 **rename이나 클래스 분리 안 함**. 단순 이동만. Save Manager는 한 파일을 두 폴더로 쪼개지 않고 잠정 `Unity/Common/SaveManager.cs`에 둠 (Phase 3에서 분해).

### 3. 자동화 스크립트 (`migration/2a_move.sh`)

```bash
#!/usr/bin/env bash
set -eu
MAPPING="${1:-tmp/refactor-2026-05/plan/phase2a_mapping.tsv}"
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"

while IFS=$'\t' read -r src dst; do
    [ -z "$src" ] && continue
    case "$src" in '#'*) continue ;; esac
    [ ! -f "$ROOT/$src" ] && { echo "SKIP (not found): $src" >&2; continue; }
    mkdir -p "$ROOT/$(dirname "$dst")"
    git mv "$ROOT/$src" "$ROOT/$dst"
    [ -f "$ROOT/$src.meta" ] && git mv "$ROOT/$src.meta" "$ROOT/$dst.meta"
    echo "MOVED: $src → $dst"
done < "$MAPPING"
```

### 4. 컴파일 에러 해결 (예상)

이동 후 발생 가능한 에러:

**A. Schema가 Domain/Unity를 참조**
```
error CS0246: The type or namespace name 'XxxService' could not be found
```
- 원인: Schema asmdef가 Domain ref 안 함 (의도된 제약)
- 해결:
  - Schema 클래스가 Domain 타입을 참조하면 안 됨 → Schema에서 제거하거나 인터페이스로 추출
  - 또는 해당 클래스를 Domain으로 이동 (매핑 표 수정 + git mv)

**B. Domain이 MonoBehaviour 사용**
```
error CS0246: The type or namespace name 'MonoBehaviour' could not be found
```
- Domain은 UnityEngine 참조 가능하므로 `using UnityEngine;` 추가 필요
- 만약 의도가 MonoBehaviour를 정말 사용하는 거라면 → Unity asmdef로 이동

**C. using 누락**
- namespace 변경에 따라 using 추가
- IDE 자동 import 활용

각 에러는 개별 commit으로 추적 (`fix: move XxxService to Domain due to schema dependency`).

### 5. 회귀 검증

```bash
# 1. 컴파일
# Unity Editor 열어서 Console 에러 0 확인

# 2. 테스트
# Window → Test Runner → Run All (EditMode)

# 3. 베이스라인 재측정
bash tmp/refactor-2026-05/harness/run_all.sh
# 자동으로 history/에 백업됨

# 4. 변동 확인
diff tmp/refactor-2026-05/reports/history/<prev>.md tmp/refactor-2026-05/reports/baseline.md
# 기대: 함수 길이/라인 분포 변동 ±5%, 안티패턴 카운트 동일
```

### 6. 위험 신호 (작업 중단/롤백 트리거)

- 컴파일 에러가 50+ 발생 시: 매핑 표 재검토
- EditMode 테스트 1개 이상 실패: 매핑 오류 가능성 — 해당 클래스 위치 재확인
- 베이스라인 안티패턴 카운트 변동 5%+: 의도치 않은 코드 변경 — diff 확인
