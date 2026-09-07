# QA 인풋 — 개발자 노트

> **이 문서 성격**: 개발자만 아는 미구현/사장/알려진 이슈/의도적 결정을 QA에게 미리 알려 헛발질을 방지. **"이거 버그 아닌가요?" 리포트 쓰기 전 여기부터 확인 권장**.
> **스냅샷 시점**: 2026-07-10 기준.
> **QA 영역 침범 금지**: 이 문서는 정보 제공. 여기 없는 이슈는 QA가 자유롭게 리포트.

---

## 1. 미구현 기능 (설계에 있으나 미구현)

| 항목 | 현재 상태 | 관련 파일 | QA 시사점 |
|---|---|---|---|
| **파산** | 미구현. `Stats.SubMoney`가 `Mathf.Max(0, value)`로 클램프. Money가 0에서 stagnate하지만 게임 오버·경고·UI 트리거 없음 | [`StatsService.cs:102`](../../../Assets/Scripts/Domain/Common/StatsService.cs) | Money 0 상태에서도 게임 계속 진행됨 — 이건 버그 아님 |
| **엔딩 조건** | 명시적 엔딩 없음. Day 60~90 최종 업그레이드 완료가 소프트 목표 (밸런싱 기준) | — | 특정 Day 도달 시 엔딩 컷씬/자동 종료 없음 |
| **Weather 반영** | 40% Bad 확률로 매일 갱신되지만 실질 게임에 미반영. NPC 대사 필터(게토로/파자마 등) 외 관찰 지점 없음 | [`WeatherService`](../../../Assets/Scripts/Domain/Common/WeatherService.cs) | Bad Weather 날에도 손님 수/매출/텃밭 성장/BGM/VFX 변화 없음 |
| **모바일 터치** | 미지원 | — | 모바일 브라우저에서 열어도 조작 불가 |
| **게임패드** | 미지원 | — | 컨트롤러 인식 안 됨 |
| **NPC 표정 다양성** | 각 NPC 1장(default sprite)만. 표정별 초상화 다수 정의 없음 | [`DeliveryNpcData.cs`](../../../Assets/Scripts/Schema/Config/Mall/DeliveryNpcData.cs) | 대화 중 표정 변화 없음 = 사양대로 |

---

## 2. 사장 (deprecated / 사실상 미사용) 기능

| 항목 | 상태 | 참고 |
|---|---|---|
| **Weather 시스템** | 코드 살아있음. `IsBadWeather` 매일 계산됨. **실제 반영은 NPC 대사 필터 뿐** — 밸런싱 결합 미구현. | [`../systems/weather-system.md`](../systems/weather-system.md) |
| **`ActionSelectionManager` / `ProgressSystem` / `RecipeDataManager` / `StatsSystem` (GameStart 씬의 placeholder GO)** | 리팩터링 이전 잔재. 실제 매니저는 Managers 씬. 삭제 대상이지만 씬 저장에 남아있음. | [`../scenes/scene-gamestart.md`](../scenes/scene-gamestart.md) |
| **`goHomeButton` (Mall 인스펙터 필드)** | 상단 UI GoHome 버튼 — 사실상 이제 playerStore GoHome trigger가 주 경로. 인스펙터 필드는 legacy | [`MallSceneController.cs`](../../../Assets/Scripts/Unity/Common/MallSceneController.cs) |
| **`LoadInventoryUsecase` Search 메서드** | `/* Deprecated */` 주석 표기 | [`LoadInventoryUsecase.cs:5`](../../../Assets/Scripts/Schema/Interfaces/Cooking/LoadInventoryUsecase.cs) |
| **`moai_solo` dialog.csv 라인** | 남아있지만 카탈로그는 그룹 대사만 씀. 캐주얼 대사로 흡수됨 | [`../content/npcs.md`](../content/npcs.md) |

---

## 3. 알려진 이슈 (재현 가능/불가능)

| 이슈 | 재현성 | 대응 |
|---|---|---|
| **WebGL 전체화면 Escape 충돌** | 재현 가능. 전체화면 탈출과 게임 내부 모달 닫기가 모두 `Escape`를 사용한다 | 모달이 열려 있으면 게임이 먼저 닫고 입력을 소비한다. 닫을 모달이 없을 때만 브라우저의 전체화면 탈출 동작이 가능해야 한다. WebGL 실기기 검증 필요 |
| **Cooking 조기 종료 버튼과 시계 겹침** | 재현 가능. 영업 중 상단 우측에서 두 UI가 겹친다 | 조기 종료 버튼을 시계 안전 영역 아래로 이동. 다양한 가로폭에서 중첩 여부 검증 필요 |
| **Garden 작물 타이머/이름 겹침** | 재현 가능. 월드 공간 텃밭 UI에서 타이머가 크고 작물명 영역과 겹친다 | 타이머 크기와 오프셋을 조정하고, 성장 단계별 스프라이트에서 가독성 확인 필요 |
| **해상도 변경 뒤 UI 클릭 좌표 불일치** | 재현 가능. 화면은 새 해상도로 렌더링되지만 입력 hit 영역은 이전 위치에 남는다 | **배포 차단(P0)**. Canvas/입력 좌표 변환과 해상도 변경 후 레이아웃 갱신 경로를 조사한다. 변경 전후 실제 클릭 좌표를 포함한 PlayMode 또는 WebGL 회귀 시나리오가 필요 |
| **Cooking "종료하기" 버튼 이슈** | 2026-07 초 관찰. 특정 상황에서 종료 후 흐름 오류. **재현 불가** (다시 시도하면 정상) | Watch item. 발견 시 재현 조건 최대한 상세 리포트 요청 |
| **첫 프레임 AudioListener 없어 소리 안 남** | 씬 로드 첫 프레임에 발생 가능. 다음 프레임 refresh로 자동 복구 | 이번 세션 fix ([`SoundManager.cs`](../../../Assets/Scripts/Unity/Common/SoundManager.cs)). 재발 시 리포트 |
| **WebGL 첫 로딩 30~60초** | Unity WebGL 특성. 압축 해제·에셋 로드 시간 | 사양대로 (개선 예정 별건). 첫 접속 안내 예정 |
| **저장 파일 스키마 마이그레이션 시 롤백 불가** | Legacy 5-file → 1-file 변환은 자동. 그러나 신규 필드 추가 시 이전 저장 로드는 default 값으로 채움 | 저장 파일 소실/오염 대비. 리포트 시 `gamedata.json` 파일 동봉 부탁 |
| **Editor 재컴파일 후 Cooking 씬 UI 상태 초기화** | Domain Reload 시 Managers 씬 상태 복원되지 않을 수 있음 | Editor only. WebGL 빌드에는 무관 |

---

## 4. 의도적 결정 (버그로 보일 수 있으나 의도적)

| 결정 | 이유 |
|---|---|
| **W / S 키 미지원** | 2D 사이드뷰(Mall/Shop/Garden). 상하 이동 없음. Cooking은 top-down이지만 상호작용 방식이 클릭·드래그이므로 상하 이동 키 없음 |
| **우클릭 미사용** | 좌클릭 + Space 조합으로 통일 |
| **마우스 휠 미사용** | 줌 없음 (고정 orthographic size) |
| **`EventSystem.sendNavigationEvents = false` (Mall)** | Space/Enter가 UI 버튼 selected 상태를 잘못 트리거 방지. 카메라 이동키(A/D)가 UI 오염 안 하게 |
| **Preparation 페이즈 자동 영업** | Preparation은 도시락 메뉴 선택 후 자동으로 Cooking(Morning) 진입. 선택지 없음 (기획 의도) |
| **Morning 자동 영업** | 3택 UI 없음. Preparation 확정 후 자동으로 Cooking |
| **3택 액션은 Afternoon/Evening/Night만** | Work / Rest / Shopping. Preparation·Morning엔 안 뜸 |
| **Settlement "아무 키" 입력으로 Mall 복귀** | 명시적 버튼 없음. 편의성 (`Input.anyKeyDown`) |
| **같은 유통기한 인벤토리 배치 병합** | 구매 시각 다르더라도 `daysRemaining` 같으면 한 배치로 합침 (2026-07 fix). UI 간결화 |
| **실패 손님 exit x = -9.89** | 성공/실패 모두 왼쪽으로 나가되, 실패는 애매한 위치가 아닌 명시적 exit 좌표 (2026-07 fix) |
| **CookingTutorial 별도 씬** | 실 Cooking 씬을 격리해 mock 조건(재료 무한 refill, 시간 정지, 손님 억제) 안전하게 걸기 위해 |
| **재료 자동 심기 (Garden)** | 잠기지 않은 빈 밭 진입 시 자동으로 spawnWeight 랜덤 심기 — 유저가 항상 심을 필요 없음 |
| **NPC 대화 중 스페이스도 클릭도 대체 허용** | 튜토리얼 Space dismiss 파트는 좌클릭도 dismiss (마우스 유저 편의) |
| **로그 스택트레이스 스크립트만** | `SetStackTraceLogType(Log, None)` — Log는 스택 없음. Warning/Error/Exception/Assert는 ScriptOnly (Unity 내부 스택 제거) |
| **BGM `Play()` 사용 (`UnPause` 아님)** | `Play`는 stopped/paused 둘 다 커버. `UnPause`는 paused만 (2026-07 fix) |

---

## 5. 미지원 플랫폼 / 입력

- **모바일 브라우저**: 터치 미지원
- **게임패드**: 미지원
- **키보드 nav (Space/Enter → UI 버튼)**: Mall에서 명시적으로 비활성
- **우클릭**: 미사용
- **마우스 휠**: 미사용
- **컨트롤러 리매핑 UI**: 없음
- **해상도 UI**: WebGL 기본 크기 사용 (BuildAutomation 설정)

---

## 6. 개발/QA 환경 주의사항

| 항목 | 내용 |
|---|---|
| **Unity 버전** | 6000.3.2f1 (다른 버전 열면 오작동 가능) |
| **활성 Input Handler** | Legacy (`activeInputHandler: 0`). Unity 6 Input System 아님 |
| **WebGL 압축** | Brotli + gzip DecompressionFallback 사용. 서버 헤더 없이도 브라우저에서 복원 가능 |
| **Editor 직접 Play** | Boot 씬 안 거치고 GameStart부터 열어도 `ManagerBootstrap.EnsureAll()`이 fallback |
| **Editor에서만 재현되는 이슈** | Domain Reload, Awake/Start 타이밍, `#if UNITY_EDITOR` 코드 등. WebGL 빌드로 재검증 권장 |
| **PlayMode 테스트** | `Assets/Tests/EditMode/` 에 유닛 테스트. 회귀 시 참고 |
| **Unity MCP** | 개발자용 원격 조작 도구. QA는 사용 대상 아님 |

---

## 7. 저장 (Save/Load) 관련

| 항목 | 내용 |
|---|---|
| **저장 파일 이름** | `gamedata.json` (단일 파일, 이전 5-file은 자동 마이그레이션) |
| **저장 위치 (Standalone)** | `Application.persistentDataPath` |
| **저장 위치 (WebGL)** | IndexedDB (`FS.syncfs`로 flush) |
| **저장 시점** | `PassDay()` 자동 저장 + NewGame 즉시 저장 |
| **저장 실패 시 UI** | Settlement의 saveStatusText가 "저장 중..." → "아무 키나 눌러서 계속" 로 변경됨 |
| **Legacy migration** | 이전 5개 파일 발견 시 자동 통합. 별도 마이그레이션 도구 없음 |
| **저장 파일 편집 시** | JSON 직접 편집하면 로드 실패 가능성. 스키마는 [`GameSaveData`](../../../Assets/Scripts/Unity/Common/SaveManager.cs) 참조 |

상세: [`../systems/save-system.md`](../systems/save-system.md)

---

## 8. 로그 / 콘솔 관련

이번 세션(2026-07) 로그 정리 결과:

| 항목 | 내용 |
|---|---|
| **대부분의 Debug.Log 제거됨** | 게임 이벤트 로그(주요 전환) 위주만 유지 |
| **스택트레이스 제거** | `Application.SetStackTraceLogType`으로 Log는 없음, 나머지는 ScriptOnly |
| **GameStateReporter** | Error/Exception/Assert 로그 시 자동 상태 dump. 씬, Progress, Stats, UILocked, Audio 상태, Inventory 포함 | 
| **덤프에서 제외되는 프리픽스** | `[TextureDiag]`, `[FontPreWarmer]`, `[LogSpam]` (별건 진단용, 게임 상태 아님) |
| **AudioListener 없음 경고** | 씬 전환 첫 프레임 발생 가능성 → `AudioSource.enabled = false`로 근본 차단 (2026-07 fix). 이후 재발 시 리포트 |
| **`missing script` warning** | 씬 정리로 제거됨 (2026-07 fix). 재발 시 리포트 |

**QA 리포트 시 요청사항**: 에러/Exception 재현 시 콘솔의 **GameStateReporter dump**를 함께 스크린샷/텍스트로 첨부해주세요.

---

## 9. 밸런싱 관련 노트

| 항목 | 현재값 | 참고 |
|---|---|---|
| **초기 자산** | Money 12,000G / Stamina 100 / Day 0 | GameStart.NewGame 기본값 |
| **매일 관리비** | 1,000G (Settlement에서 차감) | `SettlementService.ManagementFee` |
| **PassDay Stamina 회복** | 100 리셋 (Rest 액션과 별개) | `Progress.PassDay()` |
| **Morning 스폰 텀** | 35s (2026-07 초 복원값) | `CustomerSpawner` |
| **인내심 기본값** | 90s | `TimeManager.defaultCustomerWaitTime` |
| **인내심 가속** | 영업 종료 후 2배 (`closedLocalTimerScale=2`) | `TimeManager` |
| **파산 임계** | 0G에서 클램프 (게임 오버 없음) | [1번 항목 참조](#1-미구현-기능-설계에-있으나-미구현) |
| **엔딩 목표** | Day 60~90 최종 업그레이드 (소프트) | 밸런싱 sim harness 기반 |
| **업그레이드 코스트 배율** | ×1.0 (2026-07 초 원상 복구) | `f088dac` commit |
| **날씨 Bad 확률** | 40% (미반영) | `WeatherService.BadWeatherChance=0.4` |

상세 값: [`../reference/balance-values.md`](../reference/balance-values.md)

---

## 10. 튜토리얼 관련

| 스텝 | 활성 조건 | 참고 |
|---|---|---|
| `WelcomeAtSpawn` | NewGame → Mall 최초 Preparation | Mall 진입 시 자동 |
| `MallCorridor` | Shopping 액션 선택 시 (튜토리얼 활성) | Mall 내부 이동 안내 |
| `CookingIntro` | Preparation 확정 시 (튜토리얼 활성) | CookingTutorial 씬으로 이동 |
| `PhaseSelectAfternoon` | Afternoon 진입 시 (튜토리얼 활성) | Work/Rest 비활성, Shopping만 |
| `MenuSelection` | 5 파트 (도시락 선택 안내) | Mall Preparation |
| `Closing` | Afternoon 종료 시 (튜토리얼 활성) | Settlement로 넘김 |
| — 튜토리얼 skip 처리 | EarlyEndButton 통해 언제든 종료 가능 (`Tutorial.MarkShown`) | 인벤토리도 원상복귀 |

---

## 11. 코드 내 TODO/FIXME/HACK 주석

**현재 grep 결과**: 활성 TODO/FIXME/HACK 주석 없음 (Deprecated 주석 1건 [7번 항목 참고](#7-저장-saveload-관련)).

---

## 12. 배포 / 인프라 노트

| 항목 | 내용 |
|---|---|
| **배포 스크립트** | `scripts/deploy-gh-pages.sh` (자동 빌드 + gh-pages 브랜치 push) |
| **빌드 스크립트** | `Assets/Editor/BuildAutomation.cs` |
| **호스팅** | GitHub Pages (`https://teamssd.github.io/unity-project/`) |
| **압축** | Brotli + gzip DecompressionFallback (서버 헤더 없이 브라우저 복원) |
| **폰트 최적화** | SUIT 5종 pre-warm + Dynamic 폰트 전환 |
| **텍스처 압축** | Crunched compression 전면 적용 |
| **저장 flush** | WebGL은 `FS.syncfs`로 IndexedDB 반영 (SaveManager) |
| **첫 로딩 시간** | 30~60초 예상 (WebGL 특성. 사양대로) |
| **Unity Cloud** | `55bd8035-bb6d-4d66-9323-0d96bb37f714` |

---

## 13. QA가 자주 오해할 수 있는 지점 (요약)

- ❌ "Money가 0인데 게임이 안 끝나요" → 파산 미구현. 사양대로.
- ❌ "Bad Weather 날인데 손님 수가 그대로예요" → Weather 사장. NPC 대사만 반응.
- ❌ "Preparation에 Work/Rest/Shopping 안 나와요" → 3택은 Afternoon 이후만. Preparation·Morning은 자동 영업.
- ❌ "W/S로 위아래 못 움직여요" → 미지원. 좌우만.
- ❌ "우클릭이 안 먹혀요" → 미사용.
- ❌ "GameStart에서 Continue가 회색이에요" → 저장 없을 때 정상 비활성.
- ❌ "Cooking에서 손님이 왼쪽으로 나가는데 x=-9.89로 확 이동해요" → 실패 손님 exit 위치. 사양대로.
- ❌ "Settlement에서 정산 후 뭘 눌러야 할지 몰라요" → 아무 키 입력. saveStatusText 안내 참조.
- ❌ "저장 파일이 어디 있어요?" → Standalone: persistentDataPath, WebGL: IndexedDB.
- ❌ "인벤토리에서 같은 재료 2번 산 게 하나로 합쳐졌어요" → 같은 유통기한이면 병합. 사양대로.

---

## 이 문서 원칙

- 이 문서는 **현재(2026-07-10) 상태 스냅샷**. 코드 변경 시 갱신 필요.
- **QA 리포트 전에 여기 항목 확인 권장** — 사양대로 동작하는 걸 버그로 오해 방지.
- **여기 없는 이슈는 QA가 자유롭게 리포트** — 이 문서는 지시가 아님.
- 항목 추가/삭제 시 이 문서도 갱신 (게임 상태의 SSOT 아님 — SSOT는 코드).
- 관련 문서:
  - [`feature-inventory-scenes.md`](./feature-inventory-scenes.md) — 씬별 기능
  - [`feature-inventory-systems.md`](./feature-inventory-systems.md) — 시스템별 기능
  - [`feature-inventory-content.md`](./feature-inventory-content.md) — 콘텐츠 관찰 지점
  - [`recent-changes.md`](./recent-changes.md) — 최근 변경 지점
