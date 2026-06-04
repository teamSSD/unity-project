using UnityEngine;

/// <summary>
/// DeliveryNpcDialogueInteraction 퀘스트 단계 진행 + 주문 생성 (partial).
/// </summary>
public partial class DeliveryNpcDialogueInteraction
{
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
        if (!IsQuestUnlocked()) return;

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

    public static void SetQuestStage(string groupId, DeliveryQuestStage stage)
        => GameSessionRoot.Instance?.DeliveryQuest.SetStage(groupId, stage);

    public static DeliveryQuestStage GetQuestStage(string groupId)
        => GameSessionRoot.Instance?.DeliveryQuest.GetStage(groupId)
            ?? DeliveryQuestStage.FirstMeet;

    public static void ResetAll()
        => GameSessionRoot.Instance?.DeliveryQuest.Clear();

    void CreateQuestOrder()
    {
        var template = GameSessionRoot.Instance?.QuestMenus.GetByGroupId(groupId);
        if (template == null) return;

        var orderSvc = GameSessionRoot.Instance?.Order;
        if (orderSvc == null) return;

        var menu = new MenuSchema(
            template.name,
            orderSvc.GetOrders().Count + 1,
            template.mainMenu,
            template.sideMenus
        );

        string questId = $"quest_{groupId}";
        var npcView = GetComponent<DeliveryNpcView>();
        orderSvc.GenerateOrder(menu, questId, npcView?.NpcId ?? "");
        UnlockMenuRecipes(menu);

        var context = GetComponent<DeliveryNpcContext>();
        if (context?.receiptPrefab != null)
        {
            var receipt = Object.Instantiate(context.receiptPrefab);
            receipt.GetComponent<Receipt>()?.Set(menu);
            receipt.GetComponent<OrderTicketModel>()?.SetMenu(menu);
        }

        Debug.Log($"[DeliveryQuest] Order created: {menu}");
    }

    /// <summary>메뉴의 모든 main/side 레시피를 해금.</summary>
    public static void UnlockMenuRecipes(MenuSchema menu)
    {
        var unlocked = GameSessionRoot.Instance?.UnlockedFood;
        if (unlocked == null || menu == null) return;
        foreach (var main in menu.mainMenus)
            if (main != null) unlocked.UnlockRecipe(main.id);
        if (menu.sideMenus != null)
            foreach (var side in menu.sideMenus)
                if (side != null) unlocked.UnlockRecipe(side.id);
    }
}
