# 최근 변경 지점

**대상 기간**: 2026-05-01 ~ 2026-07-10 (refactor/2026-05 브랜치 시작 ~ 오늘)
**목적**: 회귀 검증 지시가 아니라 **정보 제공**. 최근에 코드/씬을 어디까지 건드렸는지 QA가 참고할 수 있게 정리.
**규모**: 이 기간 커밋 약 238건.

---

## 요약

- **대규모 리팩터링 완료** (Phase 2 ~ Phase 5). 매니저 26 → 13, POCO Service 10개 도입, GameState 트리(SSOT) 도입.
- **콘텐츠·튜토리얼 확장**: 5층 몰(계단 walk), 튜토리얼 6~7단 스텝, 배달/NPC 대사 개편, 미니게임 튜닝, 조기 종료 버튼.
- **WebGL 배포 파이프라인 구축**: GH Pages 자동 배포 스크립트 + Brotli + 폰트/텍스처 최적화.
- **이번 세션(2026-07-06 ~ 07-10) 잔버그 fix + 로그/오디오 정리 + GameStateReporter 진단 툴 추가**.

---

## 카테고리별 변경

### 1. 코드 리팩터링 (2026-05 ~ 2026-06)

**Phase 2 (~2026-05-28)** — 아키텍처 기반 재편
- 어셈블리 4계층 분리 (`Game.Schema` / `Game.Domain` / `Game.Unity` / `Game.Tests.PlayMode`) — 195 파일 마이그레이션.
- Catalog 패턴 도입 (`PrefabCatalog`, `CsvCatalog`, `CropSpriteCatalog`, BGM 등) — runtime `Resources.Load` = 0 달성. 자산은 `Assets/Bundles/` 로 이동.
- SO Event 인프라 + 8개 도메인 이벤트 SO 카탈로그.
- Coroutine → UniTask 전면 마이그레이션 (16 파일).
- `DOMAIN.md` — 도메인 용어집 + 식별자 컨벤션 명시.

**Phase 3-C + Phase 4 (~2026-06-01)** — POCO Service 도입
- **POCO Service 10개**: `StatsService`, `ProgressService`, `InventoryService`, `MenuSelectionService`, `UnlockedFoodService`, `RecipeLookupService`, `WeatherService`, `SettlementService`, `OrderService`, `DeliveryQuestService`.
- `GameState` + `GameSessionRoot` 스캐폴딩 (ADR-001 Option B) — 세이브 상태 SSOT 트리.
- `SoundManager` 3개(SoundManager/UISoundManager/GlobalButtonSfxManager) → 1개 통합.
- `Storage`/`ToolUpgradeManager`/`FarmTileStorage`/`FarmUpgradeManager`/`CropDataManager` → POCO Service + Persistent 데이터.
- `Awake().Instance` 액세스 18 → 0 (local var 캐싱).

**Phase 5 (~2026-06-04)** — 큰 파일 분해 / 이벤트 누수 / 매직 스트링
- 큰 파일 분해: `MenuCardController` 584→~250, `CustomerManager` 433→243, `ShopUIAdapter` 403→220, `DialogueManager` 321→254, `DeliveryNpcDialogueInteraction` 306→155.
- `CustomerManager`/`CustomerLifecycle` 이벤트 누수 5건 수정.
- `Storage` Composition Root 명시 주입, `BentoSelectionController.Inject` — 부수효과로 stale unlock 데이터 버그 fix.
- 함수 4개 분해로 `function_over41` GREEN.

**Phase 5 이후 소규모 리팩터링 (2026-06 후반 ~ 07)**
- **ADR-008**: Shop 도메인 서비스 캐시 + Inject (17곳). `refactor(shop): 412d600`.
- **UI Find→SerializeField** 22곳 (`RecipeBookManager`, `MenuCardController`, `MenuSlot`, `ShopBook`, `BatchRow`, `DialoguePanel` 등).
- **UIColors 확장** — 하드코딩된 팔레트를 `UIColors` 경유로 통일.
- **CookingToolModel / FoodModel** — 드롭 로직 단일 dispatched로 통합. `IngredientPlacementRules` POCO 추출 + 회귀 앵커 테스트 8건 (`Assets/Scripts/Domain/Cooking/IngredientPlacementRules.cs`, `Assets/Tests/EditMode/Services/IngredientPlacementRulesTest.cs`).
- **Idle 씬 → Mall 흡수** (`refactor(scene): 7bec84d`).
- **CircularGauge** 부모 rect auto-fit stretch — FarmTile에서 재사용.
- **Day 카운터 SSOT** → `PhaseData.Day`로 통합 (`refactor(state): c727da9`).

관련 자료 (개발자용):
- `tmp/refactor-2026-05/reports/review_master.md` — 마스터 진단
- `tmp/refactor-2026-05/decisions/` — ADR 7건
- `tmp/refactor-2026-05/diagnosis_status.md` — 진단 상태

---

### 2. 신규 기능 / 콘텐츠 (2026-05 ~ 2026-07)

**Mall / 씬 구조**
- **5층 몰 + 계단 walk 시스템** + GoHome interaction (`746d591`).
- 세 사이드뷰 씬(Mall/Garden/Shop) 경계(boundary) 시스템 통일 (`5139997`).
- **페이즈 전환 시 다음 페이즈 UI 즉시 자동 표시** (`feat(mall): 8ee6872`) + 균일 규칙 완성 (`6cd1fb7`).
- Night Rest → Settlement 씬 이동 재귀 락 우회 fix (`ea6a5fa`).

**Cooking**
- **영업 조기 종료 버튼** — 우상단, 확인 모달 후 페이즈 스킵 (`758eb40` 및 후속 5건 tone/툴팁 조정).
- **손님 스폰 위치/카메라 viewport** 정비 (`a3c7934`).
- **손님 시스템 정리** + 강조 크기 + lin 비례 보정 (`60f1df8`).
- **드래그 영수증을 도시락에 reparent** + 크기 lerp (`3ea9e8e`), 부착 영수증 크기 축소 (`610cef0`).
- **불 미니게임 SFX** 40% 감소 (`26e038e`).

**Minigame — 소스통(SauceMiniGame)**
- **stop ▶ 목표 마커 + 동적 targetGauge** 추가 (`e46c91a`).
- 게이지/마커 위치, tolerance(3~5), target 정규분포([20,70]/평균 50), score 대칭화 등 다수 튜닝 (`c41908b`, `bf922e0`, `985c09c`, `8bb778c`, `96bac46`, `be3dad7`).

**튜토리얼 시스템 (신규)**
- **TutorialBubble UI** (Down/Up tail, prefab, Overlay Canvas) — `fef169a`, `1a6b0cf`.
- **Content 모델 + Controller + Target 시스템** (`f0dbaa2`).
- **Welcome 스텝 MVP** 6 파트 (`6695965`).
- **Menu Selection 스텝** 5 파트 (`78eca24`).
- **Cooking Mock 씬 + 7 파트 튜토리얼** + 카메라 Lerp (`1110404`, 시나리오 확장 `7077785`).

**Speech / Dialogue Bubble** (다수 미세 조정)
- Tail tip을 목표점에 닿게 하는 방식으로 재구현 (`82be51d`) + 방향/anchor/target-normalized 후속 조정 8건.
- Skip cooldown + 꾹 누름 auto-advance + 선택지 중 typing 완성 (`50c3337`).

**NPC / Delivery**
- **손님/배달 NPC 데이터 개편** — temp 풀 → 새 10 NPC (`bc1ad19`).
- **배달 퀘스트 대사 콘텐츠 + 선행조건 시스템** (`be68d15`).
- **배달 보상에 메인 포함** — MenuValidator 공식 사용 (`bcec9d6`).

**Shop / Inventory / RecipeBook**
- **ShopBook** (레시피북 메타포) + RecipeBook 인덱스 재설계 (`179cb85`).
- 인벤토리 페이지 신규 + 다이어리 잔재 제거 (`07f3e85`).
- 페이즈별 라인업 갱신 + General 무한 매입 (`02505bd`).
- Recipe/Ingredient 탭 UI fix — Header 팽창 방지, 색상 일치 (`9b61a3f`, `8b19...`, `8aa4a24`).

**UI (Settings, GameStart 등)**
- **Settings backdrop 씬별 스타일** — blur + tint / solid (`d0a89fe`).
- Settings blur: offscreen blit → 5-tap 가우시안 2-pass → Dual Kawase (`a6c55fe` / `1aa11d8` / `4c48d57`).
- GameStart Continue / New Game 위치 교환 (`7c61f5b`).
- 상단 HUD 바 구현 + 스태미너 휴식 버그 fix (`4738ec8`).

**Farm / Garden**
- **FarmTile 하단에 작물 이름 표시** (`59ddac9`) — `Assets/Scripts/Unity/Garden/Farm.cs`, `Assets/Bundles/Prefabs/garden/FarmTile.prefab`.
- 창고/농장 업그레이드 항목별 분리 (`5fd9a60`).

**Audio**
- **BGM/SFX 6종 구현** + 걷기·NPC blip 버그 fix (`19073ec`).
- 미니게임별 조리 SFX 연동 + 프리팹 AudioClip 필드 연결 (`713a8b9` / `745c985`).
- SoundManager LoopSFX 시스템 + BGM 씬 자동 전환 (`bf52c3b`).

---

### 3. 이번 세션(2026-07-06 ~ 07-10) 버그 픽스

| # | 이슈 | 파일 | 커밋 |
|---|---|---|---|
| 1 | Garden → Mall 복귀 시 ActionSelector 자동 재출현 방지 | `Assets/Scripts/Unity/Common/MallSceneController.cs` | `3e9deaf`, `e904128` |
| 2 | 인벤토리 구매: 같은 daysRemaining이면 배치 병합 | `Assets/Scripts/Domain/Common/InventoryService.cs` | `e904128` |
| 3 | 실패 손님 exit 위치 x=-9.89로 조정 (성공 taking만 orderingPosition.y 정렬) | `Assets/Scripts/Unity/Cooking/CustomerSpawner.cs` | `e904128` |
| 4 | WebGL BGM `loadType` 2(Streaming) → 1(Compressed In Memory) — WebGL fatal NotSupportedError 해소 | `Assets/Bundles/Sound/bgm/*.mp3.meta` | `e904128` |
| 5 | Preparation 페이즈에 BentoSelection 자동 오버레이 (균일 규칙 완성) | `Assets/Scripts/Unity/Common/MallSceneController.cs` | `6cd1fb7` |
| 6 | UI 잠금 중 이동 입력 차단 | `Assets/Scripts/Unity/Common/PlayerMove.cs` | `0128b60` |
| 7 | 스폰된 도시락을 BentoSet 시각 크기와 일치 (localScale 1.42 대응) | `Assets/Scripts/Unity/Cooking/BentoSetModel.cs` | `ddfbc8e` |
| 8 | 영수증 hover 시 restY(5.54)에서 defaultPosition으로 lerp | `Assets/Scripts/Unity/Cooking/OrderTicketBehavior.cs` | `a9f2938` |
| 9 | BGM 최초 재생 안 되던 이슈 (UnPause → Play) | `Assets/Scripts/Unity/Common/SoundManager.cs` | `f40f40d` |
| 10 | 조리 시드 책임 분리 + splitmix 기반 mixing | (seed 관련) | `fe657aa` |
| 11 | 도시락 보상 이중 지급 + 메인이 사이드로 카운트되던 두 버그 | (reward 관련) | `31fee95` |
| 12 | 가격 계산을 기획 공식과 일치시킴 (이중 페널티 제거) | (reward 관련) | `538e193` |

---

### 4. 로그 / 디버깅 인프라 (2026-07-09 신설)

**GameStateReporter 신설** — `Assets/Scripts/Unity/Common/GameStateReporter.cs`
- `Application.logMessageReceived` 훅으로 Error/Exception/Assert 발생 시 자동 발동.
- 캡처 정보: 활성 씬, 최근 씬 이력(4개), Progress(Day/Phase), Stats(money/stamina/time), UILocked, Audio 상태(listener 개수 + playing sources), Inventory item count.
- 30초 dedup + `IgnorePrefixes([TextureDiag] 등)` 필터 + 재귀 방지.

**Debug.Log 대규모 정리** (`chore(logs): 62676f3`)
- 씬 lifecycle / 초기화 / 미니게임 내부의 진행 로그 제거.
- 미할당·오류 케이스는 `LogWarning`으로 승격.
- `TextureDiagnostics`: clean 스캔 무로그 + anomaly signature dedup + `invisibleInFrustum` 검사 비활성.

**Stack trace 볼륨 감축**
- `Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)` — 콘솔 볼륨 약 15배 감소.
- Warning/Error는 ScriptOnly 스택 유지.

---

### 5. 오디오 정리

- **SoundManager: AudioListener 스팸 근본 수정** — listener 없는 순간 AudioSource.enabled 토글 (`Assets/Scripts/Unity/Common/SoundManager.cs`).
- **BGM: `UnPause` → `Play`** — 씬 로드 순서 문제로 stopped 상태에서 최초 재생 안 되던 이슈 (`f40f40d`).
- **Play2DSFX**: listener 없으면 skip.

---

### 6. UI / UX 미세 조정 (이번 세션 관련)

- 영수증 hover 시 내려오는 동작 — `OrderTicketBehavior.cs`.
- Cooking BentoSet 시각 크기와 스폰 도시락 크기 일치 — `BentoSetModel.cs`.
- FarmTile 하단에 작물 이름 표시 — `Farm.cs`, `FarmTile.prefab`.
- UI 잠금 중 이동 입력 차단 — `PlayerMove.cs`.
- 씬 파일 변경 (`Cooking.unity`, `Mall.unity`) — 배치/컴포넌트 조정 반영. Unity 에디터에서 저장된 상태.

---

### 7. WebGL 배포 인프라 (2026-07-09 신설)

- **BuildAutomation.cs** — `Tools/Build/WebGL` (Release/Development) 원클릭 메뉴. 출력: `Builds/WebGL/{version}_{tag}/{yyyyMMdd_HHmmss}/`. `Assets/Editor/BuildAutomation.cs`.
- **`scripts/deploy-gh-pages.sh`** — `git worktree`로 gh-pages 별도 checkout(현재 브랜치 안 건드림), 최신 빌드 자동 감지, `.nojekyll` 자동 생성.
- **경로 이슈 fix**: 절대 경로 변환 (`feb73c9`), 공백 처리 basename quoting (`59a6e13`).
- **압축·최적화**: Brotli 압축 + DecompressionFallback (`32a1241`), 폰트 미사용 삭제 + Dynamic 전환 + Pre-warm (`d8c9c52`), 텍스처 Crunched 압축 전면 적용 + orphan 감사 (`cc4e18d`).
- **SaveManager IDBFS flush** — WebGL 세이브 안정화 (`545d662`).
- 배포 URL: `https://teamssd.github.io/unity-project/`.

---

### 8. 콘텐츠 / 자산 정리

- **GDD 전면 재작성** — `Design/gdd/` 40여 문서 (본 문서와 별개, 기획 자료).
- **텍스처 orphan 삭제** + Crunched compression 전면 적용 (`cc4e18d`).
- `availableTool` 일관성 정비 + sprite 고아 정리 (`d56f1f6`).
- 스캔용 감사 도구(`OrphanAudit`, `TextureDiagnostics`) 추가.

---

### 9. 시뮬레이션 / 밸런스 도구 (2026-07)

- **Headless Monte Carlo sim** — 밸런스 검증용 (`129ce34`).
- 밸런스 검증 도구 확장(`1c96768`), 페이즈 타이밍 실험 + 정책 액션 분포 분석 (`9503ddd`).
- 2026-07 밸런스 튜닝 반영 (`36c0ff4`), 업그레이드 비용 원상 복구 (`f088dac`).
- 시작 재료를 인벤토리 lv 0 capacity 안에 맞춤 (`aa16065`).

---

## QA에게 주는 시사점 (참고)

- 위 변경 영역의 씬/시스템은 최근에 손을 댔으니 눈에 띄는 이상 있으면 이 문서 항목과 대조해보시면 원인 추적에 도움.
- 리팩터링으로 **매니저가 통합·POCO화** 되었으므로, 다음 시스템은 이번 릴리스에서 실제 사용 경로 확인이 유용:
  - 세이브/로드 전 구간 (`GameState` SSOT, `SaveManager` IDBFS flush)
  - Stats/Progress/Inventory 상태 (매니저 → Service 전환)
  - Shop 매입/판매, Recipe Book, Farm 업그레이드 (Persistent 재구성)
  - SoundManager 통합 후 씬 전환 BGM 흐름
- 에러/Exception 재현 시 **콘솔의 GameStateReporter dump**를 함께 스크린샷/텍스트로 첨부해주시면 상태 진단이 빠릅니다.
- 도메인 모델 참고: `Design/gdd/reference/domain-model.md` 및 개발자 자료 `tmp/refactor-2026-05/`.

---

## 참조

- 리팩터링 상세 (개발자용): `tmp/refactor-2026-05/`
  - 마스터 진단: `tmp/refactor-2026-05/reports/review_master.md`
  - 진단 상태 추적: `tmp/refactor-2026-05/diagnosis_status.md`
  - ADR 7건: `tmp/refactor-2026-05/decisions/`
- 시스템 GDD: `Design/gdd/systems/`
- 브랜치: `refactor/2026-05`
- 배포 스크립트: `scripts/deploy-gh-pages.sh`, `Assets/Editor/BuildAutomation.cs`

---

## 이 문서 원칙

- 이 문서는 **릴리스별 변경 요약** — "여기 최근에 손댔습니다"라는 정보 공유 용도.
- **회귀 검증 우선순위는 QA가 결정**. 이 문서는 지시가 아니라 참고.
- **다음 릴리스 시 새 섹션으로 갱신** 예정. 기존 섹션은 유지하거나 아카이빙.
- 사실 관계는 git log · 실제 파일 · 커밋 해시 기준. 추측/전망 X.
