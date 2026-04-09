using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryNpcDialogueInteraction : MonoBehaviour, INpcInteraction
{
    private static readonly Dictionary<string, DeliveryQuestStage> questStages = new();
    private static Dictionary<string, MenuSchema> questMenus;

    private string groupId;
    private bool isPlayerNear;
    private bool isTalking;
    private DialogueManager dialogueManager;

    public void Init(string groupId)
    {
        this.groupId = groupId;
        if (!questStages.ContainsKey(groupId))
            questStages[groupId] = DeliveryQuestStage.Normal;
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
        var stage = GetCurrentStage();

        // Ordering 단계에서 음식이 완성됐으면 → 자동 배달 + OrderEnd 대화로 전환
        if (stage == DeliveryQuestStage.Ordering)
        {
            string qId = $"quest_{groupId}";
            var order = OrderManager.Instance?.GetOrder(qId);
            if (order != null && order.state == DeliveryOrderState.Cooked)
            {
                OrderManager.Instance.ConsumeBento(qId);
                questStages[groupId] = DeliveryQuestStage.OrderEnd;
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

    void OnDialogueEnded(string resultTag)
    {
        dialogueManager.OnDialogueEnded -= OnDialogueEnded;
        AdvanceQuestStage(resultTag);

        if (this != null && gameObject.activeInHierarchy)
            StartCoroutine(ResetTalkingNextFrame());
        else
            isTalking = false;
    }

    IEnumerator ResetTalkingNextFrame()
    {
        yield return null;
        isTalking = false;
        if (isPlayerNear)
            InteractPromptUI.Show("*press spacebar to talk*");
    }

    DialogueSO GetDialogueForStage(DeliveryQuestStage stage)
    {
        return stage switch
        {
            DeliveryQuestStage.FirstMeet => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/FirstMeet"),
            DeliveryQuestStage.Normal => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/Normal"),
            DeliveryQuestStage.QuestStart => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/QuestStart"),
            DeliveryQuestStage.Ordering => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/Ordering"),
            DeliveryQuestStage.OrderEnd => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/OrderEnd"),
            DeliveryQuestStage.Completed => Resources.Load<DialogueSO>("ScriptableObjects/Dialogue/Normal"),
            _ => null
        };
    }

    void AdvanceQuestStage(string resultTag)
    {
        var current = GetCurrentStage();

        switch (current)
        {
            case DeliveryQuestStage.FirstMeet:
                questStages[groupId] = DeliveryQuestStage.Normal;
                break;

            case DeliveryQuestStage.Normal:
                questStages[groupId] = DeliveryQuestStage.QuestStart;
                break;

            case DeliveryQuestStage.QuestStart:
                if (resultTag == "accept")
                {
                    CreateQuestOrder();
                    questStages[groupId] = DeliveryQuestStage.Ordering;
                }
                break;

            case DeliveryQuestStage.Ordering:
                // Cooked 판정은 StartDialogue()에서 처리
                // 아직 요리 안 됐으면 스테이지 유지
                break;

            case DeliveryQuestStage.OrderEnd:
                questStages[groupId] = DeliveryQuestStage.Completed;
                break;
        }
    }

    DeliveryQuestStage GetCurrentStage()
    {
        return questStages.TryGetValue(groupId, out var stage)
            ? stage
            : DeliveryQuestStage.Normal;
    }

    // --- 외부 API: 퀘스트 단계 수동 전환 ---
    public static void SetQuestStage(string groupId, DeliveryQuestStage stage)
    {
        questStages[groupId] = stage;
    }

    public static DeliveryQuestStage GetQuestStage(string groupId)
    {
        return questStages.TryGetValue(groupId, out var stage)
            ? stage
            : DeliveryQuestStage.FirstMeet;
    }

    // --- 플레이어 근접 감지 ---
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (!isTalking)
                InteractPromptUI.Show("*press spacebar to talk*");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
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

        var csv = Resources.Load<TextAsset>("driveAssets/dataTables/deliveryQuest");
        if (csv == null) return;

        var lines = csv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var t = lines[i].Split(',');
            if (t.Length < 2) continue;

            string gId = t[0].Trim();
            string menuName = t[1].Trim();
            FoodData main = t.Length > 2 && !string.IsNullOrEmpty(t[2].Trim())
                ? SearchDataUtil.GetFoodDataById(t[2].Trim()) : null;

            var sides = new List<FoodData>();
            for (int j = 3; j < Mathf.Min(t.Length, 6); j++)
            {
                string id = t[j].Trim();
                if (!string.IsNullOrEmpty(id))
                    sides.Add(SearchDataUtil.GetFoodDataById(id));
            }

            questMenus[gId] = new MenuSchema(menuName, -1, main, sides);
        }
    }

    // --- 리셋 (New Game) ---
    public static void ResetAll()
    {
        questStages.Clear();
        questMenus = null;
    }

    // --- 저장/로드 (SaveManager에서 호출) ---
    public static DeliveryQuestSaveData GetSaveData()
    {
        var data = new DeliveryQuestSaveData();
        foreach (var kv in questStages)
        {
            data.groupIds.Add(kv.Key);
            data.stages.Add((int)kv.Value);
        }
        return data;
    }

    public static void ApplySaveData(DeliveryQuestSaveData data)
    {
        questStages.Clear();
        for (int i = 0; i < data.groupIds.Count; i++)
            questStages[data.groupIds[i]] = (DeliveryQuestStage)data.stages[i];
    }

    // INpcInteraction (자동 트리거 비활성 - Space 키 사용)
    public void Interact() { }
}

[System.Serializable]
public class DeliveryQuestSaveData
{
    public List<string> groupIds = new();
    public List<int> stages = new();
}
