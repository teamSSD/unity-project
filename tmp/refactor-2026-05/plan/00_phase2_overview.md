# Phase 2 Master Plan — Overview

_작성: 2026-05-28 / 입력: decisions/000_index.md (ADR v2 ACCEPTED 7건)_

## Phase 2 Goal

ADR v2 결정사항 중 **인프라 작업을 한 번에** 처리. Phase 3 (매니저 → Service 분해) 진입 전에 코드의 "기반"을 통일.

Phase 2는 **기능 변경 없음**. 모든 변경은 구조적/기반적. 베이스라인 다른 지표(라인 수, 함수 길이, 행동)는 거의 변동 없음. 게이트 5개만 0으로 떨어지면 Phase 2 종료.

## 5 Sub-Phase 구성

| # | 이름 | 결정 출처 | 산출 게이트 |
|---|---|---|---|
| 2-A | asmdef 분리 | ADR-002, 004 | (게이트 없음, 구조만) |
| 2-B | SO Event 카탈로그 | ADR-002 | (게이트 없음, 기반만) |
| 2-C | Coroutine → UniTask | ADR-005 | `yield_return_null=0`, `ienumerator_methods=0`, `start_coroutine_calls=0` |
| 2-D | Resources → Addressables | ADR-007 | `resources_load_calls=0`, `resources_folder_files=0` |
| 2-E | DOMAIN.md + rename | ADR-006 | (게이트 없음, 일관성만) |

## Dependency Graph

```
2-A (asmdef)─┬─→ 2-B (SO Event — Schema/Events/ 폴더 필요)
             ├─→ 2-C (UniTask — 안정 구조에서 마이그레이션)
             ├─→ 2-D (Addressables — 안정 구조에서 그룹 설계)
             └─→ 2-E (rename — 안정 구조에서 안전한 rename)
```

**권장 순서**: 2-A → 2-B → 2-C → 2-D → 2-E

근거:
- 2-A는 모든 후속 작업의 전제 (병합 충돌 회피)
- 2-B를 먼저 도입하면 2-C/2-D에서 이벤트 활용 자연
- 2-C 다음 2-D — Addressables 비동기 API가 UniTask와 시너지
- 2-E rename은 가장 마지막 (다른 변경 안정화 후 안전)

## Phase 2 Done Criteria

### 하네스 게이트 (`bash gate.sh`)
```
yield_return_null         OK (0)
ienumerator_methods       OK (0)
start_coroutine_calls     OK (0)
resources_load_calls      OK (0)
resources_folder_files    OK (0)
```

### 정량 (베이스라인 대비)
- 라인 수 / 함수 길이 분포: ±5% (구조 이동 외 큰 변경 없음)
- `.Instance` 호출: ~318 그대로 (Phase 3 작업)
- 매니저 수: 26 그대로 (Phase 3 작업)
- `unitask_methods`, `await_statements`, `asset_reference_decls`, `addressables_using_files`: 0 → 증가

### 정성
- Unity Editor 컴파일 통과
- 모든 EditMode 테스트 GREEN
- 핵심 플레이 시나리오 회귀 없음:
  - Boot → GameStart → New Game → Mall
  - Mall → Cooking (손님 ≥1명 처리) → Settlement
  - Mall → Shop (재료 1개 구매)
  - Mall → Garden (작물 1개 심기/수확)
  - 씬 전환 페이드 정상

## Risk + Mitigation

| 위험 | 영향 | 완화 |
|---|---|---|
| 2-A 진행 중 다른 PR 충돌 | 빌드 영구 깨짐, 머지 지옥 | 2-A 진행 동안 다른 PR 동결 |
| 2-C UniTask 마이그레이션이 씬 전환 깨뜨림 | 게임 진행 불가 | 핫패스 PR(2-C-1)/잔여 PR(2-C-2) 분할, 각 PR 후 수동 회귀 |
| 2-D Resources/ 717 파일 정리 중 사용중 자산 삭제 | 게임 자산 누락 | 사전 인벤토리화 (2-D-0)로 사용/미사용 분류 후 안전 삭제 |
| 2-E rename이 씬/프리팹 깨뜨림 | 씬 직렬화 깨짐 | IDE Rename 도구 사용, `[FormerlySerializedAs]` 적극 활용 |
| Phase 2 누적 작업이 Phase 3 진입 지연 | 일정 누적 | sub-phase별 시간 추정 명시, 초과 시 즉시 재계획 |
| UniTask/Addressables 패키지 버전 충돌 | 컴파일 실패 | 도입 전 호환성 확인, Unity 버전 고정 |

## 시간 추정 전체

| sub-phase | 추정 | 최악 | PR 수 |
|---|---|---|---|
| 2-A asmdef | 1-2주 | 3주 | 1 |
| 2-B SO Event | 3-5일 | 1주 | 1 |
| 2-C UniTask | 3-4일 | 1주 | 2 |
| 2-D Addressables | 5-7일 | 2주 | 2 |
| 2-E rename | 3-4일 | 1주 | 1 |
| **합계** | **약 4-6주** | **8-10주** | **7 PR** |

각 PR 사이에 회귀 검증 시간이 필요하므로, 작업 시간 외에 검증 1-2일씩 추가 예상.

## 사용자 검토 필요 시점

- **2-A 시작 전**: 매핑 표 (어느 파일이 어디로 가는지) 검토
- **2-B 시작 전**: 도메인 이벤트 카탈로그 12개 검토
- **2-C 시작 전**: UniTask 패키지 도입 최종 확인
- **2-D 시작 전**: Addressables 패키지 도입 + 그룹 설계 확인 + 717 파일 인벤토리 결과 확인 (특히 미사용 후보)
- **2-E 시작 전**: DOMAIN.md 사용자 검토 (모호한 용어 정의 확정)

## Phase 2 종료 후 Phase 3 진입 조건

1. 모든 게이트 GREEN
2. 모든 EditMode 테스트 GREEN
3. 핵심 시나리오 회귀 없음
4. 베이스라인 재측정 + history와 비교
5. 사용자 종료 승인

## 각 sub-phase 상세

- [2-A asmdef 분리](phase2a_asmdef.md)
- [2-B SO Event 카탈로그](phase2b_so_events.md)
- [2-C Coroutine → UniTask](phase2c_unitask.md)
- [2-D Resources → Addressables](phase2d_addressables.md)
- [2-E DOMAIN.md + rename](phase2e_domain_rename.md)
