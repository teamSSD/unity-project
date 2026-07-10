# Weather 시스템

## 개요

**날씨 시스템**은 하루 단위로 나쁜 날씨(`IsBadWeather`)를 판정하는 서비스. 현재는 **NPC 일상 대사 필터링**과 **시뮬레이션 로그 스냅샷**의 두 곳에서만 참조되며, 실제 게임 밸런스(손님 수, 매출 등)에는 반영되지 않은 **사장(死藏) 상태**의 시스템이다.

핵심 서비스: [`WeatherService.cs`](../../../Assets/Scripts/Domain/Common/WeatherService.cs) (POCO Service, `GameSessionRoot.Weather`)

## WeatherService — 전체 코드

```csharp
namespace Game.Domain.Common
{
    public class WeatherService
    {
        private const float BadWeatherChance = 0.4f;

        public bool IsBadWeather { get; private set; }

        public void UpdateWeather(int day)
        {
            IsBadWeather = GameRandom.Value(GameRandom.Immutable) < BadWeatherChance;
        }
    }
}
```

전체 22줄. 상태는 단 하나의 bool (`IsBadWeather`), 파생 상태 없음.

### 상수 / 값

| 이름 | 값 | 의미 |
|---|---|---|
| `BadWeatherChance` | `0.4f` | 나쁜 날씨 확률 40% |

### `UpdateWeather(day)`
- `GameRandom.Immutable` 시퀀스에서 `Value` (0~1) 뽑아 0.4 미만이면 `IsBadWeather = true`
- **주의**: 파라미터 `day`는 현재 미사용 (시그니처만 존재). 호출자가 미리 `GameRandom.InitDay(day)`를 부른 상태여야 결정적 재현 가능
- `GameRandom.Immutable`은 게임 상태와 무관하게 시드로부터 결정되는 시퀀스

## UpdateWeather 호출 지점

파일: [`ProgressService.cs`](../../../Assets/Scripts/Unity/Common/ProgressService.cs), [`GameStart.cs`](../../../Assets/Scripts/Unity/Common/GameStart.cs)

| 호출자 | 시점 | 코드 위치 |
|---|---|---|
| `ProgressService.PassDay()` | 매일 시작 (Settlement 확인 후) | ProgressService.cs:85 |
| `GameStart` (신규 게임) | 튜토리얼 스킵 후 day=0 초기화 | GameStart.cs:63 |
| `GameStart` (신규 게임) | Day 0 로드 시점 | GameStart.cs:83 |

즉 새 하루 시작 시점에 정확히 1회 갱신, `IsBadWeather`는 그 하루 동안 유지.

## 사용처 (실 게임 반영)

`IsBadWeather`를 참조하는 코드 전수:

### 1. NPC 일상 대사 필터링

파일: [`CasualDialogueProvider.cs`](../../../Assets/Scripts/Unity/Mall/CasualDialogueProvider.cs#L56)

```csharp
bool badWeather = GameSessionRoot.Instance?.Weather?.IsBadWeather ?? false;

var candidates = lines.Where(l =>
    l.condition == "Any" ||
    (badWeather && l.condition == "Bad") ||
    (!badWeather && l.condition == "Good")
).ToList();
```

- `npcCasualDialogue.csv`의 `Condition` 컬럼 값 (`Any` / `Good` / `Bad`) 기반 필터
- Good 날씨: `Any` + `Good` 조건 대사만 노출
- Bad 날씨: `Any` + `Bad` 조건 대사만 노출

**날씨별 대사가 있는 NPC** (`npcCasualDialogue.csv` 전수):

| NpcId | Good 대사 유무 | Bad 대사 유무 | Any 대사 |
|---|:-:|:-:|:-:|
| npc_getoro | 있음 | 있음 | — |
| npc_jar | — | — | 2건 |
| npc_sranya | — | — | 1건 |
| npc_pajama | 있음 | 있음 | — |
| npc_moai | — | — | 1건 |
| npc_nimo | — | — | 1건 |
| npc_lede | — | — | 1건 |
| npc_gabriel | — | — | 2건 |

→ **날씨 조건 대사를 가진 NPC는 게토로와 파자마 2명뿐**. 나머지는 `Any`로만 등록되어 있어 사실상 날씨 영향 없음.

### 2. 시뮬레이션 로그 스냅샷

파일: [`SimHarness.cs`](../../../Assets/Editor/Simulation/SimHarness.cs#L295)

```csharp
_ctx.Log.Add(new DayEndSnapshotEvent
{
    ...
    badWeather = _ctx.Weather.IsBadWeather,
    ...
});
```

- Editor 전용 시뮬레이션 하네스에서 하루 종료 스냅샷의 필드로 기록
- 밸런싱 분석용 로그, 게임 플레이에는 영향 없음

### 3. Editor Test

파일: [`WeatherSystemTest.cs`](../../../Assets/Tests/EditMode/Systems/WeatherSystemTest.cs)

- `IsBadWeather` 값이 결정적이고 확률 분포가 대략 40%인지 검증하는 유닛 테스트만 존재

## 밸런스에 미반영된 항목 (현재 사장 상태)

다음 항목은 날씨와 **연결되지 않았지만 자연스럽게 예상되는** 결합점. 향후 결합 여지:

| 잠재 결합 대상 | 현재 코드 | 코멘트 |
|---|---|---|
| 손님 수 / 스폰 | `CustomerSpawner`가 `IsBadWeather` 미참조 | 나쁜 날씨 시 손님 감소 등 미구현 |
| 매출 / 팁 | `SettlementService` 미참조 | — |
| 텃밭 성장 | `FarmTile.IsHarvestable` 미참조 | — |
| BGM / SFX | `SoundManager` 미참조 | Rainy 앰비언스 등 미구현 |
| VFX (비 파티클) | 어떤 씬에도 파티클/셰이더 훅 없음 | — |
| 미니게임 난이도 | `MinigameContext` 미참조 | — |

## 재현성 / 시드

- `WeatherService.UpdateWeather`는 `GameRandom.Immutable` 시퀀스 사용
  - 정의: [`GameRandom.cs`](../../../Assets/Scripts/Unity/Common/GameRandom.cs)
  - `Immutable`은 게임 상태 변화와 무관한 재현 가능 시퀀스
- 호출 전에 `GameRandom.InitDay(day)`를 부르는 것이 컨벤션 (실제 `ProgressService.PassDay`에서 preceding line에 존재)
- 즉 같은 day에서 시작하면 같은 날씨가 나오는 결정적 시스템

## 향후 확장 시 후크 포인트

- `WeatherService`에 상태 다중화 (Sunny/Rainy/Storm 등) 시 `enum WeatherType` 추가 자연스러움
- `UpdateWeather(day)` 시그니처에 이미 `day` 파라미터가 있으므로 day별 다른 확률/편향 도입 여지
- 이벤트 발행 없음 — 리스너 필요 시 `OnWeatherChanged` 추가 필요

## 관련 문서

- 호출 컨텍스트 (PassDay 흐름): [`../overview/core-loop.md`](../overview/core-loop.md)
- NPC 대사 시스템: [`mall-system.md`](mall-system.md#NPC-배치--상호작용)
- GameRandom / 시드 재현성: [`save-system.md`](save-system.md) (있다면)
