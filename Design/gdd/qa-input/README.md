# QA 인풋 인덱스

Aftertaste 게임 **QA용 참고 자료 모음**. GDD를 기반으로, QA가 테스트 케이스를 설계할 때 필요한 사전 정보를 정리.

**스냅샷 시점**: 2026-07-10

---

## 이 폴더의 성격

**"QA 시트"가 아니다.** 테스트 절차·기대치·우선순위·pass/fail 판정은 QA 영역이며, 이 폴더는 **QA가 그 시트를 설계할 때 재료로 쓸 정보**만 담는다.

- ✅ "이런 기능·UI·상호작용·자동 동작이 존재한다"
- ✅ "여기서 이 콘텐츠가 관찰된다"
- ✅ "이건 미구현·사장·의도적 결정이다"
- ✅ "이 부분 최근에 건드렸다"
- ❌ "이걸 눌러서 이 값이 나와야 한다" (← QA)
- ❌ "이 항목은 Critical 우선순위다" (← QA)
- ❌ "이 흐름은 3단계로 검증한다" (← QA)

---

## 🗺 문서 지도

### Feature Inventory — 기능 인벤토리
| 문서 | 대상 | 요약 |
|---|---|---|
| [`feature-inventory-scenes.md`](feature-inventory-scenes.md) | 씬 9개 | 각 씬의 진입/UI/인터랙티브/액션/자동 동작/탈출 |
| [`feature-inventory-systems.md`](feature-inventory-systems.md) | 시스템 16개 | 각 시스템의 기능/UI/상호작용/자동 동작/데이터/접점 |
| [`feature-inventory-content.md`](feature-inventory-content.md) | 콘텐츠 7종 | 요리(66)·재료(35)·레시피(31)·도구(5)·NPC(10)·작물(13)·배달그룹(6)이 게임 내 어디서 관찰되는가 |

### Dev Notes — 개발자 노트
| 문서 | 요약 |
|---|---|
| [`dev-notes.md`](dev-notes.md) | 미구현/사장/알려진 이슈/의도적 결정/미지원. QA 리포트 전 확인 권장 |

### Recent Changes — 최근 변경
| 문서 | 요약 |
|---|---|
| [`recent-changes.md`](recent-changes.md) | 2026-05-01 ~ 2026-07-10 리팩터링/버그픽스/인프라 변경 지점 |

---

## 🔎 자주 찾는 것

| 물음 | 문서 |
|---|---|
| 이 씬에서 뭘 볼 수 있어? | [`feature-inventory-scenes.md`](feature-inventory-scenes.md) |
| 이 시스템은 어떤 기능이 있어? | [`feature-inventory-systems.md`](feature-inventory-systems.md) |
| 이 요리/NPC/재료는 어디서 봐? | [`feature-inventory-content.md`](feature-inventory-content.md) |
| 이거 버그야, 사양이야? | [`dev-notes.md`](dev-notes.md) — 여기서 먼저 확인 |
| 최근에 뭐 건드렸어? | [`recent-changes.md`](recent-changes.md) |
| 파라미터/수치는? | [`../reference/balance-values.md`](../reference/balance-values.md) |
| 특정 콘텐츠 상세? | [`../content/`](../content/) |
| 씬 파일 상세 아키텍처? | [`../scenes/`](../scenes/) |
| 시스템 상세 로직? | [`../systems/`](../systems/) |

---

## 🎯 QA 시작 가이드 (제안)

QA 세션 시작 시 대략 이 순서로 참고하면 효율적:

1. **[`dev-notes.md`](dev-notes.md)** — 사양대로 동작하는 것들 미리 인지 (오탐 방지)
2. **[`recent-changes.md`](recent-changes.md)** — 이번 릴리스에서 건드린 영역 파악
3. **[`feature-inventory-scenes.md`](feature-inventory-scenes.md)** — 씬 순회로 스모크 (Boot → Managers → ... → Settlement)
4. **[`feature-inventory-systems.md`](feature-inventory-systems.md)** — 시스템별 심층 검증 필요 시 참조
5. **[`feature-inventory-content.md`](feature-inventory-content.md)** — 특정 콘텐츠 관찰 지점 확인 시 참조

각 단계에서 발견한 것은 QA가 자신의 테스트 시트에 옮겨 테스트 케이스로 설계.

---

## 🔗 GDD와의 관계

이 폴더는 [`Design/gdd/`](../) 전체를 참조하는 파생 문서다.

- **값이 궁금하면**: 이 문서에는 값을 반복 기재하지 않음. → [`../reference/balance-values.md`](../reference/balance-values.md)
- **로직이 궁금하면**: → [`../systems/`](../systems/)
- **씬 아키텍처가 궁금하면**: → [`../scenes/`](../scenes/)
- **콘텐츠 전수가 궁금하면**: → [`../content/`](../content/)
- **이벤트/도메인 모델**: → [`../reference/`](../reference/)

---

## 📌 문서 원칙

- **실 코드/데이터 기반** — 추측 금지. 값이 모호하면 원본 GDD로 링크.
- **QA 영역 침범 없음** — 테스트 절차/기대치/우선순위는 여기 없음.
- **누락 없이** — 씬 9개·시스템 16개·콘텐츠 7종 모두 커버.
- **버전 관리** — 코드/씬/데이터 변경 시 이 폴더도 갱신 대상.
- **한글 톤** — 문서는 한국어. 필드명·클래스명은 영어 유지.
