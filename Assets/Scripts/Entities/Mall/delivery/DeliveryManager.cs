using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DeliveryManager : MonoBehaviour
{
    public GameObject deliveryCustomerPrefab;
    public GameObject receiptPrefab;
    public GameObject waitingCustomerPrefab;
    public bool isOpen = true;
    public int deliveryOrderCount = 3;

    private List<EntryDto> waitingCustomers;
    private List<MenuSchema> salesMenus;
    private int nextOrderingNumber = 1;
    private GameObject orderingCustomer;

    void Awake()
    {
        waitingCustomers = new List<EntryDto>();

        TempSearchFoodUsecase tempSearchFoodUsecase = new TempSearchFoodUsecase();

        salesMenus = new List<MenuSchema>
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
    }

    void Start()
    {
        if (!isOpen) return;

        // 호출되자마자 배달주문손님들 생성
        for (int i = 0; i < deliveryOrderCount; i++)
        {
            GenerateCustomer();
        }
    }

    private GameObject GenerateCustomer()
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(0f, 20f), // x: 0~20 랜덤
            -3f,                  // y: 바닥 좌표대로 고정
            0f                   
        );

        GameObject ordering = Instantiate(
            deliveryCustomerPrefab,
            spawnPos,
            Quaternion.identity
        );

        DeliveryOrderingCustomer script = ordering.GetComponent<DeliveryOrderingCustomer>();
        script.menuSchema = pickRandomMenu();

        script.onExit += () =>
        {
            orderingCustomer = null;
            registRecieptAndWaitingCustomer(script.menuSchema);
        };
        return ordering;
    }

    private void registRecieptAndWaitingCustomer(MenuSchema menuSchema)
    {
        GameObject receipt = Instantiate(receiptPrefab);
        OrderTicketModel orderTicketModel = receipt.GetComponent<OrderTicketModel>();
        Receipt recieptScript = receipt.GetComponent<Receipt>();

        GameObject waitingCustomer = Instantiate(waitingCustomerPrefab);

        recieptScript.Set(menuSchema);
        orderTicketModel.waitingCustomer = waitingCustomer;
        orderTicketModel.menuSchema = menuSchema;
        orderTicketModel.onTake += () => StartCoroutine(RunNextFrame(() =>
        {
            //clearWaitingCustomer();
            //checkEnd();
        }));
    }

    private MenuSchema pickRandomMenu()
    {
        MenuSchema menuSchema = salesMenus[Random.Range(0, salesMenus.Count)];
        menuSchema.orderNumber = nextOrderingNumber;
        nextOrderingNumber++;
        return menuSchema;
    }

    IEnumerator RunNextFrame(Action action)
    {
        yield return null;
        action?.Invoke();  
    }
    private class EntryDto
    {
        public GameObject reciept;
        public EntryDto() { }
        public EntryDto(GameObject reciept)
        {
            this.reciept = reciept;
        }
    }
}
