# Core Loop — 페이즈 구조와 하루 흐름

## 개요

Aftertaste의 게임 시간은 **Day 단위**로 진행되며, 각 Day는 **5개 페이즈 + 정산 씬**으로 구성된다.

한 하루의 표준 흐름:

```
Day N Preparation → Morning → Afternoon → Evening → Night → Settlement → Day N+1 Preparation
```

## 페이즈 정의 (`PhaseType` enum)

파일: [`Assets/Scripts/Schema/Config/Common/PhaseType.cs`](../../../Assets/Scripts/Schema/Config/Common/PhaseType.cs)

| Enum | 값 | 시작 시각 | 위치 | 유저 행동 |
|---|:-:|:-:|---|---|
| `Preparation` | 0 | 05:00 | Mall | 도시락 3슬롯 메뉴 선택 → 확정 시 Cooking 진입 |
| `Morning` | 1 | 07:00 | Cooking | **자동 영업 (선택 UI 없음)** |
| `Afternoon` | 2 | 12:00 | Mall | 3택: **영업(Work) / 휴식(Rest) / 상가(Shopping)** |
| `Evening` | 3 | 17:00 | Mall | 위와 동일 |
| `Night` | 4 | 22:00 | Mall | 위와 동일 |

`PhaseType`은 게임 상태의 SSOT(Single Source of Truth). `PhaseData.Phase` 필드에 저장된다.

## 페이즈 전환 로직 (`ProgressService`)

파일: [`Assets/Scripts/Unity/Common/ProgressService.cs`](../../../Assets/Scripts/Unity/Common/ProgressService.cs)

### `PassPhase()`
- 현재 페이즈가 `Night`가 아니면 → `Phase++`, 시간 재설정, `OnPhaseChanged` 이벤트
- `Night`면 → `SceneLoader.LoadScene(Settlement)` 로드 후 `true` 반환 (호출자에게 씬 전환 알림)

### `PassDay()` (Settlement에서 호출)
`SettlementController.SaveRoutineAsync` → `Progress.PassDay()`:

1. `stats.SubMoney(ManagementFee=1000)` — 관리비 차감
2. `pd.Day++`
3. `pd.Phase = Preparation` — 다음 날 시작
4. `_cumulativePhaseIndex++`
5. `SetPhaseTime(Preparation)` — 시각 05:00으로 리셋
6. `OnPhaseChanged.Invoke(Preparation)` — 구독자에게 알림
7. `stats.SetStamina(100)` — 체력 회복
8. `GameRandom.InitDay(pd.Day)` — 그날의 시드 초기화
9. `Inventory.AdvanceDay()` — 유통기한 -1, 만료 재고 폐기
10. `Weather.UpdateWeather(pd.Day)` — 날씨 갱신
11. `Settlement.Reset(money)` — 수입/지출 카운터 초기화
12. `SaveManager.SaveAll()` — 자동 저장

### `PhaseChanged` 이벤트 구독자

| 구독자 | 반응 |
|---|---|
| `SoundManager.OnPhaseChanged` | BGM 재계산 (`UpdateBGM`) |
| `Farm.OnPhaseChangedHandler` | `OnTimePassed` — 작물 성장 tick |
| `CookingBackgroundController.ApplyPhase` | 시간대별 배경 이미지 교체 (morning/afternoon/evening/night) |
| `MallSceneController.OnPhaseChangedInMall` | Mall 체류 중 페이즈 전환 시 ActionSelector 자동 표시 (Preparation/Morning 제외) |

## 씬 전환 매핑

| 페이즈 진입 시 | 씬 |
|---|---|
| Preparation | Mall (BentoSelection UI 오버레이) |
| Morning | Cooking (Preparation 확정 시 자동 로드) |
| Afternoon | Mall (Cooking에서 복귀 후) |
| Evening | Mall |
| Night | Mall |
| Night → Settlement | Settlement (`PassPhase` 자동 로드) |
| Settlement → Preparation | Mall (SettlementController가 "아무 키" 입력 시 로드) |

메뉴 확정 → Cooking 자동 진입은 [`MallSceneController.OpenMenuSelection`](../../../Assets/Scripts/Unity/Common/MallSceneController.cs) 흐름 참조.

## Afternoon/Evening/Night의 3택 액션

Mall에서 `PhaseActionSelector` UI로 선택 (파일: [`Assets/Scripts/Unity/UI/PhaseActionSelector.cs`](../../../Assets/Scripts/Unity/UI/PhaseActionSelector.cs)):

| 액션 | 결과 |
|---|---|
| **Work (영업)** | `SceneLoader.LoadScene(Cooking)` — 요리 씬 진입 |
| **Rest (휴식)** | Stamina 회복 + `PassPhase()` (별도 씬 없이 페이즈 진행) |
| **Shopping (상가)** | `SceneLoader.LoadScene(Shop)` — 상점 씬 진입 |

또한 Mall 씬 내에서 자유롭게 걸어다니며 NPC 대화, Garden(텃밭) 진입, 상점 다른 시설 접근 가능. Shop/Garden에서 Mall 복귀 시 ActionSelector가 다시 뜨지 않음 (이번 세션 fix).

## 시간(분) 시스템

`StatsService.SetTime(hour, minute)`으로 관리. 총 하루 = 1440분 (24시간).

### 페이즈 시작/종료 시각 (`ProgressService.PhaseToStartMinutes` / `PhaseToEndMinutes`)

| 페이즈 | 시작 (분) | 종료 (분) | 지속시간 |
|---|:-:|:-:|:-:|
| Preparation | 300 (05:00) | 420 (07:00) | 2시간 |
| Morning | 420 (07:00) | 720 (12:00) | 5시간 |
| Afternoon | 720 (12:00) | 1020 (17:00) | 5시간 |
| Evening | 1020 (17:00) | 1320 (22:00) | 5시간 |
| Night | 1320 (22:00) | 1440 (24:00) | 2시간 |

## 자원 순환

### 매일 사이클

```
[Preparation]
   ↓ 메뉴 선택 (Main + Side 조합)
[Cooking (Morning)]
   ↓ 재료 소비 → 요리 완성 → 손님 서빙 → 매출 +
[Mall (Afternoon)]
   ↓ 액션 선택
   ├─ Work → Cooking (매출 +)
   ├─ Rest → Stamina +
   └─ Shopping → Shop (재료 매입 = 지출)
[Evening / Night]
   ↓ (Afternoon과 동일 흐름)
[Settlement]
   ↓ 관리비 -1000G, 수입/지출 정산, 저장
[Day+1 Preparation]
```

### 장기 자원

| 자원 | 획득 | 소비 |
|---|---|---|
| **Money** | 요리 서빙, 배달 완료, 텃밭 수확 판매 (있다면) | 관리비, 재료 매입, 업그레이드 코스트 |
| **Stamina** | Rest, PassDay (100 리셋) | 요리, 이동 등 |
| **재료 (Inventory)** | Shop 매입, Garden 수확 | 요리 소비, 유통기한 만료 |
| **레시피** | Day별/조건별 언락 (`UnlockedFoodService`) | — |
| **업그레이드 진행도** | 코스트 지불 | — |

## 하루의 예시 시나리오

**Day 3 — 아침**
- Preparation 05:00: 어제 산 재료로 도시락 슬롯 1(옥상 오믈렛 + 루미 젤리), 슬롯 2(스트리트 스테이크 49), 슬롯 3(비움) 선택
- Morning 07:00: Cooking 씬. 손님 5명 처리. 슬롯 1 3회, 슬롯 2 2회 서빙. 매출 +18,000G

**낮 · 저녁**
- Afternoon 12:00: Mall 복귀 → 3택 → Shopping 선택 → Shop에서 재료 매입 6,000G
- Evening 17:00: Work 선택 → Cooking 재영업 → 매출 +12,000G
- Night 22:00: Rest 선택 → Stamina 회복

**정산**
- Settlement: 수입 30,000G / 지출 (재료 6,000G + 관리비 1,000G) → 순이익 +23,000G → Day 4 Preparation

## 종료/실패 상태

- **파산**: 현재 미구현. `SubMoney`는 `Mathf.Max(0)`로 클램프 → Money 0에서 stagnate하지만 게임 오버 트리거 없음. 관련 상세: [`settlement-system.md`](../systems/settlement-system.md).
- **엔딩 조건**: 현재 명시적 엔딩 없음. Day 60~90에 최종 업그레이드 완료가 소프트 목표 (밸런싱 세션 기준).

## 참조

- 페이즈 세부 전환 로직: [`systems/time-phase-system.md`](../systems/time-phase-system.md)
- 페이즈별 손님 스폰 밸런스: [`systems/customer-system.md`](../systems/customer-system.md)
- 페이즈 별 UI 흐름: [`systems/mall-system.md`](../systems/mall-system.md)
- 정산 상세: [`systems/settlement-system.md`](../systems/settlement-system.md)
- 저장 흐름: [`systems/save-system.md`](../systems/save-system.md)
