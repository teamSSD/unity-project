# ADR-008 — Dependency Injection 경계 규칙

_상태: **ACCEPTED (2026-07)**_
_원천: [reports/review_2026-07_deepdig.md](../reports/review_2026-07_deepdig.md) 근본원인 A_

## 배경

2026-05 리팩터링 후에도 `GameSessionRoot`(17 POCO 서비스 보관)를 통한 뷰 접근이 **세 방식으로 병존**:

1. **완전 주입 (모범)** — CookingSceneManager: 씬 매니저가 자식 뷰에 서비스 push.
2. **반쪽 주입** — MallSceneController: 자식엔 주입하지만 스스로는 `GameSessionRoot.Instance` 직접 접근.
3. **무주입** — Shop: 씬 컨트롤러 부재, ShopUIAdapter가 `DontDestroyOnLoad` 싱글톤 자가생성, ShopDetailPanel이 클릭마다 5개 서비스 `.Instance` 회귀.

**의미**: "새 의존성을 Inject로 받을지 `.Instance`로 잡을지" **규칙이 없어서** 다음 개발자가 마지막에 본 패턴을 복사 → 엔트로피 증가.

## 결정

**도메인 서비스는 Inject, `.Instance`는 진짜 전역만.**

### `.Instance` 허용 대상 (whitelist)
- `GameSessionRoot.Instance` — 최상위 서비스 로케이터 자체. 씬 컨트롤러/부트스트랩만 여기서 서비스 fetch.
- `SoundManager.Instance` — 프로젝트 어디서든 부수효과처럼 부르는 오디오 파사드. 뷰가 직접 호출 정당.

### `.Instance` 금지 대상 (blacklist)
- `GameSessionRoot`의 하위 서비스(`Purchase`, `Stats`, `Progress`, `Inventory`, `MenuSelection`, `ToolUpgrade`, `StorageUpgrade`, `FarmUpgrade`, `Weather`, `Settlement`, `Order`, `RecipeLookup`, `UnlockedFood`, `FarmUpgrade`, ...) — **주입받아야 함**.
- 다른 뷰 매니저(`ShopUIAdapter`, `MenuCardController` 등)의 `.Instance` — 뷰-뷰 협업은 **이벤트나 명시 참조**로. `.Instance` 회귀 금지.

### 주입 채널
- **씬 컨트롤러** (`CookingSceneManager`, `MallSceneController`, **`ShopSceneController` 신설**)가 유일한 Composition Root.
- `Start()`에서 `GameSessionRoot.Instance`로 서비스 fetch → 하위 뷰에 `Inject(...)` 호출.
- 런타임 Instantiate된 프리팹은 인스턴스 직후 부모가 `Inject` push (bento/row 등).

### 부수 규칙
- **캐싱 우선**: 동일 서비스를 여러 메서드에서 쓰면 필드로 캐시. `.Instance` 반복 금지.
- **null-safe**: 주입받은 필드는 `?.` 접근으로 부트스트랩 순서 문제 회피 가능.
- **테스트성**: Inject 진입점이 있어야 EditMode 테스트에서 mock 주입 가능.

## "부분적용 거부" 원칙과의 관계

프로젝트는 **"게이트 0/패턴 100% 일치"**를 원칙으로 하나, 본 ADR은 **A에 한해 완화**한다:
- 완전 정리는 15+ 지점 × 상 비용이라 ROI 낮음.
- 규칙 공식화 + 새 코드는 규칙 준수 + 눈에 띄는 오래된 지점만 즉시 정리 = 실용적 절충.
- 신규 뷰/서비스는 예외 없이 Inject 패턴 준수.

## 즉시 정리 대상 (Wave 2 스코프)

| 대상 | 현재 | 변경 |
|---|---|---|
| `ShopSceneController` | Shop 씬에 씬 컨트롤러 없음 | **신설**하거나 `ShopUIAdapter`가 명시 Cache/Inject 담당 |
| `ShopUIAdapter.Cache*` | `PopulateItemList` 등에서 매번 `GameSessionRoot.Instance?.Purchase` | 서비스 5개(`Purchase`, `Stats`, `ToolUpgrade`, `StorageUpgrade`, `FarmUpgrade`)를 `OpenShop`/`SpawnBook`에서 필드로 캐시 |
| `ShopDetailPanel` | 클릭마다 `.Instance` × 5 | `Inject(Purchase, ToolUpgrade, StorageUpgrade, FarmUpgrade)` — `ShopUIAdapter`가 spawn 시 호출 |

## 대안 검토

- **② 전 뷰 완전 push-DI**: 런타임 Instantiate 프리팹에 주입 threading 필요, leaf 뷰 테스트 이득 미미 → 과잉.
- **③ DI 컨테이너(VContainer/Zenject)**: 새 의존성·전면 재배선·학습곡선. 규모 대비 오버킬. **거부**.
- **④ 방치**: 위험 복리, 원칙 위반 유지. **거부**.

## 후속 (본 ADR 이후)

- 신규 뷰/서비스 리뷰 시 이 규칙을 기준으로 판단.
- Wave 3의 `IngredientPlacementService` 추출 시 뷰가 서비스를 **주입 채널을 통해서만** 참조.
- `.Instance` 검색(gate)에 whitelist 필터 유지 — 잔여 13 MonoBehaviour는 wontfix 정당화(원 진단 #1).
