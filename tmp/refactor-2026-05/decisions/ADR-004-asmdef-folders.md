# ADR-004: Asmdef / Folder Structure

**상태**: **ACCEPTED** (2026-05-28)  
**카테고리**: Blocking Phase 2 (ADR-002와 연동)

## TL;DR (v2 결정 요약)

3 asmdef (Schema → Domain → Unity) + Editor + EditMode 테스트 + **PlayMode 테스트 신설**.
- Schema asmdef: Config(SO) + State(POCO) sub-folder 분리
- Domain asmdef: UnityEngine 참조 허용 (Vector3 등 실용)
- 마이그레이션은 **Phase 2-A 단일 PR** — 193 파일 일괄 이동
- 마이그레이션 PR 진행 중 다른 모든 작업 일시 중단

## Context

현재:
- `Game.Runtime.asmdef` **1개**가 모든 런타임 코드 포함 (15,226라인)
- `Tests.EditMode.asmdef` **1개**가 테스트 (1,016라인)
- 폴더 구조 혼재:
  - `Managers/` (역할 기반)
  - `Controllers/` (역할 기반)
  - `Systems/` (역할 기반)
  - `Entities/Cooking/`, `Entities/Mall/`, `Entities/Garden/`, ... (feature 기반)
  - `UI/`, `Utilities/`, `Interfaces/` (역할 기반)
- 모순 사례: `Entities/Mall/delivery/OrderManager.cs` — Manager가 Entity 폴더에 위치

문제:
- 단일 어셈블리 → 어떤 파일 수정해도 전체 리컴파일 (15K라인 리빌드)
- 레이어 강제 메커니즘 부재 (3-Layer 위반이 코드 컨벤션에만 의존)
- 폴더에 따라 "여기 둬도 되나?" 망설임이 발생

## Options

### A. 현 상태 유지
- 단순. 변경 비용 없음.
- 그러나 레이어 강제 불가, 컴파일 시간 그대로

### B. Layer-First Asmdef (3개) (★ 추천)
```
Game.Schema.asmdef    (DTO, enum, ScriptableObject 정의)
Game.Domain.asmdef    (순수 C# 로직 — ref Schema)
Game.Unity.asmdef     (MonoBehaviour, scene wiring — ref Schema, Domain)
Game.Editor.asmdef    (에디터 도구)
Tests.EditMode.asmdef (ref Schema, Domain)
Tests.PlayMode.asmdef (신설 — ref Schema, Domain, Unity)
```
- 폴더는 asmdef 내부에서 feature-first로 정리
- **장**: 3-Layer 컴파일 강제, 컴파일 시간 단축 (Schema/Domain만 수정 시 Unity 리빌드 안 함)
- **단**: 모든 파일 이동 작업, using/namespace 일부 수정

### C. Feature-First Asmdef (4-5개)
```
Game.Common.asmdef    (공통 + 매니저 진입점)
Game.Cooking.asmdef   (쿠킹 씬 전체)
Game.Shop.asmdef      (상점 전체)
Game.Garden.asmdef    (정원 전체)
Game.Mall.asmdef      (몰 허브)
```
- **장**: feature 단위 개발/테스트 격리 가능
- **단**: cross-feature 의존성 정리 부담, 레이어 강제 못 함 (각 asmdef 안에서 모든 레이어 혼재)

### D. Hybrid (Layer × Feature 2차원)
```
Game.Cooking.Schema, Game.Cooking.Domain, Game.Cooking.Unity (× 4-5 feature)
```
- 12-15개 asmdef
- **장**: 가장 엄격한 격리
- **단**: 관리 폭증, 작은 팀에 과함

## Recommendation: B (Layer-First, 3 asmdef + Editor + 2 Tests)

이유:
- ADR-002의 3-Layer 강제 필요 → 어셈블리 분리가 유일한 컴파일 강제 메커니즘
- 이 규모(15K라인, 1인 팀 추정)에 D는 과함
- C는 cross-feature 결합이 많은 게임 도메인엔 마찰 큼
- B가 트레이드오프 최적: 레이어 강제 + 폴더는 feature-first

### 구체 설계

#### asmdef 의존 그래프
```
Game.Schema  (ref: none, allow UnityEngine for ScriptableObject)
   ↑
Game.Domain  (ref: Schema)
   ↑
Game.Unity   (ref: Schema, Domain, UnityEngine.UI)
   
Game.Editor  (ref: Schema, Domain, Unity)
Tests.EditMode  (ref: Schema, Domain)
Tests.PlayMode  (ref: Schema, Domain, Unity)
```

#### 폴더 구조

```
Assets/Scripts/
├── Schema/
│   ├── Game.Schema.asmdef
│   ├── Common/        (PersistentData, GameStateDto, enum)
│   ├── Cooking/       (FoodData SO, RecipeData SO, CookingToolData SO)
│   ├── Garden/        (CropData SO, FarmTileData SO)
│   ├── Shop/          (ShopConfig SO, UpgradeTable SO)
│   ├── Mall/          (DialogueData, NpcData)
│   └── Events/        (GameEvent SOs)
│
├── Domain/
│   ├── Game.Domain.asmdef
│   ├── Common/        (GameState, SaveLoad logic, GameRandom)
│   ├── Cooking/       (CookingService, RecipeChain, MenuValidator logic)
│   ├── Garden/        (FarmService)
│   ├── Shop/          (UpgradeService, BuyService)
│   └── Mall/          (DialogueService)
│
├── Unity/
│   ├── Game.Unity.asmdef
│   ├── Common/        (GameSessionRoot, SceneCoordinator, BootLoader)
│   ├── Cooking/       (CookingSceneController, Refrigerator, ...)
│   ├── Garden/        (GardenSceneController, FarmTile, ...)
│   ├── Shop/          (ShopSceneController, ShopDetailPanel, ...)
│   ├── Mall/          (MallSceneController, PlayerMove, ...)
│   └── UI/            (HUD, MenuCard, Dialogue UI, Settings)
│
├── Editor/
│   └── Game.Editor.asmdef
│
└── Tests/
    ├── EditMode/
    │   └── Tests.EditMode.asmdef
    └── PlayMode/        (신설)
        └── Tests.PlayMode.asmdef
```

#### 마이그레이션 매핑 예시

| 현재 위치 | 이동 대상 |
|---|---|
| `Assets/Scripts/Entities/Cooking/cooking/dataType/FoodData.cs` | `Schema/Cooking/FoodData.cs` |
| `Assets/Scripts/Entities/Cooking/cooking/Food/FoodModel.cs` (258라인 MonoBehaviour) | `Unity/Cooking/FoodBehavior.cs` (rename) + `Domain/Cooking/FoodLogic.cs` (분리) |
| `Assets/Scripts/Managers/InventoryManager.cs` (Singleton MB) | 점진: `Unity/Common/InventoryAdapter.cs` + `Domain/Common/InventoryLogic.cs` |
| `Assets/Scripts/Entities/Mall/delivery/OrderManager.cs` | `Unity/Mall/OrderAdapter.cs` + `Domain/Mall/OrderLogic.cs` (모순 해결) |
| `Assets/Scripts/Utilities/SaveManager.cs` | `Domain/Common/PersistenceService.cs` + `Unity/Common/SaveBootstrapper.cs` |
| `Assets/Scripts/Interfaces/Cooking/*.cs` | `Schema/Cooking/Interfaces/` |
| `Assets/Scripts/Systems/Commons/*.cs` | `Domain/Common/` 또는 `Unity/Common/` (각 분류) |

#### 컴파일 강제 효과

ADR-002의 Hybrid Layer Architecture가 실제 강제됨:
- Schema 클래스가 Domain 타입 참조 → 컴파일 에러 (Schema asmdef는 Domain ref 없음)
- Domain 클래스가 MonoBehaviour 상속 → 컴파일 에러 (Domain asmdef는 UnityEngine 참조하지만 MonoBehaviour는 Unity asmdef에서만 정의되는 라이프사이클)
  - 정확히는: Domain asmdef도 UnityEngine 의존은 가능 (Vector3 등), MonoBehaviour 상속 자체는 가능하나 폴더 컨벤션으로 금지
  - 더 엄격하려면: Domain asmdef에 `noEngineReferences: true` → UnityEngine 자체 사용 불가 (트레이드오프 큼)
- Unity 클래스가 자기 변경한 게 Schema/Domain에 영향 없음 → 부분 리컴파일

## Consequences

### 긍정
- 3-Layer 컴파일 강제
- 컴파일 시간 단축 (특히 Unity 레이어 수정 시)
- 폴더 일관성 회복 (모순 사례 정리)
- 테스트가 Unity 없이 Schema/Domain 단위 가능 (PlayMode 별도)
- 새 파일 작성 시 "여기 둬도 되나" 망설임 감소

### 부정
- **모든 .cs 파일 이동 작업** (193 파일)
- 일부 파일은 분리됨 (Model → Schema + Domain + Unity로 쪼개짐)
- namespace 변경 시 모든 using 문 영향
- Git history 추적이 일부 깨질 수 있음 (rename 인식 한계)
- Unity의 .meta 파일도 함께 이동 (GUID 보존 필수)
- 마이그레이션 중간 단계 빌드 깨질 가능성 → 한 번의 큰 PR이 안전

### 마이그레이션 전략
1. 새 asmdef 파일 4개 생성 (빈 폴더 + asmdef + meta)
2. 자동화 스크립트로 파일 이동 (`git mv` 보존)
3. 매핑 표 기반 일괄 이동
4. 컴파일 시도 → 위반 잡힘 → 일괄 수정
5. 위반 해결 후 PR
6. **추정 작업**: 1-2주 (전담 작업, 다른 변경 없이)

#### 위험 완화
- 마이그레이션 PR은 다른 모든 작업을 일시 중단 (충돌 방지)
- 마이그레이션 중 새 기능 개발 금지
- 마이그레이션 전후 베이스라인 비교 → 회귀 0 검증

## Open Questions

1. **Domain asmdef가 UnityEngine을 참조할 수 있는가**?
   - 허용: 실용 (Vector3 사용)
   - 금지 (noEngineReferences): 가장 엄격, 그러나 Vector3 등 못 씀 → 자체 Vec3 정의 필요
   - **임시 추천**: 허용 (실용)

2. **Schema asmdef는 UnityEngine 참조 허용 여부**?
   - 허용 (필수): ScriptableObject 상속 필요
   - **결정**: 허용

3. **Interfaces 폴더 처리**?
   - 현재: `Assets/Scripts/Interfaces/` (별도 폴더)
   - 옵션 A: Schema에 흡수 (`Schema/Cooking/Interfaces/`)
   - 옵션 B: 각 feature 폴더에 분산
   - **임시 추천**: Schema에 흡수, feature별 정리

4. **MonoBehaviour 상속 클래스가 Schema 폴더에 있어도 되는가**?
   - No: Schema는 데이터 정의만
   - **결정**: No (모든 MonoBehaviour는 Unity 폴더)

5. **PlayMode 테스트 인프라 신설 시점**?
   - 마이그레이션과 함께 신설
   - 또는 마이그레이션 후 Phase 2 작업으로
   - **임시 추천**: 마이그레이션과 함께 빈 asmdef 생성 (실 테스트는 Phase 2+)

## Decision Required

- [ ] B (Layer-First 3 asmdef) 채택 동의 (또는 A/C/D 또는 수정)
- [ ] 폴더 구조 동의 (Schema/Domain/Unity + feature 하위)
- [ ] Domain의 UnityEngine 참조 허용 여부 (Open Q1)
- [ ] Interfaces 폴더 처리 (Open Q3)
- [ ] 마이그레이션 시점 (Phase 2 시작 직전 별도 큰 PR로)
- [ ] PlayMode 테스트 asmdef 신설 (Open Q5)
