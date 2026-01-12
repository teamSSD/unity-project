using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CustomerManager : MonoBehaviour
{
    public GameObject statManager;
    public GameObject orderingCustomerPrefab;
    public GameObject waitingCustomerPrefab;
    public GameObject receiptPrefab;
    public Canvas canvas;
    public Canvas worldCanvas;
    public bool isOpen = true;

    private List<EntryDto> waitingCustomers;
    private List<int> waitingPoses = new List<int> {0, 1, 2, 3, 4};
    private List<MenuSchema> salesMenus;
    private int nextOrderingNumber = 1;
    private float whenNextVisit;
    private float timer = 0;
    private GameObject orderingCustomer;

    void Awake()
    {
        waitingCustomers = new List<EntryDto>();

        TempSearchFoodUsecase tempSearchFoodUsecase = new TempSearchFoodUsecase();

        salesMenus = new List<MenuSchema>
        {
            new MenuSchema(
                "?꾩떆???뺤떇 A",
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

        StatsSystem.SetStamina(100);

        statManager.GetComponent<StatManager>().onTimeEnd
            += () =>
                {
                    isOpen = false;
                    if (orderingCustomer != null) Destroy(orderingCustomer);
                };
    }

    void Start()
    {
        whenNextVisit = RandomNormal.Get(5, 3);
    }

    void Update()
    {
        if (isOpen)
        {
            handleOrderingCustomers();
        }
    }

    private void handleOrderingCustomers()
    {
        timer += Time.deltaTime;

        if (timer < whenNextVisit) return;
        timer -= whenNextVisit;
        whenNextVisit = RandomNormal.Get(5, 3); // ?ㅼ젣 - 45, 15
        if (orderingCustomer != null || waitingCustomers.Count >= 5) return;
        orderingCustomer = GenerateCustomer();
    }

    private GameObject GenerateCustomer()
    {
        GameObject ordering = Instantiate(orderingCustomerPrefab);
        OrderingCustomer script = ordering.GetComponent<OrderingCustomer>();
        script.menuSchema = pickRandomMenu();
        script.onExit += () =>
        {
            orderingCustomer = null;
            registRecieptAndWaitingCustomer(script.menuSchema);
        };
        return ordering;
    }

    private Vector3 getNewWaitingPos(int index)
    {
        Vector3 std = new Vector3(-11.37f, 0.5f, 0);
        Vector3 offset = new Vector3(2, 0, 0);
        Vector3 difference = new Vector3(0.8f, 0.5f, 0);
        Vector3 randomVector = new Vector3(
                difference.x * RandomNormal.Range(-1, 1), 
                difference.y * RandomNormal.Range(-1, 1), 
                0
        );

        return std + offset * index + randomVector;
    }

    private void registRecieptAndWaitingCustomer(MenuSchema menuSchema)
    {
        GameObject receipt = Instantiate(receiptPrefab);
        OrderTicketModel orderTicketModel = receipt.GetComponent<OrderTicketModel>();
        Receipt recieptScript = receipt.GetComponent<Receipt>();
        GameObject waitingCustomer = Instantiate(waitingCustomerPrefab);
        WaitingCustomer waitingCustomerScript = waitingCustomer.GetComponent<WaitingCustomer>();
        
        int index = RandomGeneral.Pick(waitingPoses);
        waitingPoses.Remove(index);
        waitingCustomer.transform.position = getNewWaitingPos(index);
        waitingCustomerScript.inject(worldCanvas);
        waitingCustomerScript.onExit += () => {waitingPoses.Add(index);};

        recieptScript.Set(menuSchema);
        orderTicketModel.waitingCustomer = waitingCustomer;
        orderTicketModel.menuSchema = menuSchema;
        waitingCustomerScript.Receipt = receipt;
        orderTicketModel.onTake += () => StartCoroutine(RunNextFrame(() =>
        {
            clearWaitingCustomer();
            checkEnd();
        }));
        waitingCustomerScript.onExit += () => StartCoroutine(RunNextFrame(() =>
        {
            clearWaitingCustomer();
            checkEnd();
        }));

        addWaitingCustomer(waitingCustomer, receipt);
    }

    private void checkEnd()
    {
        if (!isOpen && waitingCustomers.Count == 0)
        {
            gameEnd();
        }
    }

    private MenuSchema pickRandomMenu()
    {
        MenuSchema menuSchema = salesMenus[Random.Range(0, salesMenus.Count)];
        menuSchema.orderNumber = nextOrderingNumber;
        nextOrderingNumber++;
        return menuSchema;
    }

    private void addWaitingCustomer(GameObject waitingCustomer, GameObject receipt)
    {
        waitingCustomers.Add(new EntryDto(waitingCustomer, receipt));
        clearWaitingCustomer();
    }

    private void clearWaitingCustomer()
    {
        Vector3 std = new Vector3(7.65f,0.94f, 0);
        Vector3 off = new Vector3(0, -2.38f, 0);
        waitingCustomers.RemoveAll(entry =>
                entry.waitingCustomer == null
                        || entry.reciept == null
                        || entry.reciept.GetComponent<OrderTicketModel>().IsAttached);

        for (int i = 0; i < waitingCustomers.Count; i++)
        {
            waitingCustomers[i].reciept.GetComponent<OrderTicketModel>()
                    .BehaviorInstance.defaultPosition = std + off * i;
        }
    }

    private void gameEnd()
    {
        Debug.Log("Game End");
    }

    IEnumerator RunNextFrame(Action action)
    {
        yield return null; // 1?꾨젅???湲?
        action?.Invoke();  // ?먮옒 ?⑥닔 ?ㅽ뻾
    }

    private class EntryDto
    {
        public GameObject waitingCustomer;
        public GameObject reciept;
        public EntryDto() {}
        public EntryDto(GameObject waitingCustomer, GameObject reciept)
        {
            this.waitingCustomer = waitingCustomer;
            this.reciept = reciept;
        }
    }
}