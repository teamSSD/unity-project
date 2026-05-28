# ADR-006: Domain Language — DOMAIN.md

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Blocking Phase 2

## TL;DR (v2 결정 요약)

- `DOMAIN.md` 작성 (프로젝트 루트에 배치)
- AI가 초안 → 사용자가 모호한 정의 확정
- **DOMAIN.md의 모든 동의어 통일 결정을 rename PR로 일괄 적용** (Phase 2-E)
- 부분 통일(3-5개)은 폐기

## Context

코드에 등장하는 도메인 용어:
- **Menu** / **Recipe** / **Bento** / **Food** / **Ingredient** / **Tool** (음식/조리 관련)
- **Customer** / **Order** / **Delivery** / **DeliveryQuest** / **NPC** (손님/주문)
- **Shop** / **Item** / **Upgrade** / **Storage** / **Crop** / **Farm** / **Tile** (상점/농장)
- **Stats** / **Progress** / **Phase** / **Day** / **Time** / **Stamina** / **Money** / **Settlement** (게임 진행)
- **Action** / **MenuSelection** / **BentoSelection** / **Validation** (액션/선택)

문제:
- 같은 단어가 여러 의미로 쓰일 가능성 (예: `Menu` ≠ `Recipe` ≠ `Bento` ≠ `Food`, 그러나 코드에서 혼용?)
- 새 코드 작성 시 "이 개념을 뭐라 부르지" 망설임 → 동의어가 또 생김
- 리팩터링 시 "Customer ↔ NPC", "Order ↔ DeliveryQuest" 같은 용어 통합 결정 필요
- 도메인 모델 다이어그램 부재 → 신규 인원 또는 미래의 나도 코드 읽어 추론해야 함

## Recommendation: DOMAIN.md 작성 (Phase 1.5 산출물)

ADR로서의 결정: **DOMAIN.md를 Phase 2 시작 전 필수 산출물로 작성**. 내용은 작성 과정에서 채워짐.

### DOMAIN.md 구조 (제안)

```markdown
# Game Domain Glossary

## Core Concepts (요리 게임)

### Food
정의: 게임 내에서 다루는 모든 식재료/완성품의 단위.
유형: GARBAGE / INGREDIENT / PROCESSING / MAIN / SIDE
관계: Food는 Recipe의 입력/출력으로 등장.
코드: FoodData (Schema), FoodLogic (Domain), FoodBehavior (Unity)
예시: 양배추(INGREDIENT), 슬라이스된 양배추(PROCESSING), 양배추말이(MAIN)

### Recipe
정의: 입력 Food들 + 미니게임 → 출력 Food 변환 규칙.
관계: 입력은 Ingredient(들), 출력은 단일 Food.
코드: RecipeData (Schema), RecipeChain (Domain — DFS 계산)
예시: [양배추 → SliceMiniGame → 슬라이스된 양배추]

### Bento
정의: 손님에게 제공하는 최종 도시락 단위. Main 1 + Side 3 슬롯 구성.
관계: 각 슬롯에 Food(MAIN 또는 SIDE 타입) 배치.
코드: BentoModel (Unity), BentoData (Schema)
**Menu와의 차이**: ...

### Menu
정의: 사장이 다음 페이즈에 제공할 도시락 목록. 메뉴 선택 액션에서 정함.
관계: Menu는 여러 Bento의 집합? 또는 단일 Bento?
코드: MenuSchema (Schema), MenuSelection
**Recipe와의 차이**: Recipe는 변환 규칙, Menu는 영업할 메뉴 (라인업)

### Ingredient
정의: ... (Food의 동의어인가, INGREDIENT 타입 Food인가?)
**Food와의 차이**: ...

### CookingTool / Tool
정의: 미니게임을 통해 Food 변환을 수행하는 도구.
유형: T001 (불판/Griddle), T002 (칼/Slicer), T003 (믹서), T004 (?), T005 (?)
코드: CookingToolData (Schema), CookingToolModel (Unity)
관계: 1 Tool ↔ 1 MiniGame

### MiniGame
정의: ...
유형: M001~M005

---

## Customer Domain

### Customer
정의: 가게에 방문해 주문하는 NPC.
상태: Ordering → Waiting → Taking (또는 Exiting)
유형: OrderingCustomer / WaitingCustomer / TakingCustomer (또는 상태 머신)
코드: ...
**NPC와의 차이**: NPC는 더 넓은 개념? 또는 Customer는 NPC의 한 종류?

### Order
정의: 손님이 요청한 도시락 사양.
관계: Customer가 발주, Bento가 응답.
코드: OrderManager, OrderTicketModel

### Delivery
정의: 가게 외부에서 받는 사전 예약 주문.
관계: Customer가 가게에 오는 것과 다른 흐름.
코드: DeliveryNpcDialogueInteraction, OrderManager (혼재?)

### DeliveryQuest
정의: ... (Delivery와 같은가 다른가)

---

## Progress / Stats Domain

### Day
정의: 게임 진행의 큰 단위. 페이즈들로 구성.

### Phase
정의: Day의 세부 단위. (Morning, Afternoon, Evening, Night ?)
관계: Day = Phase 시퀀스
코드: PhaseType enum, PhaseData

### Action
정의: 플레이어가 한 Phase 동안 선택하는 활동.
유형: MenuSelect, Work, Rest, ... (열거)
코드: ActionType, ActionSelectionManager

### Stats
정의: 캐릭터의 수치 상태. Money / Stamina / 등.
**Progress와의 차이**: Progress는 시간/페이즈 진행, Stats는 자원/능력치

### Settlement
정의: 페이즈 또는 Day 종료 시 정산.
관계: 일일 매출, 비용, 순이익 계산.

---

## Shop / Upgrade Domain

### Shop
정의: ...
유형: 일반 Shop (재료 구매)? 또는 통합 Shop?

### Upgrade
정의: 도구/창고/농장의 레벨 상승.
유형: StorageUpgrade, ToolUpgrade, FarmUpgrade

### Storage
정의: 재료를 보관하는 위치.
유형: Refrigerator / UpperShelf / LowerShelf

### Crop / Farm / Tile
정의: ...

---

## Validation

### Validation / Feedback
정의: 손님이 도시락을 검수한 결과.
점수: ...

---

## 결정된 용어 매핑 (모호한 부분 해소)

| 모호한 용어 | 통일 후 | 이유 |
|---|---|---|
| Customer vs NPC | Customer (손님 한정), NPC (모든 비-플레이어 캐릭터) | ... |
| Order vs DeliveryQuest | Order (가게 내), DeliveryQuest (가게 외) | ... |
| Menu vs Recipe vs Bento | Menu (라인업), Recipe (변환규칙), Bento (도시락 단위) | ... |
| ... | ... | ... |

---

## 도메인 다이어그램

```
[Player] ──선택── [Menu] ─포함─→ [Bento] ─구성─→ [Food (MAIN), Food (SIDE)*3]
                                                      ↑
                                                    [Recipe] ─적용─ [CookingTool] ─실행─ [MiniGame]
                                                      ↑
                                                    [Food (INGREDIENT)*]

[Customer] ─발주─→ [Order] ─기대─→ [Bento]
[NPC (배달)] ─발주─→ [DeliveryQuest] ─기대─→ [Bento]

[Day] ─구성─→ [Phase]* ─수행─→ [Action]
[Stats: Money, Stamina, ...] ─변동─→ [Settlement (정산)]

[Shop] ─판매─→ [Food (INGREDIENT)]
[Shop] ─판매─→ [Upgrade {Storage, Tool, Farm}]
```
```

### 작성 방식

1. 코드를 한 차례 훑어서 등장 용어 수집 (이미 부분 완료)
2. 각 용어의 정의를 코드와 기획 문서(`Design/`, `Recipe_Guide.md`, `INGREDIENT_VARIANT_ANALYSIS.md` 등)에서 찾음
3. 동의어/혼용 사례 식별
4. 사용자(기획자/플레이어) 확인 — 통일 결정 필요한 부분 질문
5. 결정 매트릭스 작성

## Consequences

### 긍정
- 새 클래스/파일 명명 시 망설임 제거
- 리팩터링 중 동의어 통합 작업의 근거
- 새 인원/미래의 자신에게 문서로 남음
- 코드 리뷰에서 "이거 ~로 부르는 게 맞나" 토론 종결

### 부정
- 작성 비용 (4-8시간 추정)
- 동의어 통합 시 코드 광범위 변경 (선택적, ADR에 반영 가능)
- 기획자/사용자에게 확인 필요한 부분이 다수 있을 가능성

### 의존성
- ADR-006 자체는 다른 ADR에 의존하지 않음
- 하지만 ADR-001 (State), ADR-004 (Folder)의 명명에 영향
  - 예: GameState 안의 sub-state 이름 (KitchenState vs CookingState?)
  - 예: 폴더 이름 (Cooking vs Kitchen?)

## Open Questions

1. **작성 주체**?
   - 사용자 단독
   - 사용자와 같이 (질문/응답)
   - AI가 초안, 사용자 검토
   - **임시 추천**: AI 초안 → 사용자 모호한 부분 확정 (가장 빠름)

2. **DOMAIN.md 위치**?
   - 프로젝트 루트 (`/DOMAIN.md`)
   - `Design/` 폴더 (기획 문서)
   - `Assets/Scripts/DOMAIN.md` (코드 옆)
   - **임시 추천**: 프로젝트 루트 (README와 같이 보임)

3. **용어 통일 작업의 범위** — **RESOLVED**:
   - **DOMAIN.md + 전면 통일 (rename PR)**
   - 부분 적용은 또 다른 일관성 문제를 만듦. 결정된 모든 통일사항을 일괄 적용
   - Phase 2-E 단일 PR로 묶음 (asmdef 마이그레이션 후, GameState 작업 전)

4. **다국어**?
   - 현재 코드는 한국어 주석 + 영어 식별자 혼재
   - DOMAIN.md는 한국어로 (이미 그렇게 작성된 예시)
   - **결정**: 한국어 정의 + 영어 식별자 매핑

## Decision Required

- [ ] DOMAIN.md를 Phase 1.5 산출물로 작성 동의
- [ ] 작성 주체 (Open Q1)
- [ ] 위치 (Open Q2)
- [ ] 용어 통일 범위 (Open Q3)
- [ ] DOMAIN.md 작성 후 사용자 검토 필수 (모호한 정의 확정 위해)
