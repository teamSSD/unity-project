# Architecture Decision Records — Phase 1.5

_v2 ACCEPTED: 2026-05-28 / Phase 2 마스터 플랜의 입력_

## 상태 범례
- **PROPOSED**: 작성 완료, 사용자 결정 대기
- **ACCEPTED**: 합의됨, Phase 2 입력
- **REJECTED**: 다른 안 채택
- **SUPERSEDED**: 다른 ADR로 대체

## 모든 ADR: **ACCEPTED**

| # | 주제 | 결정 |
|---|---|---|
| [001](ADR-001-state-architecture.md) | State Architecture | **GameState POCO + Slim Services**. Stateful 매니저 26→**0**, Unity Adapter 5-7개만 잔존. Feature-first 마이그레이션. |
| [002](ADR-002-layer-architecture.md) | Layer Architecture | **3-Layer 하이브리드**. "Model = 순수 C#" → "MonoBehaviour 금지, Unity 값 타입 허용". **SO Event를 cross-cutting 알림의 기본 메커니즘으로 채택**. |
| [003](ADR-003-save-load.md) | Save/Load | **ISaveable 폐기, GameState 직행**. PersistenceService 한 줄 직렬화. ADR-001과 동시 진행. |
| [004](ADR-004-asmdef-folders.md) | Asmdef / Folders | **3 asmdef** (Schema/Domain/Unity) + Editor + EditMode + **PlayMode 신설**. Schema 안에 Config(SO)/State(POCO) 폴더 분리. |
| [005](ADR-005-async-runtime.md) | Async Runtime | **UniTask 전면 마이그레이션** (Phase 2-C). 점진 폐기, 코루틴 0 목표. |
| [006](ADR-006-domain-language.md) | Domain Language | **DOMAIN.md 작성 + 전면 rename PR**. 부분 통일 폐기. |
| [007](ADR-007-asset-loading.md) | Asset Loading | **Resources → Addressables 전면 마이그레이션** (Phase 2-D). 보류 폐기, Resources/ 폴더 비움. |

## 결정 후 Phase 2/3 구조

```
[Phase 2 — 인프라 (전 파일 영향 작업을 한 번에)]
  2-A: asmdef 분리 (193 파일 이동) — 1 PR
        ↓
  2-B: SO Event 카탈로그 정의 + 도메인 이벤트 도입 — 1 PR
        ↓
  2-C: 코루틴 → UniTask 전면 — 2 PR (핫패스 + 잔여)
        ↓
  2-D: Resources → Addressables 전면 — 1 PR
        ↓
  2-E: DOMAIN.md + 용어 통일 rename — 1 PR

[Phase 3 — 본격 도메인 리팩터링 (feature 단위)]
  3-A: Cooking feature (GameState + Service 분해)
  3-B: Shop feature
  3-C: Garden + Mall feature
  3-D: 잔여 매니저 (UICoordinator, SceneCoordinator, GameClock, ...)

[Phase 4 — 검증 + 정리]
  4-A: 매니저 잔재 제거 (SaveManager 등 임시 잔존물)
  4-B: 테스트 커버리지 확대 (목표 25%+)
  4-C: 하네스 게이트 모두 GREEN 확인 (yield_null=0, resources=0, .Instance≤제한)
```

## 정량 목표 (1차)

| 지표 | 베이스라인 | Phase 2 종료 | Phase 3 종료 | Phase 4 종료 |
|---|---:|---:|---:|---:|
| Singleton 매니저 | 26 | 26 | 5-10 | 0 (Unity Adapter 5-7) |
| `.Instance` 호출 | 318 | ~250 (이벤트 도입) | ~50 | ≤10 |
| Awake/Start의 `.Instance` | 17 | 17 | 0 | 0 |
| Entities/ `.Instance` 파일 | 25 | (구조 이동) | 0 | 0 |
| 자체 Singleton | 8 | 0 (베이스 통일) | 0 | 0 |
| `yield return null` | 21 | **0** | 0 | 0 |
| `Resources.Load` | 36 | **0** | 0 | 0 |
| `Assets/Resources/` 파일 | (?) | **0** | 0 | 0 |
| OnValidate 누락 | 68 | ≤20 | ≤10 | ≤5 |
| RequireComponent 누락 | 112 | ≤30 | ≤10 | 0 |
| 테스트 커버리지 | 7.6% | 10%+ | 18%+ | 25%+ |
| 최대 파일 | 594 | ≤500 | ≤400 | ≤300 |

## 의존 관계 (수정)

```
ADR-006 (Domain) ──→ 모든 명명/rename 결정 (Phase 2-E)

ADR-002 (Layer) ──→ ADR-004 (Asmdef) ──→ Phase 2-A 폴더 이동
                  └─→ Phase 2-B SO Event 카탈로그

ADR-005 (Async) ──→ Phase 2-C UniTask 전면

ADR-007 (Asset) ──→ Phase 2-D Addressables 전면

ADR-001 + ADR-003 ──→ Phase 3 GameState + PersistenceService (feature 단위)
```

## 다음 단계 (Phase 2 마스터 플랜)

각 Phase 2-X 작업을 다음과 같이 상세화:
- **목표**: 무엇이 달라지나
- **가설**: 왜 이게 해법인가
- **측정 기준**: 하네스의 어떤 지표가 어떻게 변해야 성공인가
- **검증 방법**: 회귀 없음을 어떻게 확인하나 (수동/자동)
- **회귀 방지 체크리스트**: 무엇을 깨면 안 되나
- **추정 시간**
- **선결 조건**: 어떤 ADR/작업이 완료돼야 시작 가능
- **PR 분할**: 단일 PR vs 여러 PR

Phase 2 마스터 플랜은 다음 산출물.
