# 배달 그룹 (Delivery Groups)

**배달 그룹**은 하나의 `MenuSchema` 템플릿과 하나의 대화 config 세트(`DeliveryDialogueConfig`)를 공유하는 NPC들의 묶음이다. 그룹은 `Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv`에서 메뉴가 정의되고, `Assets/Bundles/driveAssets/dataTables/deliveryNPC.csv` 또는 `DeliveryNpcData` SO의 `groupId` 필드로 NPC와 매핑된다.

시스템 개요는 [systems/delivery-quest-system.md](../systems/delivery-quest-system.md), NPC 프로필은 [content/npcs.md](npcs.md) 참조.

## 메뉴 템플릿 원본 (deliveryQuest.csv)

**전체 rows** (헤더: `GroupId, MenuName, MainMenuId, MainMenu2Id, SideMenu1Id, SideMenu2Id, SideMenu3Id`):

```
power_room_pair, 전력실 도시락, I044, ,      I058, ,
night_market,   야시장 도시락, I039, I049, ,     ,
nimo_solo,      새벽국 도시락, I034, ,      ,     ,
seraph_solo,    네온 샐러드,   I060, ,      I056, ,
lede_solo,      삼각밥 도시락, I053, ,      ,     ,
gabriel_solo,   기계장 고기정식, I044, ,    ,     ,
```

**파싱 규칙** (`GameSessionRoot.ParseQuestMenus`):
- 컬럼 2~3 (MainMenuId, MainMenu2Id) → `MenuSchema.mainMenus` (여러 개 가능).
- 컬럼 4~6 (SideMenu1Id~SideMenu3Id) → `MenuSchema.sideMenus`.
- 각 ID는 `SearchDataUtil.GetFoodDataById`로 `FoodData` SO 해결.
- 결과 `QuestMenuCatalog` (Dictionary<groupId, MenuSchema>) 는 세션 시작 시 1회 빌드, `GameSessionRoot.QuestMenus` 프로퍼티.

## 그룹별 상세

### `power_room_pair` — 전력실 도시락

| 항목 | 값 |
|---|---|
| **메뉴명** | 전력실 도시락 |
| **Main 1** | `I044` (합성 닭 계열 — 대사 "합성 닭 도시락 배달 왔습니다"에서 확인) |
| **Main 2** | (없음) |
| **Side 1** | `I058` |
| **Side 2, 3** | (없음) |
| **참여 NPC** | `npc_jar` (자르), `npc_getoro` (게토로) |
| **선행 조건** | 없음 — **스토리 시작점** |
| **prerequisiteGroupId를 이 그룹으로 두는 후속 그룹** | `night_market` (파자마, 모아이, 스라냐) |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/power_room_pair/{FirstMeet, QuestStart, Ordering, OrderEnd}.asset` |

**스토리 훅**: 전력실 환경(전류, 절연 필요성)이 도시락 조리에 관한 문제 제기 → 유저가 "절연 버섯" 재료를 찾아 절연 도시락 제작. QuestStart 대사에 `accept`/`defer` choice 존재, `accept` 선택 시 서사가 이어지고 주문 생성.

### `night_market` — 야시장 도시락

| 항목 | 값 |
|---|---|
| **메뉴명** | 야시장 도시락 |
| **Main 1** | `I039` |
| **Main 2** | `I049` — **유일한 2-main 메뉴** |
| **Side** | (없음) |
| **참여 NPC** | `npc_pajama` (파자마), `npc_moai` (모아이), `npc_sranya` (스라냐) — **3인 그룹** |
| **선행 조건** | `power_room_pair` Completed |
| **후속** | `nimo_solo` |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/night_market/{...}.asset` |

야간 상권 3인이 함께 대화에 나오는 그룹. `DeliveryNpcDialogueInteraction.StartDialogue`에서 같은 groupId의 모든 `DeliveryNpcView`를 순회해 초상화를 수집하므로, 어느 NPC에게 말을 걸어도 3명의 초상화가 함께 노출된다.

### `nimo_solo` — 새벽국 도시락

| 항목 | 값 |
|---|---|
| **메뉴명** | 새벽국 도시락 |
| **Main** | `I034` (새벽국 — 대사에도 명시) |
| **Side** | (없음) |
| **참여 NPC** | `npc_nimo` (니모) — **1인 그룹** |
| **선행 조건** | `night_market` Completed |
| **후속** | `seraph_solo` |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/nimo_solo/{...}.asset` |

`dialog.csv`의 nimo_solo 라인은 "폐건물 삼각밥" 이야기지만, csv 메뉴는 `I034` (새벽국). csv/asset 이원화 상태에서 asset이 최종 프로덕션 소스로 우선한다.

### `seraph_solo` — 네온 샐러드

| 항목 | 값 |
|---|---|
| **메뉴명** | 네온 샐러드 |
| **Main** | `I060` |
| **Side** | `I056` |
| **참여 NPC** | `npc_seraph` (세라프) — 1인 그룹 |
| **선행 조건** | `nimo_solo` Completed |
| **후속** | `lede_solo` |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/seraph_solo/{...}.asset` |

`deliveryNPC.csv`에는 세라프 row가 없음 — Mall 씬 GameObject Transform이 위치 source of truth이고, `DeliveryNpcData` SO에서 groupId=seraph_solo로 세팅됨.

### `lede_solo` — 삼각밥 도시락

| 항목 | 값 |
|---|---|
| **메뉴명** | 삼각밥 도시락 |
| **Main** | `I053` |
| **Side** | (없음) |
| **참여 NPC** | `npc_lede` (레데) — 1인 그룹 |
| **선행 조건** | `seraph_solo` Completed |
| **후속** | `gabriel_solo` |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/lede_solo/{...}.asset` |

`dialog.csv`의 lede_solo 라인은 "기계장 고기정식" 서사인데 csv 메뉴는 `I053` (삼각밥). asset(SO 카탈로그) 대사 원본이 우선 반영되며, csv는 레거시 참고.

### `gabriel_solo` — 기계장 고기정식

| 항목 | 값 |
|---|---|
| **메뉴명** | 기계장 고기정식 |
| **Main** | `I044` (전력실 메뉴와 동일 ID — 확인 필요) |
| **Side** | (없음) |
| **참여 NPC** | `npc_gabriel` (가브리엘) — 1인 그룹 |
| **선행 조건** | `lede_solo` Completed |
| **후속** | (없음) — **스토리 종점** |
| **대사 파일** | `Assets/Bundles/ScriptableObjects/Dialogue/gabriel_solo/{...}.asset` |

가장 긴 일상 사이클(17 sections)을 가진 캐릭터이자 스토리 종점. Completed 이후에도 반복 방문할 가치가 있게 설계됨.

## 매핑 표 요약

| groupId | NPC들 | Main IDs | Side IDs | 선행 | 대사 sections (FM/QS/O/OE) |
|---|---|---|---|---|---|
| `power_room_pair` | 자르, 게토로 | I044 | I058 | — | 6/14/6/22 |
| `night_market` | 파자마, 모아이, 스라냐 | I039, I049 | — | power_room_pair | 3/11/18/20 |
| `nimo_solo` | 니모 | I034 | — | night_market | 3/11/15/25 |
| `seraph_solo` | 세라프 | I060 | I056 | nimo_solo | 3/9/14/19 |
| `lede_solo` | 레데 | I053 | — | seraph_solo | 3/10/16/22 |
| `gabriel_solo` | 가브리엘 | I044 | — | lede_solo | 4/15/16/22 |
| (없음) | 린 (npc_lin) | — | — | — | Casual only, 1 section |

## deliveryNPC.csv (레거시 참조)

**전체 rows** (헤더: `NpcId, CharacterName, SpritePath, PosX, PosY, DeliveryState, GroupId`):

```
npc_jar,     자르,   driveAssets/art/character/char_jar_default,     18.77,  0.34, Orderable, power_room_pair
npc_getoro,  게토로, driveAssets/art/character/char_getoro_default,  13.53,  0.30, Orderable, power_room_pair
npc_sranya,  스라냐, driveAssets/art/character/char_sranya_default, -22.60, -7.20, Orderable, sranya_solo    ← 레거시. Asset은 night_market
npc_pajama,  파자마, driveAssets/art/character/char_pajama_default, -16.90, -7.20, Orderable, pajama_solo    ← 레거시. Asset은 night_market
npc_moai,    모아이, driveAssets/art/character/char_moai_default,   -13.00, -5.90, Orderable, moai_solo      ← 레거시. Asset은 night_market
npc_nimo,    니모,   driveAssets/art/character/char_nimo_default,   -21.20,  5.40, Orderable, nimo_solo
npc_lede,    레데,   driveAssets/art/character/char_lede_default,    27.00, -5.90, Orderable, lede_solo
npc_gabriel, 가브리엘, driveAssets/art/character/char_gabriel_default, -30.10, 9.10, Orderable, (empty)      ← 레거시. Asset은 gabriel_solo + prereq lede_solo
```

**중요**: `.asset` 파일(source of truth)과 `.csv` (레거시 편집기 초기화용)에서 그룹 매핑이 다르다.
- csv: sranya/pajama/moai는 각각 단독 그룹 (`sranya_solo` / `pajama_solo` / `moai_solo`).
- asset: 셋 다 `night_market` 그룹.
- 프로덕션은 asset 값을 우선. csv의 `PosX/PosY` 컬럼은 `DeliveryNpcData.Init`에서 명시적으로 무시 (씬 GameObject Transform이 source).

**미등재 NPC (asset에만 존재)**: `npc_seraph`, `npc_lin`.

`DeliveryNpcData.Init` 코드:
```csharp
public void Init(string[] args) {  // Editor CSV → SO 변환
    id = args[0].Trim();
    characterName = args[1].Trim();
    sprite = Resources.Load<Sprite>(args[2].Trim());
    // args[3], args[4] = PosX, PosY — 무시 (씬 GameObject가 source)
    state = System.Enum.Parse<DeliveryNpcState>(args[5].Trim());
    groupId = args.Length > 6 ? args[6].Trim() : "";
}
```

## Food ID 참조

`I0xx` 코드는 `Assets/Bundles/driveAssets/dataTables/food.csv` 또는 `Assets/Bundles/ScriptableObjects/FoodData/*.asset`에서 조회 가능. 배달 메뉴에서 등장하는 ID들:

- `I034` — 새벽국 (nimo_solo)
- `I039`, `I049` — 야시장 도시락 재료 (night_market)
- `I044` — 합성 닭 계열 (power_room_pair, gabriel_solo 공용)
- `I053` — 삼각밥 (lede_solo)
- `I056` — 네온 샐러드 사이드 (seraph_solo)
- `I058` — 전력실 사이드 (power_room_pair)
- `I060` — 네온 샐러드 메인 (seraph_solo)

실제 FoodData의 표시명·이미지·가격은 `Design/gdd/content/foods.md` 또는 카탈로그 어셋에서 대조.

## 스토리 진행 흐름 (요약)

```
Day N Preparation/Afternoon:
  ├─ 자르/게토로에게 대화 → FirstMeet
  │  다시 대화 → QuestStart choice
  │    accept → 전력실 도시락 주문 생성, "절연 버섯" 서사, Ordering
  │    defer → 유지
  ├─ (Ordering 중) Cooking 페이즈에서 요리 완료 → MarkCookedWithPrice
  ├─ 다시 자르/게토로에게 대화 → OrderEnd 자동 재생 + 매출 지급
  └─ Completed → 이후 자르/게토로 재대화 시 Normal 사이클(Section 순환)

night_market 그룹 해금 → 파자마/모아이/스라냐 상호작용 가능
  ... (다음 그룹으로 반복)

gabriel_solo 완료 → 배달 스토리 종점
```

각 그룹은 1회 완료. `DeliveryQuestService.Clear()`로 개발자 리셋 가능.
