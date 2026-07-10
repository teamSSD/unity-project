# 컨셉

## 기본 정보

| 항목 | 값 |
|---|---|
| **제목** | Aftertaste |
| **버전** | 0.1.0 |
| **팀** | teamSSD |
| **엔진** | Unity 6000.3.2f1 |
| **타겟 플랫폼** | WebGL (itch.io / GitHub Pages) |
| **해상도** | 1920×1080 (Canvas Scaler ScaleWithScreenSize) |
| **저장** | 브라우저 IndexedDB (`persistentDataPath`) |
| **최신 배포** | https://teamssd.github.io/unity-project/ |

## 한 문단 요약

**Aftertaste**는 마법 도시의 상가 한 구석에서 도시락 가게를 운영하는 **2D 경영 시뮬레이션 게임**이다. 플레이어는 물려받은 비밀 레시피북으로 손님의 취향에 맞춰 도시락을 조합하고, 하루를 4단계 페이즈로 진행하며 매출을 쌓아 가게를 확장한다. 요리·손님·텃밭·배달·업그레이드 시스템이 서로 얽혀 있어 **자원 배분 판단**이 핵심 재미다.

## 장르 & 시점

| 항목 | 내용 |
|---|---|
| **메인 장르** | 경영 시뮬레이션 (Tycoon) + 요리 미니게임 |
| **서브 장르** | 어반 판타지 스토리 (배달 퀘스트) |
| **시점** | 사이드뷰, POV |
| **입력** | · 마우스 클릭 (기본 인터랙션)<br>· `A` / `D` / 화살표 좌우: 카메라 좌우 이동<br>· `Space`: 상호작용 · 화살표 상하: 미니게임 방향 입력<br>· `Tab`: 레시피북 열기/닫기<br>· `Escape`: 설정 프롬프트 / 취소 |
| **저장 시점** | 매 PassDay(하루 마감) 후 자동 (`SaveManager.SaveAll` → `WebGL: FS.syncfs`) |

## 세계관

**마법 산업 시대의 도시.** 생산·조리·재배 계열 마법이 결합된 상가 지역은 유명한 상점·공방·온실이 밀집한 상업 중심이다. 상가는 여러 층의 복도식 구조로, 층마다 특색 있는 마법사·직업인이 오간다.

플레이어는 이 상가의 **중간층에 위치한 한 상가**에서 **작은 도시락 가게**를 물려받는다. 세계관 톤은:
- **Cozy Fantasy** — 따뜻하고 익숙한 느낌의 일상감
- **Urban Fantasy** — 도시적 세팅에 마법 요소가 자연스럽게 스민 배경 (전력실, 야시장, 폐건물 등 실사 지명이 도시락 이름·재료로 등장)
- **NPC 개성** — 각 손님이 직업·라이프스타일에 맞는 취향의 도시락을 원함

## 시놉시스

> 마법으로 성장한 도시. 그 중 생산계열 마법을 강점으로 갖는 상가에는 유명한 상점과 공방, 온실 등이 모여있다.
> 주인공은 물려받은 비밀 레시피북을 가지고 상가의 중간층 작은 구석에서 도시락 가게를 차린다.
> 다양한 직업을 가진 마법사들에게 도시락을 팔아 가게를 운영하자.

## 코어 재미 (Fun Pillars)

우선순위대로 정리. 개발 결정 시 이 순서를 기준으로 트레이드오프.

| 순위 | 요소 | 설명 |
|---|---|---|
| 1 | **요리 미니게임 + 조합** | 도구별 미니게임(불 조절, 소스 뿌리기, 자르기, 섞기, 재료 배치)의 손맛 + 하루 3슬롯 메뉴 선택의 전략 |
| 2 | **가게 운영 판단** | 재고 관리, 페이즈별 영업 여부, 텃밭 재배·업그레이드 투자 배분 |
| 3 | **스토리 · 배달 퀘스트** | NPC별 개성 있는 대화와 배달 의뢰. 진행에 따라 도시 지역·재료·요리가 해금 |

## 게임 요소 개관

*상세는 각 시스템 문서 참조.*

| 카테고리 | 수량 / 요약 |
|---|---|
| 요리 재료 (`IngredientData`) | 35종 (일반 재료 · 특수 재료) |
| 요리 (`FoodData`) | 총 66종<br>· MAIN 6개: 새벽국, 구룡면, 기계장 고기정식, 스트리트 스테이크 49, 폐건물 삼각밥, 옥상 오믈렛<br>· SIDE 4개: 루미 젤리, 네온 샐러드, 전력실 꼬치, 환기구 연어구이<br>· 중간산물·쓰레기 등 나머지 56개 |
| 레시피 (`RecipeData`) | 31개 (재료 조합 → 결과물 트리) |
| 조리도구 | 5종 (팬 T001, 냄비 T002, 볼 T003, 도마 T004, 철판 T005) |
| 텃밭 작물 (`CropData`) | 별도 문서 참조 |
| 배달 NPC | 10명 (가브리엘, 게토로, 자르, 레데, 린, 모아이, 니모, 파자마, 세라프, 스라냐) |
| 배달 퀘스트 그룹 | 여러 그룹 (power_room_pair, night_market, nimo_solo, seraph_solo 등) — CSV 기반 확장 가능 |
| 튜토리얼 스텝 | 7개 (Welcome, Cooking, Recipe Book, PhaseSelect, Mall Corridor, Closing 등) |
| 게임 씬 | 9개 (Boot, Managers, GameStart, Mall, Shop, Cooking, Garden, Settlement, CookingTutorial) |

## 하루의 흐름 (요약)

*상세: [`core-loop.md`](./core-loop.md)*

한 하루는 **5개 페이즈 + 정산**으로 구성:

| 페이즈 | 위치 | 행동 |
|---|---|---|
| **Preparation** (준비) | Mall (BentoSelection UI) | **도시락 3슬롯** 각각에 Main 1개(필수) + Side 최대 3개 선택 → 확정 시 자동으로 Cooking 진입 (Morning으로 페이즈 전환) |
| **Morning** (아침) | Cooking | **자동 영업 (강제)** — 액션 선택 UI 없음 |
| **Afternoon** (점심) | Mall | **3택 선택창**: 영업(Work) / 휴식(Rest) / 상가(Shopping)<br>* Mall 자유 이동 중 Garden 진입 가능 |
| **Evening** (저녁) | Mall | 위와 동일 3택 |
| **Night** (밤) | Mall | 위와 동일 3택 |
| **Settlement** (정산) | Settlement | 하루 수입/지출/관리비 정산 → 아무 키 → 다음 날 Preparation |

**메뉴 슬롯 구조**: 3개 슬롯. 각 슬롯 = Main 1 + Side 0~3.  Main 없이 Side만 있으면 도시락 성립 불가.

## 대상 유저

| 기준 | 값 |
|---|---|
| 연령 | 12세 이상 (가벼운 판타지 톤, 폭력 없음) |
| 플레이 세션 | 1회 15–30분 (하루 1–3일 진행) |
| 총 클리어 예상 | Day 60–90 (마지막 업그레이드까지) |
| 첫 컨텐츠 오픈 | Day 15 (모든 메뉴 해금 목표) |
| 성향 | 코지 게임 · 경영 시뮬 · 요리 게임 팬 |

## 기술 상수

| 항목 | 값 |
|---|---|
| WebGL Compression | Gzip (with Decompression Fallback ON) |
| 첫 다운로드 예상 | ~55 MB (Brotli 대안 시 ~48 MB) |
| 첫 로딩 시간 (50Mbps) | ~20초 |
| 재방문 로딩 (IndexedDB 캐시) | ~3초 |
| 대상 브라우저 | Chrome, Firefox, Safari (모던 브라우저) |
| 오디오 | Vorbis Q0.6 (BGM Compressed In Memory + SFX Decompress) |
| 폰트 | SUIT (Regular/Medium/Bold/SemiBold/ExtraBold) Dynamic + Pre-warm |

## 참조 문서

| 문서 | 내용 |
|---|---|
| [`core-loop.md`](./core-loop.md) | 페이즈 구조, 하루 흐름, 자원 순환 |
| [`cooking-system.md`](../systems/cooking-system.md) | 요리 미니게임, 도구, 가격 산정, weight 체계 |
| [`customer-system.md`](../systems/customer-system.md) | 손님 스폰, 대기시간, 보상 공식 |
| [`garden-system.md`](../systems/garden-system.md) | 텃밭 재배, 작물, 텃밭의 경제적 역할 |
| [`progression.md`](../systems/progression-system.md) | 진행 곡선, 머니싱크, 레시피 해금 흐름 |
| [`upgrade-system.md`](../systems/upgrade-system.md) | 업그레이드 시스템 (조리도구·보관소·텃밭) |
| [`team.md`](../team.md) | teamSSD 팀 구성 |
