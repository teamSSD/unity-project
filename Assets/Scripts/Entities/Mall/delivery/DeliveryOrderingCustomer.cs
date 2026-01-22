using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ClickStateUtil))]
public class DeliveryOrderingCustomer : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject speechBubblePrefab;
    public GameObject receiptPrefab;

    [Header("상태")]
    private bool isDisplaying = false;
    private GameObject speechBubble;
    private SpeechBubble speechBubbleScript;

    private MenuSchema menuSchema;

    private static int nextOrderingNumber = 1;

    void Awake()
    {
        menuSchema = GenerateRandomMenu();
    }

    void OnDestroy()
    {
        if (speechBubble != null)
            Destroy(speechBubble);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ClickRoutine();
    }

    private void ClickRoutine()
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

    private void CreateOrder()
    {
        string questId = Guid.NewGuid().ToString();

        OrderManager.Instance.GenerateOrder(menuSchema, questId);
        CreateReceipt();
    }

    private void CreateReceipt()
    {
        GameObject receipt = Instantiate(receiptPrefab);

        Receipt receiptScript = receipt.GetComponent<Receipt>();
        OrderTicketModel ticketModel = receipt.GetComponent<OrderTicketModel>();

        receiptScript.Set(menuSchema);
        ticketModel.menuSchema = menuSchema;
    }

    private void Say(string message)
    {
        speechBubble = Instantiate(speechBubblePrefab);
        speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();

        speechBubbleScript.setContents(message);
        speechBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);
    }

    private MenuSchema GenerateRandomMenu()
    {
        TempSearchFoodUsecase tempSearchFoodUsecase = new TempSearchFoodUsecase();

        List<MenuSchema> menus = new List<MenuSchema>
        {
            new MenuSchema(
                "임시정식 A",
                -1,
                tempSearchFoodUsecase.Search("I034"),
                new List<FoodData>
                {
                    tempSearchFoodUsecase.Search("I046"),
                    tempSearchFoodUsecase.Search("I058"),
                    tempSearchFoodUsecase.Search("I062")
                }
            ),
        };

        MenuSchema picked = menus[Random.Range(0, menus.Count)];
        picked.orderNumber = nextOrderingNumber++;
        return picked;
    }
}
