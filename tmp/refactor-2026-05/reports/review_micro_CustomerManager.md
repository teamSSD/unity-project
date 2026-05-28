# Micro Review: CustomerManager (+ cooking/ 컴포넌트들)

_Subagent 결과 — CustomerManager 422라인, depth 6, 이벤트 누수 5건, hidden dep 9_

## 1. 현재 분리 상태 평가

**분리 상태: 양호하나 경계선 모호**

이전 CustomerEntry God class 분할은 기본적으로 잘 수행됨:
- **CustomerManager**: 고객 흐름 오케스트레이션, 스포닝 타이밍, 세션 통계
- **CustomerSpawner**: 위치 지정, 3가지 고객 유형 프리팹 인스턴스화, 대기열 위치 풀
- **OrderTicketController**: 영수증 생성, 배치, 정렬
- **CustomerLifecycle**: 단일 고객의 상태 머신 (주문→대기→인수/퇴출)

### 남은 문제
- **CustomerLifecycle은 Value Object가 아닌 State Container**: 생성 직후 `StartOrder()` 호출 필수 (암묵적 계약)
- **CustomerManager 책임 혼재**: 스포닝 + 통계 + 게임 종료 판정이 한 클래스
- **CustomerSpawner 분산**: 위치 계산/프리팹 선택은 책임이나, 고객 타입별 행동은 외부 엔티티에 위임 (분리 점이 일관되지 않음)

## 2. 이벤트 구독 누수 5건 분석

| # | 위치 | 평가 | 비고 |
|---|---|---|---|
| 1 | CustomerManager L83 `StatManager.onTimeEnd += OnTimeEnd` | **False Positive** | L357에서 OnDestroy 시 `-=` 정상 |
| 2 | CustomerManager L199 `lifecycle.OnCustomerServed += λ` | **실제 누수 (중대)** | 람다, 구독 해제 없음, 클로저로 lifecycle 캡처 |
| 3 | CustomerManager L200 `lifecycle.OnCustomerLeft += λ` | **실제 누수 (중대)** | 동일 패턴 |
| 4 | CustomerManager L146 `ticket.onTake += λ` (delivery) | **실제 누수 (중대)** | 배달 영수증, 구독 해제 없음 |
| 5 | CustomerLifecycle L68 `waitingCustomer.onExit += OnCustomerTimeout` | **False Positive** | L189 Cleanup에서 `-=` 정상 |
| +6 | CustomerLifecycle L71 `orderTicket.OnAttached += λ` | **실제 누수 가능 (중상)** | 정리 없음 |
| +7 | CustomerLifecycle L72 `orderTicket.onTake += OnOrderDelivered` | **실제 누수 (중상)** | this 참조 유지 |
| +8 | OrderTicketController L49 `ticketModel.onTake += λ` | **False Positive** | 자기 정리 (RemoveTicket) |

**실제 누수 5건 (False Positive 3건 제외, 추가 발견 2건 포함)**: L199, L200, L146, L71, L72
- 모두 람다식 또는 instance 메서드 → 구독 시 원본 참조 보존 못함
- 세션 길이에 비례해 누적

## 3. 식별된 안티패턴

### A. Hidden Dependency (Singleton 9회)
- ProgressSystem (3), OrderManager (4), SoundManager (1), ValidationFeedbackUI (3), SettlementManager (3), 기타
- 강결합, 테스트 불가, 초기화 순서 암묵적 의존

### B. 이벤트 핸들러 클로저 남용
- 람다식이 lifecycle/orderTicket 캡처 → 구독 해제 시 원본 대리자 참조 분실

### C. 단위 테스트 불가능한 상태 머신
- `new CustomerLifecycle(...)` 후 `StartOrder()` 호출 안 하면 동작 안 함 (암묵적 계약)
- IDisposable 미구현

### D. 배달 주문 특수 처리 (CreateDeliveryTickets L125-157)
- 일반 고객 흐름과 분기, 람다 누수 위험 높음

### E. HandleCustomerSpawning 들여쓰기 (45라인)
- 단순 가드문이 다중 라인으로 — 한 줄로 정리 가능

### F. 명시적 생명주기 호출 강제
- `Cleanup()`, `ReleaseWaitingPosition()` 호출이 호출자 책임
- IDisposable 미구현 → C# 관례 어긋남

## 4. 의존 그래프 / 결합 분석

```
CustomerManager
├── spawner: CustomerSpawner
├── ticketController: OrderTicketController
└── activeCustomers: List<CustomerLifecycle>
    └── lifecycle 이벤트로 역방향 통신 (정상)

CustomerLifecycle
├── spawner, ticketController (주입)
├── waitingCustomer, orderTicket (생성)
└── 이벤트 발행 → CustomerManager

CustomerSpawner (Stateless utility + position pool)
OrderTicketController (Factory + active list)
```

- **순환 의존 없음** ✓
- 이벤트 비율 ~29% (24개 의존 포인트 중 7건) — 적절한 느슨한 결합
- **그러나 이벤트 누수 5건이 반복 고객 생성 루프에서 발생** → 세션 길수록 누적

## 5. 분리 가능한 추가 책임

### A. SessionStatisticsTracker
- totalOrders, perfectOrders, totalEarnings 추적을 별도 컴포넌트로
- LogSessionSummary 호출 포함
- 독립 테스트 가능

### B. GameEndChecker
- CheckGameEnd, OnTimeEnd, OnGameEnd 이벤트
- StatManager의 onTimeEnd와 TicketController의 HasActiveTickets 결합

### C. EventSubscriptionCleaner / IDisposable
- 람다 누수 자동 정리
- using 패턴 또는 명시적 Dispose

### D. DeliveryOrderProcessor
- CreateDeliveryTickets 책임 캡슐화
- 배달 특수 처리 분리

### E. ISoundService / IGamePhaseProvider 주입
- Singleton 의존성 → DI 인터페이스
- 테스트 가능

## 6. 우선순위 권고

| # | 항목 | 우선순위 | 난이도 | 추정 시간 |
|---|---|---|---|---|
| 1 | **이벤트 누수 5건 수정 (L71, L72, L146, L199, L200)** | **High** | Low | 2-3h |
| 2 | Singleton → DI (ISoundService, IPhaseProvider) | High | High | 8-12h |
| 3 | SessionStatisticsTracker 분리 | Med | Low | 2-3h |
| 4 | DeliveryOrderProcessor 분리 | Med | Med | 2-3h |
| 5 | CustomerLifecycle IDisposable 구현 | Med | Low | 1-2h |
| 6 | GameEndChecker 분리 | Med | Med | 3-4h |
| 7 | HandleCustomerSpawning 인덴트 개선 | Low | Very Low | 30m |

### 권장 실행 순서
1. **Week 1**: 이벤트 누수 5건 즉시 수정 (메모리 누수 차단)
2. **Week 2-3**: SessionStatisticsTracker 분리
3. **Week 3**: DeliveryOrderProcessor 분리
4. **Month 1**: Singleton → DI 전환 (테스트 가능성)
5. (옵션): GameEndChecker 분리

**핵심**: 분리는 잘 됐지만 이벤트 누수가 즉시 처리 필요. DI 전환은 큰 일이지만 효과 큼.
