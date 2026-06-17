using UnityEngine;

/// <summary>
/// DeliveryNpcDialogueInteraction 퀘스트 단계 진행 + 주문 생성 (partial).
/// </summary>
public partial class DeliveryNpcDialogueInteraction
{
    DialogueSO GetDialogueForStage(DeliveryQuestStage stage)
    {
        if (stage == DeliveryQuestStage.Normal || stage == DeliveryQuestStage.Completed)
            return GetNormalSection();

        if (dialogueConfig == null) return null;
        return stage switch
        {
            DeliveryQuestStage.FirstMeet  => dialogueConfig.firstMeet,
            DeliveryQuestStage.QuestStart => dialogueConfig.questStart,
            DeliveryQuestStage.Ordering   => dialogueConfig.ordering,
            DeliveryQuestStage.OrderEnd   => dialogueConfig.orderEnd,
            _ => null
        };
    }

    /// <summary>Normal 사이클 인덱스를 다음으로 진행 + 저장. Quest 단계와 무관.</summary>
    void AdvanceNormalCycle()
    {
        var catalog = CatalogProvider.NpcNormalDialogue;
        var sections = catalog?.GetSections(npcId);
        if (sections == null || sections.Count == 0) return;
        GameSessionRoot.Instance?.NpcNormalDialogue?.Advance(npcId, sections.Count);
    }

    /// <summary>npcId 기반 NpcNormalDialogueCatalog에서 현재 사이클 인덱스의 section 반환. 없으면 fallback.</summary>
    DialogueSO GetNormalSection()
    {
        var catalog = CatalogProvider.NpcNormalDialogue;
        var sections = catalog?.GetSections(npcId);
        if (sections == null || sections.Count == 0)
            return dialogueConfig?.normal; // legacy fallback

        var svc = GameSessionRoot.Instance?.NpcNormalDialogue;
        int idx = svc?.GetIndex(npcId) ?? 0;
        if (idx < 0 || idx >= sections.Count) idx = 0;
        return sections[idx];
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
                // FirstMeet 끝나면 곧바로 QuestStart로 진행 (Normal 단계 거치지 않음)
                svc.SetStage(groupId, DeliveryQuestStage.QuestStart);
                break;
            case DeliveryQuestStage.Normal:
            case DeliveryQuestStage.Completed:
                // 일상 대화 사이클 인덱스만 진행. 다른 quest 단계로는 전이하지 않음.
                AdvanceNormalCycle();
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
        if (svc == null) return DeliveryQuestStage.FirstMeet;
        // Service는 미등록 시 FirstMeet 반환 — 새 플레이어/새 NPC는 자연스럽게 FirstMeet에서 시작.
        return svc.GetStage(groupId);
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
