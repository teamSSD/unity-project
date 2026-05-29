using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DeliveryNpcDialogueInteraction : MonoBehaviour, INpcInteraction
{
    private DeliveryDialogueConfig dialogueConfig;

    // questStages는 GameState.mall.persistent에 흡수 (3-C-3-a, DeliveryQuestService 경유).
    private static Dictionary<string, MenuSchema> questMenus;

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
        // 첫 진입은 Normal(이미 알던 사이) 기본값으로 시작. 이전 SetStage 없으면 Service가 FirstMeet 반환하므로
        // 명시적으로 Normal 셋팅하여 기존 동작 유지.
        var svc = GameSessionRoot.Instance?.DeliveryQuest;
        if (svc != null && !MallPersistentHasGroup(groupId))
            svc.SetStage(groupId, DeliveryQuestStage.Normal);
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
            var order = OrderManager.Instance?.GetOrder(qId);
            if (order != null && order.state == DeliveryOrderState.Cooked)
            {
                OrderManager.Instance.ConsumeBento(qId);
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

        var dialogue = CasualDialogueProvider.GetRandomDialogue(npcId);
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

    DialogueSO GetDialogueForStage(DeliveryQuestStage stage)
    {
        if (dialogueConfig == null) return null;
        return stage switch
        {
            DeliveryQuestStage.FirstMeet  => dialogueConfig.firstMeet,
            DeliveryQuestStage.Normal     => dialogueConfig.normal,
            DeliveryQuestStage.QuestStart => dialogueConfig.questStart,
            DeliveryQuestStage.Ordering   => dialogueConfig.ordering,
            DeliveryQuestStage.OrderEnd   => dialogueConfig.orderEnd,
            DeliveryQuestStage.Completed  => dialogueConfig.normal,
            _ => null
        };
    }

    void AdvanceQuestStage(string resultTag)
    {
        if (!IsQuestUnlocked())
            return;

        var svc = GameSessionRoot.Instance?.DeliveryQuest;
        if (svc == null) return;

        var current = GetCurrentStage();

        switch (current)
        {
            case DeliveryQuestStage.FirstMeet:
                svc.SetStage(groupId, DeliveryQuestStage.Normal);
                break;

            case DeliveryQuestStage.Normal:
                svc.SetStage(groupId, DeliveryQuestStage.QuestStart);
                break;

            case DeliveryQuestStage.QuestStart:
                if (resultTag == "accept")
                {
                    CreateQuestOrder();
                    svc.SetStage(groupId, DeliveryQuestStage.Ordering);
                }
                break;

            case DeliveryQuestStage.Ordering:
                // Cooked 판정은 StartDialogue()에서 처리
                break;

            case DeliveryQuestStage.OrderEnd:
                svc.SetStage(groupId, DeliveryQuestStage.Completed);
                break;
        }
    }

    DeliveryQuestStage GetCurrentStage()
    {
        var svc = GameSessionRoot.Instance?.DeliveryQuest;
        if (svc == null) return DeliveryQuestStage.Normal;
        var stage = svc.GetStage(groupId);
        // Service는 미등록 시 FirstMeet 반환하지만, 이 클래스는 Normal을 기본값으로 사용
        return MallPersistentHasGroup(groupId) ? stage : DeliveryQuestStage.Normal;
    }

    bool IsQuestUnlocked()
    {
        return string.IsNullOrEmpty(prerequisiteGroupId) ||
            GetQuestStage(prerequisiteGroupId) == DeliveryQuestStage.Completed;
    }

    // --- 외부 API: 퀘스트 단계 수동 전환 (Service 위임) ---
    public static void SetQuestStage(string groupId, DeliveryQuestStage stage)
    {
        GameSessionRoot.Instance?.DeliveryQuest.SetStage(groupId, stage);
    }

    public static DeliveryQuestStage GetQuestStage(string groupId)
    {
        return GameSessionRoot.Instance?.DeliveryQuest.GetStage(groupId)
            ?? DeliveryQuestStage.FirstMeet;
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

    // --- 퀘스트 주문 생성 ---
    void CreateQuestOrder()
    {
        LoadQuestMenus();
        if (!questMenus.TryGetValue(groupId, out var template)) return;

        var menu = new MenuSchema(
            template.name,
            OrderManager.Instance.GetOrders().Count + 1,
            template.mainMenu,
            template.sideMenus
        );

        string questId = $"quest_{groupId}";
        var npcView = GetComponent<DeliveryNpcView>();
        OrderManager.Instance.GenerateOrder(menu, questId, npcView?.NpcId ?? "");

        var context = GetComponent<DeliveryNpcContext>();
        if (context?.receiptPrefab != null)
        {
            var receipt = Object.Instantiate(context.receiptPrefab);
            receipt.GetComponent<Receipt>()?.Set(menu);
            receipt.GetComponent<OrderTicketModel>()?.SetMenu(menu);
        }

        Debug.Log($"[DeliveryQuest] Order created: {menu}");
    }

    static void LoadQuestMenus()
    {
        if (questMenus != null) return;
        questMenus = new Dictionary<string, MenuSchema>();

        var csv = CatalogProvider.Csvs?.deliveryQuest;
        if (csv == null) return;

        var lines = csv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        // CSV 헤더: GroupId, MenuName, MainMenuId, MainMenu2Id, SideMenu1Id, SideMenu2Id, SideMenu3Id
        for (int i = 1; i < lines.Length; i++)
        {
            var t = lines[i].Split(',');
            if (t.Length < 2) continue;

            string gId = t[0].Trim();
            string menuName = t[1].Trim();

            var mains = new List<FoodData>();
            for (int j = 2; j <= 3 && j < t.Length; j++)
            {
                string id = t[j].Trim();
                if (!string.IsNullOrEmpty(id))
                    mains.Add(SearchDataUtil.GetFoodDataById(id));
            }

            var sides = new List<FoodData>();
            for (int j = 4; j < Mathf.Min(t.Length, 7); j++)
            {
                string id = t[j].Trim();
                if (!string.IsNullOrEmpty(id))
                    sides.Add(SearchDataUtil.GetFoodDataById(id));
            }

            questMenus[gId] = new MenuSchema(menuName, -1, mains, sides);
        }
    }

    // --- 리셋 (New Game) ---
    public static void ResetAll()
    {
        GameSessionRoot.Instance?.DeliveryQuest.Clear();
        questMenus = null;
    }

    // INpcInteraction (자동 트리거 비활성 - Space 키 사용)
    public void Interact() { }
}
