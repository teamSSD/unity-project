# Aftertaste — Game Design Documents

`teamSSD` · Aftertaste 0.1.0 · Unity 6000.3.2f1 · WebGL

레포지토리 전체의 게임 콘텐츠·시스템·씬·상수·이벤트를 누락 없이 압축한 문서. 각 문서는 실제 코드/asset/CSV/scene 파일 참조에 근거.

## 🗺 문서 지도

### Overview (전체 개관)
| 문서 | 요약 |
|---|---|
| [`overview/concept.md`](overview/concept.md) | 게임 개요, 브랜딩, 장르·시점, 세계관, 코어 재미, 게임 요소 개관 |
| [`overview/core-loop.md`](overview/core-loop.md) | 5개 페이즈 흐름, 페이즈 전환 로직, 씬 전환 매핑, 자원 순환 |
| [`overview/controls.md`](overview/controls.md) | 키맵, 미니게임 입력, UILockManager, 씬별 인터랙션 |

### Systems (기능 시스템 15개)
| 문서 | 요약 |
|---|---|
| [`systems/cooking-system.md`](systems/cooking-system.md) | 조리 파이프라인, 미니게임 5+1종, 재료→도구→결과물 |
| [`systems/cooking-tools.md`](systems/cooking-tools.md) | 5개 도구 (T001~T005) 상세: durationMultiplier, 미니게임 매핑 |
| [`systems/customer-system.md`](systems/customer-system.md) | 스폰 파라미터, 인내심(90s→2배 가속), MenuValidator, 보상 공식 |
| [`systems/delivery-quest-system.md`](systems/delivery-quest-system.md) | DeliveryQuestStage 6단계, QuestMenuCatalog, 재수주 정책 |
| [`systems/dialogue-system.md`](systems/dialogue-system.md) | DialogueManager 파이프라인, choices, 초상화, 사이클 관리 |
| [`systems/garden-system.md`](systems/garden-system.md) | Farm/FarmTile 7칸, 성장 페이즈 단위, timeReduction 업그레이드 |
| [`systems/mall-system.md`](systems/mall-system.md) | 상가 구조, MallSceneController, 3택 액션, GoHome 흐름 |
| [`systems/menu-compositions.md`](systems/menu-compositions.md) | MenuSchema/MenuSelection 규칙, Main+Side, 실 조합 예시 |
| [`systems/progression-system.md`](systems/progression-system.md) | 레시피 해금 곡선, Day별 오픈, 머니싱크, 목표 (Day 15 / Day 60-90) |
| [`systems/save-system.md`](systems/save-system.md) | GameSaveData 전 필드, DataSaveUtil, WebGL FS.syncfs, Legacy migration |
| [`systems/settlement-system.md`](systems/settlement-system.md) | SettlementService, 관리비 1000G, 수입/지출 카테고리, DayEnd |
| [`systems/shop-system.md`](systems/shop-system.md) | PurchaseService (day, phase) 캐시, General 16 + Special 19, 4탭 UI |
| [`systems/time-phase-system.md`](systems/time-phase-system.md) | TimeManager, 페이즈별 시각/duration, cumulativePhaseIndex |
| [`systems/tutorial-system.md`](systems/tutorial-system.md) | TutorialController/StepData/StepPart 전 옵션, 6 asset 파트 dump |
| [`systems/upgrade-system.md`](systems/upgrade-system.md) | Tool/Storage/Farm 3종 업그레이드, CSV 실값 |
| [`systems/weather-system.md`](systems/weather-system.md) | WeatherService 40% Bad. 실질 사장 상태 (NPC 대사 필터 외 미반영) |

### Content (게임 데이터 전수)
| 문서 | 커버 |
|---|---|
| [`content/foods.md`](content/foods.md) | FoodData 66개 전수 (id/name/type/tools/price/exp/sprite) |
| [`content/ingredients.md`](content/ingredients.md) | IngredientData 35개 전수 (name/exp/price/category) |
| [`content/recipes.md`](content/recipes.md) | RecipeData 31개 전수 + DFS 트리 + 미니게임 매핑 |
| [`content/crops.md`](content/crops.md) | 텃밭 작물 13종 전수 (spawnWeight, 성장 페이즈) |
| [`content/npcs.md`](content/npcs.md) | 10명 NPC 프로필 (자르/게토로/파자마/모아이/스라냐/니모/세라프/레데/가브리엘/린) |
| [`content/delivery-groups.md`](content/delivery-groups.md) | 6개 배달 그룹 (power_room_pair 등) 메뉴+NPC+선행조건 |

### Scenes (씬 9개)
| 문서 | 씬 |
|---|---|
| [`scenes/scene-boot.md`](scenes/scene-boot.md) | Boot.unity — BootLoader, FontPreWarmer |
| [`scenes/scene-managers.md`](scenes/scene-managers.md) | Managers.unity — 7 매니저, ExecutionOrder |
| [`scenes/scene-gamestart.md`](scenes/scene-gamestart.md) | GameStart.unity — NewGame/Continue |
| [`scenes/scene-mall.md`](scenes/scene-mall.md) | Mall.unity — 5층 상가, GoHome |
| [`scenes/scene-shop.md`](scenes/scene-shop.md) | Shop.unity — 4탭 UI, 진입/이탈 |
| [`scenes/scene-cooking.md`](scenes/scene-cooking.md) | Cooking.unity — top-down 조리대, 도구 5, 도시락 3 |
| [`scenes/scene-cookingtutorial.md`](scenes/scene-cookingtutorial.md) | CookingTutorial.unity — mock 튜토리얼 |
| [`scenes/scene-garden.md`](scenes/scene-garden.md) | Garden.unity — FarmTile 40칸 |
| [`scenes/scene-settlement.md`](scenes/scene-settlement.md) | Settlement.unity — 정산 UI, PassDay |

### Reference (레퍼런스)
| 문서 | 요약 |
|---|---|
| [`reference/balance-values.md`](reference/balance-values.md) | 밸런스 상수 20 섹션 (초기 자산, 페이즈, 스폰, 스코어링, 업그레이드 CSV) |
| [`reference/domain-model.md`](reference/domain-model.md) | POCO Service 18개, GameState 트리, Adapter/Provider |
| [`reference/events-and-messages.md`](reference/events-and-messages.md) | C# 이벤트 31건 발행/구독 매핑 + Orphan SO Event 8건 |
| [`reference/settings-and-audio.md`](reference/settings-and-audio.md) | Settings UI, SoundManager, BGM 3+SFX 20, 폰트 SUIT 5+FontPreWarmer |

### 팀
| [`team.md`](team.md) | teamSSD 팀 구성 & 브랜딩 |

## 🔎 자주 찾는 것

| 물음 | 문서 |
|---|---|
| 하루가 어떻게 흘러가지? | [`overview/core-loop.md`](overview/core-loop.md) |
| 뭐 눌러야 하지? | [`overview/controls.md`](overview/controls.md) |
| 몇 개 요리 있어? | [`content/foods.md`](content/foods.md) |
| 특정 요리 만드는 법 | [`content/recipes.md`](content/recipes.md) |
| 손님 몇 명 오지? | [`systems/customer-system.md`](systems/customer-system.md) |
| 업그레이드 값 얼마? | [`systems/upgrade-system.md`](systems/upgrade-system.md) or [`reference/balance-values.md`](reference/balance-values.md) |
| 저장 어떻게? | [`systems/save-system.md`](systems/save-system.md) |
| NPC 몇 명? | [`content/npcs.md`](content/npcs.md) |
| 배달 퀘스트 흐름 | [`systems/delivery-quest-system.md`](systems/delivery-quest-system.md) |
| 밸런스 상수 총집합 | [`reference/balance-values.md`](reference/balance-values.md) |

## 🗄 이전 GDD

이전 8문서는 [`../archive/gdd-legacy/`](../archive/gdd-legacy/)에 보관.

## 📌 문서 원칙

- **실 코드/데이터 기반** — 추측 금지. 값이 안 보이면 "미확인" 표시.
- **파일 경로 링크** — Unity C# 스크립트, CSV, asset 경로 명시.
- **크로스레퍼런스** — 관련 시스템 간 상호 링크.
- **한글 톤** — 문서 한국어. 필드명은 영어 유지.
- **실시간 반영** — 게임 변경 시 관련 문서 업데이트.
