# Phase Boundary Ceremony

Phase 시작 / 종료 시 반드시 수행하는 의례. **목적**: 게이트가 진단을 덮어쓰지 않도록 강제.

## 왜 의례인가

자동 게이트는 *측정 가능한 부분*만 보여줌. 마스터 진단의 절반(OnValidate, RequireComponent, MenuCardController 분해, 이벤트 누수 등)은 의식적으로 끌어와야 함. 그렇지 않으면 침묵으로 빠짐 (2026-06-01 회고 참조 [feedback_diagnosis_over_gate](.../memory/feedback_diagnosis_over_gate.md)).

---

## Phase 시작 의례 (필수)

1. **`diagnosis_status.md` 열기**
   - 🔴 untouched 항목 + ⚠️ partial 항목 top 5 확인.
   - 신규 phase가 이 중 어느 것을 다루는지 명시.
2. **`bash gate.sh` 실행**
   - 현재 게이트 상태 출력. 최소 베이스라인 vs 현재 delta 확인.
3. **사용자에게 보고**
   - "이번 phase가 다룰 진단 항목 = [X, Y, Z]"
   - "그 외 잔존 🔴 항목 = [...]" (의도적 deferred임을 명시)

## Phase 작업 중

- 새로운 안티패턴/회귀 발견 시 `diagnosis_status.md`에 N.X로 즉시 기록 (커밋 메시지에도 명시).
- Facade/Adapter 도입 시: ADR 정신 재해석 → 커밋 메시지에 답 ("이 facade가 ADR-XXX의 '...' 정신을 만족하는지").
- 사용자가 "검증 후순위 / 디버깅 나중에 / X→Y" 같은 신호를 줘도 **마스터 진단 Critical 항목을 자동 제외하지 않음**. 명시 거절 없으면 살아 있는 것.

## Phase 종료 의례 (필수)

1. **`bash gate.sh`** 다시 실행
   - delta 컬럼 확인. ⚠️ 악화 표시된 항목 있나?
   - 악화 있으면 `diagnosis_status.md`에 N.X로 기록 + 이유 + 재방문 시점.
2. **`bash gate.sh --check-regression`** (선택, CI/pre-commit 후보)
   - 베이스라인 대비 악화만 출력. 있으면 exit 1.
3. **`diagnosis_status.md` 갱신**
   - 이번 phase에서 다룬 항목 → done/partial로 라벨 변경.
   - 새로 발견한 누락 → 🔴 untouched로 등록.
4. **사용자 보고**
   - "Phase X 종료. 게이트: NN/MM. 다룬 진단 항목 [...]. 새 누락 [...]. 미해결 top 3 [...]"
   - **금지 표현**: "다 끝났습니다", "GREEN 달성", "완료" 단독 사용 — 진단 미해결 항목 옆에 두고 비교한 문장으로.
5. **사용자가 "다음 phase로" 결정하면 `bash gate.sh --snapshot`** — 새 베이스라인.

---

## 의례를 빠뜨리지 않으려면

- `CLAUDE.md`에서 본 문서 참조.
- 새 phase 작업 시작 전 첫 turn에 무조건 `bash gate.sh` + `diagnosis_status.md` 읽기 (Bash + Read).
- "진척 보고" 작성 시 게이트 점수만 헤드라인으로 쓰는 자기 검열.

---

## 안티 패턴 (회고에서 빠진 함정)

| 함정 | 발생 시점 | 방지 |
|---|---|---|
| 게이트 0 = 완료 자축 | Phase 종료 시 | 게이트는 진단의 *부분 측정*. 진단 미해결 항목과 비교 필수 |
| Phase 문서 = 작업 목록 | Phase 진행 중 | 마스터 진단의 Critical/High는 Phase 문서에 없어도 자동 우선 |
| Facade로 우회 | 콜러 수정 부담 클 때 | ADR 정신 명시적 재해석 + 사용자 확인 |
| "지루한 chore 안 함" | Phase 후반 | OnValidate 등 정리 작업의 ROI는 종종 아키텍처 작업보다 좋음 |
| "검증 나중에" 무한 연기 | 사용자 신호 받았을 때 | 명시 거절 아니면 진단 항목 살아있음 |
| 누적 drift 안 봄 | 매 commit 통과 | `gate.sh --check-regression` |
