# NPC 프로필 (10명 전수)

Aftertaste의 Mall 씬에 배치된 배달/캐주얼 NPC 10명 전원 프로필. 각 NPC는 `DeliveryNpcData` SO 인스턴스로 정의되며 (`Assets/Bundles/ScriptableObjects/DeliveryNpcData/*.asset`), Mall 씬의 프리팹 인스턴스에 wire 된다. 각 NPC의 대사는 배달 그룹 대사(그룹 단위, `Assets/Bundles/ScriptableObjects/Dialogue/<groupId>/`)와 일상 사이클 대사(NPC 개인 단위, `Assets/Bundles/ScriptableObjects/Dialogue/npc_normal/<name>/Section_*.asset`)로 이원화되어 있다.

## 스토리라인 잠금 체인

`prerequisiteGroupId`로 형성된 배달 퀘스트 스토리 순서:

```
power_room_pair (자르 + 게토로)         ← 시작. prerequisite 없음
    ├─▶ night_market (파자마 + 모아이 + 스라냐)
    │       └─▶ nimo_solo (니모)
    │               └─▶ seraph_solo (세라프)
    │                       └─▶ lede_solo (레데)
    │                               └─▶ gabriel_solo (가브리엘)
    └─ npc_lin (린) — groupId 없음, 순수 캐주얼
```

각 그룹은 이전 그룹이 `Completed`가 될 때까지 `DeliveryNpcDialogueInteraction.IsQuestUnlocked() == false` → 배달 대사 대신 일상 사이클(또는 CSV 폴백)만 재생.

---

## 1. 자르 (npc_jar)

| 필드 | 값 |
|---|---|
| **id** | `npc_jar` |
| **characterName** | 자르 |
| **sprite** | `driveAssets/art/character/char_jar_default` |
| **groupId** | `power_room_pair` |
| **prerequisiteGroupId** | (없음) |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 2, `(18.77, 0.34)` |
| **배달 파트너** | 게토로 (같은 그룹) |
| **의뢰 메뉴** | 전력실 도시락 (main=I044, side=I058) |
| **일상 사이클 섹션 수** | 10 (`Dialogue/npc_normal/jar/Section_1..10`) |
| **CasualDialogue (Any 조건)** | "이것 참, 1지구는 항상 설비 중 하나는 맛이 간다니까." / "어이, 반갑구만! 도시락 가게 수도관은 문제 없지? 별 일 있으면 불러!" |

**대사 톤**: 거칠고 시원시원한 배달꾼 방언. 스토리 시작점 NPC이자 전력실 그룹의 대변인.

## 2. 게토로 (npc_getoro)

| 필드 | 값 |
|---|---|
| **id** | `npc_getoro` |
| **characterName** | 게토로 |
| **sprite** | `driveAssets/art/character/char_getoro_default` |
| **groupId** | `power_room_pair` |
| **prerequisiteGroupId** | (없음) |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 3, `(13.53, 0.30)` |
| **배달 파트너** | 자르 (같은 그룹) |
| **일상 사이클 섹션 수** | 17 (`Dialogue/npc_normal/getoro/Section_1..17`) |
| **CasualDialogue (Good)** | "미세먼지 농도 수치 좋음. 오늘은 산책하기 좋은 날씨 군요." |
| **CasualDialogue (Bad)** | "미세먼지 농도 수치 나쁨. 마스크 쓰시는 걸 권장드립니다." |

**대사 톤**: 기계적/수치 중심 화법 ("안전 확률 43%. 평균 이하입니다.", "풍미 안정 84%. 식용 가능."). 자르와 대비되는 냉소적 관찰자.

## 3. 파자마 (npc_pajama)

| 필드 | 값 |
|---|---|
| **id** | `npc_pajama` |
| **characterName** | 파자마 |
| **sprite** | `driveAssets/art/character/char_pajama_default` |
| **groupId** | `night_market` |
| **prerequisiteGroupId** | `power_room_pair` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 5, `(-16.9, -7.2)` |
| **배달 파트너** | 모아이 + 스라냐 (같은 night_market 그룹) |
| **의뢰 메뉴 (그룹 공용)** | 야시장 도시락 (main1=I039, main2=I049) |
| **일상 사이클 섹션 수** | 10 |
| **CasualDialogue (Good)** | "오늘은 날씨가 좋아서 광원이 잘 나오겠는 걸… 어맛! 언제 오셨어요?" |
| **CasualDialogue (Bad)** | "오늘은 날씨가 탁해서 분위기가 잘 살겠는 걸… 어맛! 언제 오셨어요?" |

**대사 톤**: 잠들어 있다가 놀란 듯 반응하는 어리버리한 캐릭터. 야시장 그룹의 리더 라인.

## 4. 모아이 (npc_moai)

| 필드 | 값 |
|---|---|
| **id** | `npc_moai` |
| **characterName** | 모아이 |
| **sprite** | `driveAssets/art/character/char_moai_default` |
| **groupId** | `night_market` |
| **prerequisiteGroupId** | `power_room_pair` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 6, `(-13.0, -5.9)` |
| **일상 사이클 섹션 수** | 14 |
| **CasualDialogue (Any)** | "으흠, 으흠… 한 놈 두시기 석삼…" |

**대사 톤**: 말수 극소 ("…", "(끄덕)", "…맛있다. 고맙다."). 무언극처럼 진행되는 캐릭터. `dialog.csv` moai_solo 라인이 남아 있으나 카탈로그는 그룹 대사만 씀.

## 5. 스라냐 (npc_sranya)

| 필드 | 값 |
|---|---|
| **id** | `npc_sranya` |
| **characterName** | 스라냐 |
| **sprite** | `driveAssets/art/character/char_sranya_default` |
| **groupId** | `night_market` |
| **prerequisiteGroupId** | `power_room_pair` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 4, `(-22.6, -7.2)` |
| **일상 사이클 섹션 수** | 8 |
| **CasualDialogue (Any)** | "라랄 라라~ 아니야, 더 좋은 후렴이 있지 않을까? 루룰 루루~" |

**대사 톤**: 리듬 타는 노래 애호가. 즉흥 곡조를 흥얼거리는 아티스트 캐릭터. `dialog.csv`엔 sranya_solo 그룹의 별도 스토리 라인도 남아 있음(레거시).

## 6. 니모 (npc_nimo)

| 필드 | 값 |
|---|---|
| **id** | `npc_nimo` |
| **characterName** | 니모 |
| **sprite** | `driveAssets/art/character/char_nimo_default` |
| **groupId** | `nimo_solo` |
| **prerequisiteGroupId** | `night_market` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 7, `(-21.2, 5.4)` |
| **의뢰 메뉴** | 새벽국 도시락 (main=I034) |
| **일상 사이클 섹션 수** | 13 |
| **CasualDialogue (Any)** | "바쁘다 바빠! 조만간 더 큰 수송선을 사고야 말테야." |

**대사 톤**: 텐션 높은 활발한 상인. "우와아아!", "삼각밥! 삼각밥!" 같은 감탄사 다용.

## 7. 세라프 (npc_seraph)

| 필드 | 값 |
|---|---|
| **id** | `npc_seraph` |
| **characterName** | 세라프 |
| **sprite** | `driveAssets/art/character/char_seraph_default` (추정 — DeliveryNpcData asset의 sprite 필드가 GUID로 wire됨) |
| **groupId** | `seraph_solo` |
| **prerequisiteGroupId** | `nimo_solo` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv`에 미등재 (씬 GameObject Transform이 source of truth) |
| **의뢰 메뉴** | 네온 샐러드 (main=I060, side=I056) |
| **일상 사이클 섹션 수** | 10 |
| **CasualDialogue** | `npcCasualDialogue.csv`에 미등재 → 카탈로그 Normal 사이클만 재생. |

## 8. 레데 (npc_lede)

| 필드 | 값 |
|---|---|
| **id** | `npc_lede` |
| **characterName** | 레데 |
| **sprite** | `driveAssets/art/character/char_lede_default` |
| **groupId** | `lede_solo` |
| **prerequisiteGroupId** | `seraph_solo` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 8, `(27.0, -5.9)` |
| **의뢰 메뉴** | 삼각밥 도시락 (main=I053) — csv 기준. Config asset은 `기계장 고기정식` 계열 대사(레거시 dialog.csv와 매칭). |
| **일상 사이클 섹션 수** | 11 |
| **CasualDialogue (Any)** | "음흠흠… 오늘 거래는 틀어지지 않아야 할텐데…" |

**대사 톤**: 격식체 존댓말 ("실례합니다. 이 구역에 무슨 일이신지?", "감사히 잘 먹겠습니다."). 정중한 상인.

## 9. 가브리엘 (npc_gabriel)

| 필드 | 값 |
|---|---|
| **id** | `npc_gabriel` |
| **characterName** | 가브리엘 |
| **sprite** | `driveAssets/art/character/char_gabriel_default` |
| **groupId** | `gabriel_solo` |
| **prerequisiteGroupId** | `lede_solo` |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv` row 9, `(-30.1, 9.1)` |
| **의뢰 메뉴** | 기계장 고기정식 (main=I044) |
| **일상 사이클 섹션 수** | 17 (`Dialogue/npc_normal/gabriel/Section_1..17`) |
| **CasualDialogue (Any)** | "하… 인생이 살 맛이 안 나 살맛이…" / "도시락 장수, 지독하게도 할 일이 없나보군." |

**대사 톤**: 비관·자조. 스토리 체인의 종착점 NPC로, 가장 긴 일상 대사 사이클을 가진다.

## 10. 린 (npc_lin) — 순수 캐주얼

| 필드 | 값 |
|---|---|
| **id** | `npc_lin` |
| **characterName** | 린 |
| **sprite** | (asset의 sprite 필드) |
| **groupId** | (**없음**) |
| **prerequisiteGroupId** | (없음) |
| **초기 state** | `Orderable` |
| **CSV 위치 (레거시)** | `deliveryNPC.csv`에 미등재. `npcCasualDialogue.csv`도 없음. |
| **일상 사이클 섹션 수** | 1 (`Dialogue/npc_normal/lin/Section_1.asset`) |

`DeliveryNpcView.ApplyState`에서 `groupId` 비었으므로 `DeliveryNpcDialogueInteraction` 대신 **`CasualNpcInteraction`** 컴포넌트가 붙는다. 배달 흐름 없이 일상 대사 1개만 반복.

---

## 그룹별 대사 SO 요약

각 배달 그룹은 `Assets/Bundles/ScriptableObjects/Dialogue/<groupId>/` 하위에 5개 asset:

| groupId | Config | FirstMeet | QuestStart | Ordering | OrderEnd |
|---|---|---:|---:|---:|---:|
| `power_room_pair` | ✓ | 6 entry | 14 entry (choice) | 6 entry | 22 entry |
| `night_market` | ✓ | 3 | 11 | 18 | 20 |
| `nimo_solo` | ✓ | 3 | 11 | 15 | 25 |
| `seraph_solo` | ✓ | 3 | 9 | 14 | 19 |
| `lede_solo` | ✓ | 3 | 10 | 16 | 22 |
| `gabriel_solo` | ✓ | 4 | 15 | 16 | 22 |

`Config.asset`의 `normal` 슬롯은 6개 그룹 모두 `null` — 일상 대사는 그룹 단위 SO가 아니라 **NPC 개인의 npc_normal 카탈로그**에서 온다.

Config 카탈로그: `Assets/Bundles/Catalogs/DialogueConfigCatalog.asset` — 6개 그룹 매핑.
Normal 카탈로그: `Assets/Bundles/Catalogs/NpcNormalDialogueCatalog.asset` — 10명 NPC 매핑.

## 대사 진행 규칙 요약 (자세히는 [systems/delivery-quest-system.md](../systems/delivery-quest-system.md) / [systems/dialogue-system.md](../systems/dialogue-system.md))

1. **잠긴 NPC**: `prerequisiteGroupId`가 `Completed`가 아니면 배달 흐름 접근 불가 → `GetNormalSection()` 또는 `CasualDialogueProvider`.
2. **열린 NPC, 최초 대화**: `DeliveryDialogueConfig.firstMeet` → `AdvanceQuestStage`가 `QuestStart`로 전이.
3. **QuestStart 재대화**: `dialogueConfig.questStart` 재생. choice `accept` → 주문 생성 + `Ordering` / `defer` → 유지.
4. **Ordering 재대화 (요리 미완)**: `dialogueConfig.ordering` 재생.
5. **Ordering 재대화 (Cooked 감지)**: `ConsumeBento` + `orderEnd` 즉시 재생 → `Completed`.
6. **Completed 재대화**: `NpcNormalDialogueCatalog.GetSections(npcId)` 순환.
