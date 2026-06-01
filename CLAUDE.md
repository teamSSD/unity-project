# Claude Instructions — Project Refactoring Context

## 진단/리팩터링 작업 시 필수 의례

대규모 리팩터링 phase 시작/종료 시 다음을 반드시 따른다:

→ **[tmp/refactor-2026-05/process_phase_boundary.md](tmp/refactor-2026-05/process_phase_boundary.md)**

핵심:
- **단일 원천**: `tmp/refactor-2026-05/diagnosis_status.md` — 마스터 진단의 모든 항목과 상태가 라벨링되어 있음.
- **게이트는 도구**: `tmp/refactor-2026-05/harness/gate.sh` — 진단의 자동 측정 가능 부분만. 게이트 GREEN = 일부 완료, 전체 완료 아님.
- **게이트 점수만 보고 금지** — 항상 `diagnosis_status.md` 미해결 항목과 같이 보고.

## 빠른 명령

```bash
# 게이트 현재 상태 + 베이스라인 delta
bash tmp/refactor-2026-05/harness/gate.sh

# 새 베이스라인 저장 (phase 종료 시 권장)
bash tmp/refactor-2026-05/harness/gate.sh --snapshot

# 회귀 검사 (CI/pre-commit 후보)
bash tmp/refactor-2026-05/harness/gate.sh --check-regression

# 전체 하네스 재실행
bash tmp/refactor-2026-05/harness/run_all.sh

# 단일 게이트 strict 검사
bash tmp/refactor-2026-05/harness/gate.sh <gate_name>
```

## 마스터 진단 위치 (canonical)

- 진단 통합본: [tmp/refactor-2026-05/reports/review_master.md](tmp/refactor-2026-05/reports/review_master.md)
- 거시 리뷰: [tmp/refactor-2026-05/reports/review_macro.md](tmp/refactor-2026-05/reports/review_macro.md)
- 미시 리뷰: `tmp/refactor-2026-05/reports/review_micro_*.md`
- ADR (7건): `tmp/refactor-2026-05/decisions/`
- 상태 추적: [tmp/refactor-2026-05/diagnosis_status.md](tmp/refactor-2026-05/diagnosis_status.md) — **이 파일이 진단의 실시간 상태**

## 회고에서 도출된 함정 (절대 다시 하지 말 것)

1. **게이트 = 목표 동일시** — Goodhart's law. 게이트 0 만족했다고 진단 끝난 게 아님.
2. **Phase 문서 = 작업 목록 동일시** — 마스터 진단의 Critical 항목은 Phase 문서에 없어도 우선.
3. **Facade로 ADR 우회** — "콜러 안전" 명분으로 ADR 정신 외면 금지.
4. **보이는 일 편향** — 지루한 chore (OnValidate 추가, RequireComponent 정리)가 ROI 더 좋을 수 있음.
5. **사용자 신호 과해석** — "다 잘 되는듯", "나중에 디버깅" → 진단 항목 자동 제외 금지.

상세: `[~/.claude/.../memory/feedback_diagnosis_over_gate.md]`

## Unity 작업 시 추가 규칙

- `.unity`/`.prefab` 직접 편집 금지 (Unity MCP 또는 Editor 스크립트만)
- 컴파일 에러는 즉시 멈추고 fix
- 매 commit 전 컴파일 + 콘솔 에러 0 확인
