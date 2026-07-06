# Code↔View 경합 심층 디깅 — 문제/위험도/대안/비용 — 2026-07-06

_[review_2026-07_code_view.md](review_2026-07_code_view.md) 진단의 근본원인 A·B·D를 실제 코드 정독으로 파고든 후속._
_방법: GameSessionRoot / MallSceneController / CookingSceneManager / ShopDetailPanel / ShopUIAdapter / FoodModel / CookingToolModel / ConfirmModal / BentoModel 정독._
_각 문제 = **문제(what) / 기존 코드 위험도(risk of leaving) / 대안(alternatives) / 비용(cost)**._

---

## ⚠️ 검증된 확정 버그 — FoodModel.AddToBento 극성 반전

`BentoModel.AddIngredient`는 성공 시 `true` 반환 (BentoModel.cs:89), `CookingToolModel.AddIngredient`와 **동일 계약**. 따라서:

- `FoodModel.AddToCookingTool` (FoodModel.cs:203) — `if (reflected)` → `ConsumeFood`. ✅ 담기 성공 시 인벤토리 소비 (정상).
- `FoodModel.AddToBento` (FoodModel.cs:231) — `if (!reflected)` → `ConsumeFood` + `Destroy`. ❌ **도시락이 거부했을 때 인벤토리를 소비하고 오브젝트를 파괴.**

`BentoModel.AddIngredient`는 MAIN/SIDE 타입만 수용(그 외 `false`). raw INGREDIENT를 도시락에 드롭하면 → `false` → `!reflected`=true → **재료가 인벤토리에서 사라지고 오브젝트 파괴**. 복붙 중 극성 반전이 남은 것.

**양쪽 브랜치가 다 깨져 있음** (2026-07 검토 반영):
- **거부 시**(현재 `!reflected`): 인벤토리 소비 + destroy → economy 손실.
- **수락 시**(현재 아무것도 안 함): `AddToCookingTool:206`은 성공 시 `ConsumeFood` + `position = defaultPosition`(제자리 복귀)인데, AddToBento 수락 브랜치엔 **소비도 복귀도 없음** → 도시락엔 담기나 인벤토리 안 줄고 드래그 sprite는 떨군 자리에 방치(중복 + 유령 sprite).

**→ 수정은 "1줄 flip"이 아니라 2줄**: `if (!reflected)` → `if (reflected)` + `AddToCookingTool`의 reposition 줄 복사(대칭). *성공 바디를 손으로 복붙하게 되는 것 자체가 두 메서드 통합(=IngredientPlacementService) 논증의 축소판.*

**검증-먼저 vs 수정-먼저 (병렬, 순서 종속 아님)**:
- **수정은 도달성과 무관하게 correct** — 양 브랜치 다 틀렸으므로 blind flip이 깰 "정상 케이스"가 없음. Wave 0에서 **독립 커밋**으로 즉시.
- **5분 도달성 체크는 correctness가 아니라 triage용**: (a) 우선순위(라이브=핫픽스 / 잠복=정규), (b) 기존 세이브·빌드에 이미 아이템 손실 피해가 발생했는지 판정.

---

## A. 접근이 전역 static — DI 이음새가 절반만 지어짐

### 문제
`GameSessionRoot`(GameSessionRoot.cs:21)는 17개 POCO 서비스를 생성·보관하는 **서비스 로케이터 겸 싱글톤**. 뷰들이 이걸 세 방식으로 만짐:
- **완전 주입(모범)**: CookingSceneManager.cs:28-30 — 씬 매니저가 CookingToolModel/FoodModel/Storage에 서비스 push.
- **반쪽 주입**: MallSceneController.cs:56은 BentoSelectionController엔 주입하나, 자신은 :44/:100/:116/:133에서 `GameSessionRoot.Instance` 직접 접근. 주입받은 CookingToolModel.cs:182조차 `SoundManager.Instance`로 회귀.
- **무주입**: Shop엔 씬 컨트롤러 부재. ShopUIAdapter가 `DontDestroyOnLoad` 싱글톤 자가생성, ShopDetailPanel.cs:124가 클릭마다 `.Instance`로 5개 서비스 회귀.

**의미**: "새 의존성을 Inject로 받을지 `.Instance`로 잡을지" 규칙 부재. "DI냐 싱글톤이냐"가 아니라 **이음새를 절반 짓고 방치**. 다음 개발자는 마지막에 본 패턴 복사 → 엔트로피 증가.

### 기존 코드 위험도: 🟡 MED (지금 안 터지나 복리 악화)
- `.Instance` 지점은 단위 테스트 불가 — 정확히 규칙이 사는 곳.
- `[DefaultExecutionOrder(-999)]`(GameSessionRoot.cs:20)로 Awake 순서 수동 관리 = 깨지기 쉬운 순서 해킹. 새 싱글톤이 먼저 Awake 시 `State` null NRE.
- 패턴 3종 병존 → 신규 코드 무작위 분기.

### 대안과 비용
| 대안 | 비용 | 평가 |
|---|---|---|
| ① 규칙 공식화: 도메인 서비스=Inject, `.Instance`=진짜 전역(SoundManager/GameSessionRoot 루트)만. 문서화 + Shop 씬 컨트롤러 신설 | 중 (Shop 컨트롤러 1 + ~15개소 정리) | **추천.** 불일치 제거 + 규칙 사는 곳 테스트성 |

> **원칙 완화 명시 (정직성)**: 추천안 ①은 완전 정리가 아니라 규칙 공식화 + 최소 정리다. 프로젝트 "부분적용 거부" 원칙은 게이트/패턴 100% 일치가 현실적인 항목의 원칙이며, **A는 완전 정리가 15+ 지점 × 상 비용이라 의도적으로 완화한다**. 즉 A에 한해 "부분적용 거부"는 적용하지 않는다 — 이 예외를 canonical에 기록해야 함.
| ② 전 뷰 완전 push-DI | 상 | 런타임 Instantiate 프리팹엔 주입 threading 필요, leaf 뷰 테스트 이득 미미 → 과잉 |
| ③ DI 컨테이너(VContainer/Zenject) | 매우 상 | 새 의존성·전면 재배선·학습곡선. 규모 대비 오버킬 |
| ④ 방치 | 0 | 위험 복리, 원칙 위반 유지 |

---

## B. UI를 코드로 통째 생성 — 경합의 핵심

### 문제 (두 극단 비교)
- **정당에 가까운 쪽 — ConfirmModal.BuildUI(ConfirmModal.cs:78)**: Canvas/Dim/Panel/VLG/버튼 60줄 생성 + ~15 리터럴(width 600, padding 32/32/28/28, font 36/26, 버튼 180×64). **색은 이미 `UIColors.PanelBg` 경유(:104)**. 전역 재사용 싱글톤이라 코드-생성=씬별 배선 불필요·자기완결. 게다가 프로젝트가 `Resources/`를 0으로 없앰(게이트) → 전역 싱글톤이 프리팹 핸들 잡을 쉬운 길이 없음 = **코드-생성의 진짜 이유**.
- **가장 나쁜 쪽 — ShopUIAdapter.SpawnBook(ShopUIAdapter.cs:39)**: ShopBook은 *프리팹* Instantiate(:54)하면서 Canvas 래퍼는 *코드* 생성(:44), 프리팹 내부를 `book.Find("Page/ShopPage/Page_L/Scroll/Viewport/Content")`(:57-68) 깊은 문자열 배선. `$"...BookMark_{(Tab)i}"`(:68)로 enum 이름=GameObject 이름 결합. 절반 프리팹/절반 코드로 일관성 붕괴.

### 기존 코드 위험도: 🟠 MED-HIGH (조용한 런타임 파손)
A보다 높음. `book.Find(...)`(:57-68)는 프리팹 reparent/rename 시 **컴파일 에러·OnValidate 없이 런타임 null** → 상점 조용히 죽음. `Tab` enum rename 시 북마크 조용히 미부착. 북마크 `sizeDelta` 직접 주입(:130)은 레이아웃과 충돌. ConfirmModal 쪽 위험은 낮음(자기완결·색 통일).

### 대안과 비용
| 대상 | 대안 | 비용 |
|---|---|---|
| ShopUIAdapter | Canvas를 ShopBook 프리팹에 포함 + `ShopBookRefs` 컴포넌트(직렬화 참조 + `Button[] bookmarks`) → `Find` 전량 제거 | 중 (Unity MCP/에디터 필수, .prefab 직접편집 금지) |
| ConfirmModal | `ConfirmModal.prefab`을 PrefabCatalog에 등록 → ShopBook과 동일 패턴 Instantiate | 하 (Resources 우회, 기존 패턴 재사용) |
| InteractPromptUI / MenuCardOverlay 닫기버튼 / DialogueManager Canvas | 프리팹화, `Build*` 삭제 | 중 (각 Unity 작업) |
| 방치 | — | 0 (조용한 파손 위험 유지) |

**최고 ROI 조각**: `transform.Find` 계층 문자열 전량 → `[SerializeField]` 배선(파손 위험 제거 + 디자이너 노출). 프리팹화는 Unity 작업이라 별도 phase.

---

## D. 규칙이 뷰에 살고, 드롭 규칙이 5곳에 복붙됨

### 문제 (FoodModel/CookingToolModel 정독)
"드롭 대상에 놓기" 규칙이 draggable View마다 재구현: `FoodModel.AddToCookingTool`/`AddToBento`, `CookingToolModel.AddToBento`/`TransferIngredient`, `BentoModel.AddIngredient` — **5메서드 동일 골격**(overlap 스캔 → null → addable/stock → AddIngredient → 조건부 consume/clear/destroy). MEMORY.md BaseStorage 리팩터가 잡은 중복 클래스인데 드래그-드롭 계층 미확장.

더 깊은 구조: FoodModel.cs:34-35, CookingToolModel.cs:47-50은 **여러 핸들러를 전부 `OnDragEnd`에 구독** → 드래그 끝마다 각 핸들러가 **독립적으로 콜라이더 스캔**. 드롭 *타깃*을 한 번 해석·디스패치해야 할 것을 N핸들러가 경쟁 스캔. 신규 타깃 추가 시 모든 draggable 편집 + 발화 순서 추론 필요.

부수:
- CookingToolModel.cs:62-65 `Update`가 매 프레임 `isCookable` 폴링(이벤트로 될 일).
- `OnMinigameEnd`(:212) cook(도메인)+sprite(뷰) 혼재.
- FoodModel이 80줄 convex-hull 수학 라이브러리(:92-171) 품음.
- `TransferIngredient`(:158) `ForEach(i => collision.AddIngredient(i))` — MEMORY.md storage 버그와 동일 idiom, 재발 취약.

### 기존 코드 위험도: 🔴 HIGH
1. **확정 극성 반전 버그**(상단) — 인벤토리 소비=경제 직결.
2. 다중-구독 `OnDragEnd` — 타깃 동시 겹침 시 발화 순서 의존 → 미래 버그 온상.
3. 쿠킹은 코어 게임플레이(45파일) → blast radius 최대.
4. `ForEach` idiom 재발 취약.

### 대안과 비용
| 대안 | 비용 | 평가 |
|---|---|---|
| ① `IngredientPlacementService`(POCO) 추출: `TryPlace(source, target) → 결과 enum`. 뷰는 `OnDragEnd` **한 번** 구독→타깃 해석→서비스 호출→결과 반응 | 중-상 (3-4파일 5메서드, 코어라 신중 테스트) | **구조적 정답.** 단일 원천 + 극성 버그 제거 + 신규 타깃 자명 + 테스트 가능 |
| ② 최소 수정: 극성 버그만 고치고 두 FoodModel 메서드 통합, 나머지 방치 | 하 (1파일) | 버그는 잡히나 구조 중복 잔존 |
| ③ 방치 | 0 | 경제 버그 잠복(HIGH) |

**권장**: ②를 Wave 0에서 즉시(버그 봉합) → ①을 Wave 3에서(구조 정리). 버그 수정과 구조 리팩터 분리로 위험 저감.

---

## C. 하드코딩 튜닝값/팔레트 — 두 "버그성" 항목은 등급이 다름 (2026-07 격상)

대량의 하드코딩 팔레트/수치(🟡 MED, 대안 단일: `[SerializeField]` + UIColors 경유, 비용 하)와 별개로, **"값이 무시/왜곡되는" 두 항목은 단순 스멜이 아니라 결함이며 서로 등급이 다르다**:

- **VisibleStateUtil.cs:10/18 — 🔴 상시 발현 결함 (도달성 걱정 없음).** `[Range(0,1)] public float minAlpha`(:10)를 `Awake()`(:18)가 무조건 `= 0.01f`로 덮어씀. **인스펙터 필드가 매 실행 100% 무효화되는 "거짓말 필드"** — D 극성 버그와 달리 도달성 조건도 없이 항상 틀림. 검증 완료(정독). 수정: :18 삭제(필드 이니셜라이저로) 또는 필드 제거. **1줄, Wave 0.**
- **StaminaGauge(LinearGauge) 40/15 — 🟡 잠복 결합 위험 (상시 결함 아님).** 지금은 정상 작동. `maxValue`(직렬화됨)를 바꾸는 순간에만 임계 비율이 깨짐. 즉 미래 튜닝 시 조용히 깨질 함정이지 현재 버그 아님. 수정: `[SerializeField] int lowThreshold/severeThreshold` 또는 `[Range(0,1)]` 분수. **~6줄, Wave 0~1.**

## E. 네이밍
🟢 LOW지만 IDE/MCP 내비 함정. 파일명↔클래스명(MoneyUI=SmoothMoneyText, StaminaGauge=LinearGauge) rename 비용 하, `*Model`→`*View` 대량 rename 중(churn) → Wave 3.

---

## 위험도 종합

| 문제 | 위험도 | 성격 | 즉시성 |
|---|---|---|---|
| D 극성 반전 버그 | 🔴 HIGH | 확정 로직 결함(경제) | **Wave 0 즉시(독립 커밋)** |
| VisibleStateUtil clobber | 🔴 상시 | 인스펙터 필드 100% 무효(거짓말 필드) | **Wave 0** |
| B `transform.Find` 문자열 | 🟠 MED-HIGH | 조용한 런타임 파손 | Wave 1 |
| D 드롭규칙 중복/다중구독 | 🟠 MED-HIGH | 미래 버그 온상, 코어 | Wave 3 |
| A DI 절반 이음새 | 🟡 MED | 테스트성·엔트로피 복리 | Wave 2 |
| StaminaGauge 40/15 | 🟡 잠복 | max 변경 시에만 깨짐(현재 정상) | Wave 0~1 |
| B 코드-생성 UI(ConfirmModal류) | 🟢 LOW-MED | 디자이너 차단(자기완결은 정당) | Wave 1 |
| C 하드코딩 값/팔레트 | 🟡 MED | 규칙 위반, 튜닝 불가 | Wave 1 |
| E 네이밍 | 🟢 LOW | 내비 함정 | Wave 3 |

---

## 비용 정량 (2026-07 추가)

> LOC는 근사. **Unity 에디터 배선 시간은 LOC에 안 잡힘** — 별도 표기.

| 작업 | 코드 LOC | 에디터 작업 | PR | 실비용 드라이버 |
|---|---|---|---|---|
| 극성 flip + reposition | ~2 | 없음 | 1 (독립) | 도달성 5분 triage |
| VisibleStateUtil clobber 제거 | ~1 | 없음 | 1 | — |
| StaminaGauge 임계 직렬화 | ~6 | 인스펙터 2필드 wire | 1 | — |
| `transform.Find`→SerializeField (ShopUIAdapter) | +25 신규 `ShopBookRefs` / −12 Find | **프리팹 참조 ~12개 드래그** | 1 | **에디터 배선** |
| `transform.Find` (MenuCardController 9개 등) | 파일당 ~+15/−15 | 파일당 참조 wire | 조각 단위 | 에디터 배선 |
| A DI 규칙 + Shop 컨트롤러 | 신규 ~50-70 + ~15 콜사이트 | 최소 | 1-2 | **규칙 결정·문서화** |
| IngredientPlacementService 추출 | 신규 ~100-150 / 뷰 net −50 | 없음 | 1 | **Playmode 회귀 테스트(테스트 부재)** |

**핵심**: IngredientPlacementService의 진짜 상 비용은 코드 이동이 아니라 **자동 회귀 테스트 부재 하의 수동 검증**. 선행 조건 = 최소 쿠킹 플로우 수동 회귀 체크리스트.

---

## Wave 재정렬 (2026-07 검토 반영)

| Wave | 내용 |
|---|---|
| **0 (즉시)** | 극성 버그 도달성 확인 → flip+reposition(**독립 커밋**). VisibleStateUtil clobber 제거. StaminaGauge 40/15 직렬화. |
| **1 (근접)** | `transform.Find`→SerializeField (조각 단위, **ShopUIAdapter 우선**). C 하드코딩 팔레트→UIColors/SerializeField. ConfirmModal류 프리팹화. |
| **2** | A DI 규칙 공식화 + Shop 씬 컨트롤러 (["부분적용 거부" 완화 명시](#a-접근이-전역-static--di-이음새가-절반만-지어짐)). |
| **3** | IngredientPlacementService 추출(수동 회귀 선행), FoodModel 분해, 네이밍 정리. |

---

## 미결 (다음 확인)
- **극성 버그 도달성**: raw FoodModel이 현재 씬에서 BentoModel과 겹칠 수 있는지 씬/콜라이더 검증 → HIGH가 라이브인지 잠복인지 확정.
- diagnosis_status.md에 위험도 테이블 + 확정 버그 반영.
- 미디깅 SRP 대상(DialogueManager 입력 상태머신, TimeManager 이중 스케줄러)은 동일 깊이 후속 가능.
