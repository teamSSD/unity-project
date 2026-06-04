# Master Diagnosis — Status Tracking (Canonical)

_갱신: 2026-06-01 / 원천: `reports/review_master.md`, `decisions/000_index.md`_

> **이 문서가 유일한 진단 상태 원천**. `gate.sh`는 측정 가능한 일부를 자동화한 *파생물*.
> 진단 항목별 상태가 `done/partial/deferred/wontfix` 중 하나로 명시되어야 함.
> "측정 안 됨"이 "안 다룸"으로 침묵 누락되지 않도록 *모든 항목 라벨 필수*.

## 상태 범례

- ✅ **done** — 해결 완료, 측정/검증됨
- ⚠️ **partial** — 부분 진행, 잔여 작업 명시
- 🟡 **deferred** — 의도적 연기, **반드시 이유 + 재방문 시점 명시**
- ❌ **wontfix** — 의도적 미수정, **반드시 이유 명시**
- 🔴 **untouched** — 누락. 결정 안 함. **즉시 라벨링 필요**

---

## Top 5 구조 문제 (마스터 진단)

### #1 — 선언 vs 실제 8배 갭 (Singleton 26, .Instance 318)
- **상태**: ⚠️ partial (Sprint 3 진행 중)
- **결과**: 매니저 26 → ... → 15. Sprint 3-1 Stats / 3-2 Progress / 3-3 Inventory / 3-4 Recipe 도메인(MenuSelection+UnlockedFood+RecipeLookup) facade 제거 (콜러 직접 POCO Service 호출).
- **잔여**: Phase loop 도메인(TimeManager/WeatherSystem/SettlementManager) — Update 루프 의존이라 POCO 변환 비용 큼. UI 매니저(HUDManager/UIManager/etc.)는 MonoBehaviour 적합으로 유지 후보.
- **재방문**: Sprint 3-5 (Phase loop) 또는 다른 Critical로 분기

### #2 — Composition Root가 GameStart에서 멈춤
- **상태**: ⚠️ partial
- **결과**: Refrigerator.Awake race 1건은 해결. 4가지 위반 중 1개만.
- **잔여**: 씬별 Composition Root 확장 (CookingSceneController가 자식에 capacity 주입 등). 매직 스트링/넘버 그대로.
- **재방문**: Phase 5+

### #3 — 이벤트 누수 5+건 (CustomerManager 람다 클로저)
- **상태**: ✅ done (2026-06-01)
- **해결**:
  - CustomerLifecycle.OnAttached 람다 → OnTicketAttached 명명 메서드 + Cleanup -=
  - CustomerLifecycle.OnOrderDelivered → Cleanup에 -= 추가
  - CustomerLifecycle event signature 변경 (`OnCustomerServed/Left`가 this 인자 전달) → CustomerManager에서 명명 메서드 구독
  - CustomerManager.CreateDeliveryTickets 람다 → Dictionary로 추적 + OnDestroy -=
  - CustomerManager.OnCustomerCompleted/OnDestroy에서 모든 lifecycle 이벤트 -= 처리
- **잔존**: 하네스가 event_leaks_files=29 보고하지만 CustomerManager 6건은 regex 오류 (`timer += Time.deltaTime`, `totalEarnings += reward` 같은 산술 연산까지 잡힘). 실제 누수 0. 하네스 정확도 개선 후보(별도).

### #4 — "Unified" 분산 패턴
- **상태**: ✅ done
- **결과**: UnifiedShopManager → ShopUIAdapter + PurchaseService. 3개 Upgrade Manager → 3개 POCO Service. 단, `UpgradeManagerBase<TConfig>` 추출은 안 함 (코드 중복은 잔존).

### #5 — God Class MenuCardController (594라인)
- **상태**: 🟡 deferred
- **이유**: 작업량 5-7일 + UI 회귀 위험 큼 + 현재 사용자 우선순위 신기능 개발로 복귀 추정
- **재방문**: 신기능에서 MenuCard 관련 작업 시 자연 분해 기회

---

## Cross-cutting 패턴 (A~H)

### A. Awake/Start 글로벌 의존성 (17건)
- **상태**: ✅ done (0)

### B. 자체 Singleton 8개
- **상태**: ✅ done (0)

### C. OnValidate 누락 (68 파일, 전체 36%)
- **상태**: 🔴 untouched
- **현재**: 67 (1 감소만)
- **이유**: 게이트 카탈로그에 없어서 무시했음. 마스터 진단 High 우선순위.
- **재방문**: Layer 1 (gate.sh 확장)으로 자동 노출 → 다음 sprint 후보

### D. RequireComponent 누락 (112건)
- **상태**: 🔴 untouched
- **현재**: 112 (변동 없음)
- **이유**: 동일 (게이트 없음, 무관심)
- **재방문**: Layer 1으로 노출 → 다음 sprint 후보

### E. View가 매니저 6개 직접 호출 (ShopDetailPanel)
- **상태**: ⚠️ partial
- **결과**: Service 추상화 도입 (StorageUpgrade/Tool/Farm 등). 단 ShopDetailPanel의 `.Instance` 12회 직접 호출은 그대로.
- **재방문**: Phase 5+

### F. 유사 매니저 분립 (Sound 3, Upgrade 3)
- **상태**: ✅ done
- **결과**: Sound 1개로 통합, Upgrade 3개 POCO Service로 분리.

### G. GameStart Init 중복 (ProcessContinue/NewGame ~30라인)
- **상태**: 🔴 untouched
- **이유**: phase 문서에 없어서 누락
- **재방문**: 다음 sprint 후보

### H. Piggyback 매니저 (RecipeData/UnlockedFood가 PhaseData에 얹힘)
- **상태**: 🔴 untouched
- **이유**: phase 문서에 없어서 누락
- **재방문**: Phase 5+ (데이터 모델 정리)

---

## 정량 목표 (ADR 인덱스 14개)

| 지표 | 베이스 | 목표 | 현재 | 상태 |
|---|---:|---:|---:|---|
| Singleton 매니저 | 26 | 0 (Adapter 5-7) | 22 | ⚠️ partial |
| `.Instance` 호출 | 318 | ≤10 | 325 | ❌ wontfix (facade 패턴 비용. 매니저 삭제 시 자동 해결) |
| Awake/Start `.Instance` | 17 | 0 | 0 | ✅ done |
| Entities/ `.Instance` | 25 | 0 | N/A | ✅ done (asmdef 마이그레이션) |
| 자체 Singleton | 8 | 0 | 0 | ✅ done |
| `yield return null` | 21 | 0 | 0 | ✅ done |
| `Resources.Load` (effective) | 36 | 0 | 0 | ✅ done |
| `Resources/` 파일 | 717 | 0 | 0 | ✅ done (TMP 제외) |
| OnValidate 누락 | 68 | ≤5 | 67 | 🔴 untouched |
| RequireComponent 누락 | 112 | 0 | 112 | 🔴 untouched |
| 테스트 커버리지 | 7.6% | 25%+ | 15.6% | ⚠️ partial |
| 최대 파일 | 594 | ≤300 | 584 | 🟡 deferred (MenuCardController) |
| 함수 41+ 라인 | 24 | ≤12 | 23 | 🔴 untouched |
| 함수 61+ 라인 | 2 | 0 | 3 | ❌ 악화 → 🔴 (SaveManager LoadAll 119라인) |

**진척**: 8/14 done, 2 partial, 1 deferred, 3 untouched.

---

## 신규 도입 (Phase 2-4 부작용)

### N.1 SaveManager 비대화
- LoadAll: ~50 → **119라인**, SaveAll: ~40 → **79라인**
- 원인: GameState ↔ legacy SaveData 변환 boundary 집중
- 상태: 🟡 deferred (도메인별 Adapt 메서드 추출 후보)

### N.2 GameSessionRoot 책임 혼재
- WireServices 안에 CSV 파싱(`ParseQuestMenus` 38라인) 인라인
- 상태: 🟡 deferred (QuestMenuCatalogBuilder 추출 후보)

### N.3 .Instance 호출 +7 회귀
- 318 → 325. Facade 패턴 부작용
- 상태: ❌ wontfix (facade 매니저 삭제 시 자동 해결)

---

## Phase boundary 의례 (필수)

매 phase 시작/종료 시:

1. **시작**: 이 파일 열고 🔴 untouched 항목 + ⚠️ partial 항목 top 5 확인
2. **종료**: 진행한 항목 상태 갱신 (done/partial). 새로 발견한 누락은 🔴 추가.
3. **gate.sh 실행** + delta 출력 확인 → 악화된 항목은 본 문서에 N.X로 기록
4. **사용자 보고 시 이 파일 링크 + 미해결 top 3 명시** (게이트 점수만 보고 금지)

위 의례를 빼먹지 않기 위해 `process_phase_boundary.md` 별도 작성 + CLAUDE.md 참조 추가.
