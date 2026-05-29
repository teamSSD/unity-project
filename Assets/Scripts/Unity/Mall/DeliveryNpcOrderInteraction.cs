using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DeliveryNpcOrderInteraction
    : MonoBehaviour, INpcInteraction
{
    private DeliveryNpcContext context;
    private bool isDisplaying = false;

    private MenuSchema menuSchema;
    private DeliveryNpcView npcView;

    private static int nextOrderingNumber = 1;
    private void Awake()
    {
        context = GetComponent<DeliveryNpcContext>();
        npcView = GetComponent<DeliveryNpcView>();

        menuSchema = GenerateRandomMenu();

    }

    public void Interact()
    {
        if (isDisplaying)
        {
            CreateOrder();
            Destroy(gameObject);
            return;
        }

        Say($"배달주문이요! {menuSchema.name} 하나 부탁드릴게요");
        isDisplaying = true;
    }

    void OnDestroy()
    {
        if (context.speechBubble != null)
            Destroy(context.speechBubble);
    }


    private void CreateOrder()
    {
        string questId = Guid.NewGuid().ToString();

        GameSessionRoot.Instance?.Order.GenerateOrder(menuSchema, questId, npcView.NpcId);
        DeliveryNpcDialogueInteraction.UnlockMenuRecipes(menuSchema);
        CreateReceipt();
    }

    private void CreateReceipt()//@@@@@@@@@@@@@이런건 좀 분리 필요해보이긴 한다.....******
    {
        GameObject receipt = Instantiate(context.receiptPrefab);

        Receipt receiptScript = receipt.GetComponent<Receipt>();
        OrderTicketModel ticketModel = receipt.GetComponent<OrderTicketModel>();

        receiptScript.Set(menuSchema, true);
        ticketModel.SetMenu(menuSchema);
    }

    private void Say(string message)
    {
        context.speechBubble = Instantiate(context.speechBubblePrefab);
        context.speechBubbleScript = context.speechBubble.GetComponent<SpeechBubble>();

        context.speechBubbleScript.setContents(message);
        context.speechBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);
    }

    private MenuSchema GenerateRandomMenu()
    {
        List<MenuSchema> menus = new List<MenuSchema>
        {
            new MenuSchema(
                "임시정식 A",
                -1,
                SearchDataUtil.GetFoodDataById("I034"),
                new List<FoodData>
                {
                    SearchDataUtil.GetFoodDataById("I046"),
                    SearchDataUtil.GetFoodDataById("I058"),
                    SearchDataUtil.GetFoodDataById("I062")
                }
            ),
        };

        MenuSchema picked = menus[Random.Range(0, menus.Count)];
        picked.orderNumber = nextOrderingNumber++;
        return picked;
    }
}
