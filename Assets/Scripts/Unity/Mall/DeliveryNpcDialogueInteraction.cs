using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(DeliveryNpcContext))]
[RequireComponent(typeof(DeliveryNpcView))]
public partial class DeliveryNpcDialogueInteraction : MonoBehaviour, INpcInteraction
{
    private DeliveryDialogueConfig dialogueConfig;

    // questStages는 GameState.mall.persistent에 흡수 (3-C-3-a, DeliveryQuestService 경유).
    // questMenus는 QuestMenuCatalog에 흡수 (3-C-3-c, GameSessionRoot.QuestMenus 경유).

    private string groupId;
    private string prerequisiteGroupId;
    private string npcId;
    private string characterName;
    private Sprite portrait;
    private bool isPlayerNear;
    private bool isTalking;
    private DialogueManager dialogueManager;

    public void Init(string groupId, string prerequisiteGroupId = "", string npcId = "", string characterName = "", Sprite portrait = null)
    {
        this.groupId = groupId;
        this.prerequisiteGroupId = prerequisiteGroupId;
        this.npcId = npcId;
        this.characterName = characterName;
        this.portrait = portrait;
        dialogueConfig = CatalogProvider.DialogueConfig?.GetByGroupId(groupId);
        // 첫 진입은 FirstMeet에서 시작 — DeliveryQuestService가 미등록 groupId에 FirstMeet 반환하므로 별도 셋팅 불필요.
        // (이전엔 Normal로 시작했으나 Normal은 Completed 이후의 일상 대화 사이클로 분리됨)
    }

    private static bool MallPersistentHasGroup(string groupId)
    {
        var gp = GameSessionRoot.Instance?.State.mall.persistent;
        return gp != null && gp.questGroupIds.Contains(groupId);
    }

    void Start()
    {
        dialogueManager = Object.FindFirstObjectByType<DialogueManager>();
    }

    void Update()
    {
        if (isPlayerNear && !isTalking && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        if (!IsQuestUnlocked())
        {
            StartCasualDialogue();
            return;
        }

        var stage = GetCurrentStage();

        // Ordering 단계에서 음식이 완성됐으면 → 자동 배달 + OrderEnd 대화로 전환
        if (stage == DeliveryQuestStage.Ordering)
        {
            string qId = $"quest_{groupId}";
            var order = GameSessionRoot.Instance?.Order?.GetOrder(qId);
            if (order != null && order.state == DeliveryOrderState.Cooked)
            {
                GameSessionRoot.Instance?.Order.ConsumeBento(qId);
                GameSessionRoot.Instance?.DeliveryQuest.SetStage(groupId, DeliveryQuestStage.OrderEnd);
                stage = DeliveryQuestStage.OrderEnd;
            }
        }

        var dialogue = GetDialogueForStage(stage);
        if (dialogue == null)
            return;

        isTalking = true;
        InteractPromptUI.Hide();

        // 같은 그룹의 모든 NPC 초상화 수집
        var portraits = new Dictionary<string, Sprite>();
        foreach (var view in Object.FindObjectsByType<DeliveryNpcView>(FindObjectsSortMode.None))
        {
            if (view.GroupId == groupId && !string.IsNullOrEmpty(view.CharacterName) && view.Sprite != null)
                portraits[view.CharacterName] = view.Sprite;
        }

        dialogueManager.OnDialogueEnded += OnDialogueEnded;
        dialogueManager.StartDialogue(dialogue, portraits);
    }

    void StartCasualDialogue()
    {
        if (dialogueManager == null)
            return;

        // Quest 잠긴 NPC도 새 NpcNormalCatalog 사이클을 우선 사용. 카탈로그에 없으면 CSV 폴백.
        var dialogue = GetNormalSection() ?? CasualDialogueProvider.GetRandomDialogue(npcId);
        if (dialogue == null)
            return;

        isTalking = true;
        InteractPromptUI.Hide();

        Dictionary<string, Sprite> portraits = null;
        if (portrait != null && !string.IsNullOrEmpty(characterName))
            portraits = new Dictionary<string, Sprite> { { characterName, portrait } };

        dialogueManager.OnDialogueEnded += OnCasualDialogueEnded;
        dialogueManager.StartDialogue(dialogue, portraits);
    }

    void OnDialogueEnded(string resultTag)
    {
        dialogueManager.OnDialogueEnded -= OnDialogueEnded;
        AdvanceQuestStage(resultTag);

        if (this != null && gameObject.activeInHierarchy)
            ResetTalkingNextFrameAsync().Forget();
        else
            isTalking = false;
    }

    void OnCasualDialogueEnded(string resultTag)
    {
        dialogueManager.OnDialogueEnded -= OnCasualDialogueEnded;

        // Catalog 기반 Normal section을 보여줬다면 사이클 진행 (catalog 없으면 no-op)
        AdvanceNormalCycle();

        if (this != null && gameObject.activeInHierarchy)
            ResetTalkingNextFrameAsync().Forget();
        else
            isTalking = false;
    }

    private async UniTaskVoid ResetTalkingNextFrameAsync()
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        isTalking = false;
        if (isPlayerNear)
            InteractPromptUI.Show("*press spacebar to talk*");
    }

    // --- 플레이어 근접 감지 ---
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tags.Player))
        {
            isPlayerNear = true;
            if (!isTalking)
                InteractPromptUI.Show("*press spacebar to talk*");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(Tags.Player))
        {
            if (isTalking)
                dialogueManager.EndDialogue();

            isPlayerNear = false;
            InteractPromptUI.Hide();
            isTalking = false;
        }
    }

    // INpcInteraction (자동 트리거 비활성 - Space 키 사용)
    public void Interact() { }
}
