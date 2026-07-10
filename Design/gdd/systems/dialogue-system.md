# 대화 시스템 (Dialogue)

Aftertaste의 대화 시스템은 **하나의 `DialogueManager`** 가 모든 NPC 대사(배달 퀘스트 5단계, 일상 사이클, 랜덤 CSV 대사)를 재생하는 파이프라인이다. 대사 데이터는 `DialogueSO`(SO 자산) 또는 `npcCasualDialogue.csv`(런타임에 SO로 변환)에서 오며, 초상화는 씬의 `DeliveryNpcView`에서 수집한다.

## 데이터 모델

`Assets/Scripts/Schema/Config/Mall/DialogueSO.cs`:

```csharp
public class DialogueSO : ScriptableObject {
    public List<DialogueEntry> entries;
}

[Serializable] public class DialogueLine {
    public string speaker;           // "자르", "게토로", "플레이어", "내레이션" 등
    [TextArea(2,4)] public string text;
}

[Serializable] public class DialogueEntry : DialogueLine {
    public List<DialogueChoice> choices;   // 비어 있으면 자동 진행
}

[Serializable] public class DialogueChoice {
    public string label;             // 버튼 텍스트
    public List<DialogueLine> responses;  // 선택 후 재생될 부가 대사
    public string resultTag;         // "accept" / "defer" 등 → AdvanceQuestStage에서 사용
}
```

- **entries**: 순차 재생되는 대사 리스트. 마지막 entry 지나면 `EndDialogue()`.
- **choices**: entry에 있으면 선택지 UI 표시, 하나 고르면 `responses`를 순차 재생 후 다음 entry로.
- **resultTag**: 선택 후 `lastResultTag`에 저장 → `OnDialogueEnded(resultTag)` 이벤트로 전달 → 퀘스트 단계 진행 로직이 판단.

## DialogueManager (파이프라인)

**파일**: `Assets/Scripts/Unity/Mall/DialogueManager.cs` + `Assets/Scripts/Unity/Mall/DialogueManager.Choices.cs` (partial)

### 초기화

`Start()`에서 자체 Canvas를 스폰(ScreenSpaceOverlay, sortingOrder=50), `dialoguePanelPrefab`을 자식으로 인스턴스화. `DialoguePanelRefs` 컴포넌트로 참조 수집:

- `nameText` (화자 이름)
- `dialogueText` (본문)
- `choicesParent` (선택지 버튼 컨테이너)
- `portraitContainer` / `npcPortraitImage` (초상화)

패널 자체를 Button으로 만들어 클릭도 진행 입력으로 처리.

### API

```csharp
public void StartDialogue(DialogueSO dialogue, Dictionary<string, Sprite> portraits = null)
public void EndDialogue()
public event Action<string> OnDialogueEnded;   // resultTag 전달
```

### 입력 파이프라인 (Update)

| 조건 | 동작 |
|---|---|
| `Space` down + `_isTyping` | `SkipTyping()` — 타이핑 즉시 완료, 재입력 방어 `skipCooldown=0.15s` |
| `Space` down + typing 완료 + `!waitingForChoice` | `AdvanceDialogue()` — 다음 entry로 |
| `Space` 꾹 누름 (>=`holdThreshold=0.3s`) | `autoAdvanceInterval=0.15s` 마다 자동 진행 (여러 대사 빠른 스킵) |
| 패널 클릭 | 동일 진행 로직 (`OnPanelClicked`) |

`justStarted` 플래그로 첫 프레임 입력 무시 (진입 keydown 재소비 방지).

### AdvanceDialogue 흐름

```csharp
// 1. 분기(response) 재생 중이면 다음 response
if (branchResponses != null) {
    branchIndex++;
    if (branchIndex < branchResponses.Count) { ShowLine(...); return; }
    branchResponses = null; // 끝 → 메인으로 복귀
}

// 2. 메인 entries 진행
currentIndex++;
if (currentIndex < currentDialogue.entries.Count) ShowCurrentEntry();
else EndDialogue();
```

### 타이핑 애니메이션 (TypeTextAsync)

- `UniTask.Delay(0.04s)` 로 한 글자씩 append.
- 3글자마다 `npcBlipSfx` 재생 (`SoundManager.Play2DSFX(vol=0.7)`).
- `SkipTyping` — CTS 취소하고 `_currentLineText` 전문 즉시 대입.

### 초상화 처리 (ApplyPortrait)

```csharp
// "플레이어"는 이름 숨김 (닉네임 대신 " ") — CSV 원본은 유지, 렌더만 필터
string displayName = line.speaker == "플레이어" ? " " : line.speaker;

if (speakerPortraits.TryGetValue(line.speaker, out sprite))
    // 스프라이트 있음 → 흰색 표시
else if (portraits.Count > 0)
    // 다른 화자 스프라이트만 있음 → 회색 (0.5,0.5,0.5)
else
    // 초상화 없음 → 이름만
```

→ **이번 세션 수정**: "플레이어" 화자는 이름표를 공백으로 렌더. CSV / SO 원본 speaker 값은 그대로 유지되어 로직(초상화 매칭 등)이 깨지지 않는다.

### 종료 (EndDialogue)

- 패널 비활성, choices 클리어, 이름/텍스트 초기화.
- **1프레임 지연 Unlock** — 같은 프레임에서 Space 재입력이 다음 인터랙션으로 넘어가는 것 방지 (`DelayedUnlockAsync`).
- `OnDialogueEnded(resultTag)` 발화.

## 선택지 (DialogueManager.Choices.cs)

`ShowChoices(List<DialogueChoice>)` → 각 choice마다 `Button` prefab 인스턴스, 다음 구조로 레이아웃:

- 왼쪽에 삼각형 마커 (`▶`, TextMeshProUGUI, preferredWidth=30).
- 유동 너비 label + `HorizontalLayoutGroup` (childAlignment=MiddleLeft, spacing=5, padding=10).

`OnChoiceSelected(choice)`:

1. `choices` UI 비움, `waitingForChoice=false`.
2. `resultTag` 저장.
3. `responses`가 있으면 → `branchResponses` 세팅 후 `ShowLine(responses[0])`.
4. 없으면 → `AdvanceDialogue()`.

## 대화 진입점 (누가 StartDialogue를 호출하는가)

### 1. 배달 NPC 대화 (`DeliveryNpcDialogueInteraction`)

`Space` down 감지 → `StartDialogue()`:

```csharp
if (!IsQuestUnlocked()) { StartCasualDialogue(); return; }
var stage = GetCurrentStage();
if (stage == Ordering) {
    // Cooked면 자동 배달 + OrderEnd 대사로 전환
    ...
}
var dialogue = GetDialogueForStage(stage);   // firstMeet / questStart / ordering / orderEnd / normal
// 같은 groupId NPC들의 초상화 수집 (그룹 대화 지원)
var portraits = new Dictionary<string, Sprite>();
foreach (view in FindObjectsByType<DeliveryNpcView>())
    if (view.GroupId == groupId) portraits[view.CharacterName] = view.Sprite;
dialogueManager.OnDialogueEnded += OnDialogueEnded;
dialogueManager.StartDialogue(dialogue, portraits);
```

`OnDialogueEnded(resultTag)` → `AdvanceQuestStage(resultTag)` → 다음 단계로 상태 갱신.

### 2. Quest 잠긴 배달 NPC의 일상 대화 (`StartCasualDialogue`)

`prerequisiteGroupId`가 `Completed`가 아니면:

```csharp
// Catalog 우선, 없으면 CSV 폴백
var dialogue = GetNormalSection() ?? CasualDialogueProvider.GetRandomDialogue(npcId);
```

### 3. 일반 캐주얼 NPC (`CasualNpcInteraction`)

`DeliveryNpcView`가 `groupId == ""` (예: `npc_lin`) 인 NPC에 붙는 컴포넌트. `Interact()`시 `CasualDialogueProvider.GetRandomDialogue(npcId)`.

## CasualDialogueProvider (npcCasualDialogue.csv 런타임 파서)

**파일**: `Assets/Scripts/Unity/Mall/CasualDialogueProvider.cs`

### Load()

- `CatalogProvider.Csvs.npcCasualDialogue` (CSV 텍스트) 파싱.
- 헤더: `NpcId, Condition, Speaker, Text`.
- npcId별 List로 저장, `Condition ∈ {Any, Good, Bad}` (날씨 필터).

### GetRandomDialogue(npcId)

```csharp
bool badWeather = GameSessionRoot.Instance?.Weather?.IsBadWeather ?? false;
var candidates = lines.Where(l =>
    l.condition == "Any" ||
    (badWeather && l.condition == "Bad") ||
    (!badWeather && l.condition == "Good")
).ToList();
// GameRandom.Variable로 1개 픽업 → 런타임 DialogueSO 인스턴스 생성 (1 entry)
```

### `dialog.csv`

레거시 대사 DB (`groupId, id, npc, contents, condition, next, branchId` 컬럼). 현재 프로덕션은 SO 자산 (`Assets/Bundles/ScriptableObjects/Dialogue/**/*.asset`)을 우선 사용하나, `dialog.csv`는 배달 그룹 5개(power_room_pair, sranya_solo, pajama_solo, moai_solo, nimo_solo, lede_solo)의 대사 원본으로 남아 있다 (147 rows).

## NPC별 일반 대화 사이클 (Normal Cycle)

배달 퀘스트가 `Completed`된 뒤 (또는 잠긴 상태) 반복 재생되는 일상 대사.

### 카탈로그 (`NpcNormalDialogueCatalogSO`)

`Assets/Scripts/Schema/Catalog/NpcNormalDialogueCatalogSO.cs`:

```csharp
[Serializable] public struct Entry {
    public string npcId;
    public List<DialogueSO> sections;   // 순차 재생될 여러 대화 조각
}
public List<DialogueSO> GetSections(string npcId);
```

프로덕션 카탈로그 인스턴스: `Assets/Bundles/Catalogs/NpcNormalDialogueCatalog.asset` — 10명 NPC 모두 등록 (섹션 개수는 [content/npcs.md](../content/npcs.md) 참조).

### 진행 서비스 (`NpcNormalDialogueService`)

`Assets/Scripts/Domain/Mall/NpcNormalDialogueService.cs`:

```csharp
public int GetIndex(string npcId);                 // 현재 재생 인덱스
public void Advance(string npcId, int sectionCount); // (idx+1) % sectionCount
```

상태 저장: `MallPersistent.normalCycleNpcIds[]` / `normalCycleIndices[]` parallel list → `gamedata.json`.

### 흐름

1. `DeliveryNpcDialogueInteraction.GetNormalSection()`이 catalog에서 현재 인덱스의 section 반환. catalog에 없으면 `DeliveryDialogueConfig.normal` 폴백.
2. 대화 종료 후 `AdvanceNormalCycle()` → 다음 회차. 인덱스가 sectionCount에 도달하면 1번으로 순환.

## 프리팹 참조

`Assets/Bundles/Prefab/DialoguePanel.prefab` (추정 경로 — `dialoguePanelPrefab` 필드에 인스펙터 바인딩) — `DialoguePanelRefs` 컴포넌트로 references 노출.

## 사운드

- `npcBlipSfx` — 타이핑 중 3글자마다 `SoundManager.Play2DSFX`.
- `SoundManager.PlayUIBook()` — 대화 시작 시.
- `SoundManager.RegisterButtons(dialoguePanel.transform)` — 선택지 hover/click SFX 자동 wiring.
