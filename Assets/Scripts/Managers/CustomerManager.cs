using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public GameObject orderingCustomerPrefab;
    public GameObject waitingCustomerPrefab;
    public GameObject takingCustomerPrefab;
    public GameObject receiptPrefab;
    public Canvas canvas;

    private List<GameObject> orderingQueue;
    private List<GameObject> watingCustomers;
    private List<MenuSchema> salesMenus;
    private int nextOrderingNumber = 1;
    private float whenNextVisit;
    private float timer = 0;
    private GameObject front = null;

    void Awake()
    {
        orderingQueue = new List<GameObject>();
        watingCustomers = new List<GameObject>();

        TempSearchFoodUsecase tempSearchFoodUsecase = new TempSearchFoodUsecase();

        salesMenus = new List<MenuSchema>
        {
            new MenuSchema(
                "도시락 정식 A",
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
        whenNextVisit = RandomNormal.Get(5, 3);
    }

    void Update()
    {
        handleOrderingCustomers();
    }

    private void arrangeQueue()
    {
        Vector3 std = new Vector3(2.61f, -0.86f, 0);
        Vector3 off = new Vector3(0.3f, 0.2f, 0.1f);
        if (orderingQueue.Count == 0) return;
        orderingQueue[0].GetComponent<OrderingCustomer>().readyToOrder = true;
        for (int i = 0; i < orderingQueue.Count; i++)
        {
            orderingQueue[i].gameObject.transform.position = std + (off * i);
        }
    }

    private void enQueue(GameObject orderingCustomer)
    {
        orderingQueue.Add(orderingCustomer);
        arrangeQueue();
    }

    private void handleOrderingCustomers()
    {
        timer += Time.deltaTime;

        if (timer < whenNextVisit) return;
        timer -= whenNextVisit;
        whenNextVisit = RandomNormal.Get(45, 15);
        if (orderingQueue.Count >= 5) return;
        enQueue(GenerateCustomer());
    }

    private GameObject GenerateCustomer()
    {
        GameObject waiting = Instantiate(orderingCustomerPrefab);
        OrderingCustomer script = waiting.GetComponent<OrderingCustomer>();
        script.menuSchema = pickRandomMenu();
        script.onExit += () =>
        {
            orderingQueue.Remove(waiting);
            arrangeQueue();
            registRecieptAndWaitingCustomer(script.menuSchema);
        };
        return waiting;
    }

    private void registRecieptAndWaitingCustomer(MenuSchema menuSchema)
    {
        GameObject receipt = Instantiate(receiptPrefab);
        OrderTicketModel orderTicketModel = receipt.GetComponent<OrderTicketModel>();
        Receipt recieptScript = receipt.GetComponent<Receipt>();
        GameObject waitingCustomer = Instantiate(waitingCustomerPrefab);
        WaitingCustomer waitingCustomerScript = waitingCustomer.GetComponent<WaitingCustomer>();
        recieptScript.Set(menuSchema);
        orderTicketModel.waitingCustomer = waitingCustomer;
        orderTicketModel.menuSchema = menuSchema;
        waitingCustomerScript.Receipt = receipt;
    }

    private MenuSchema pickRandomMenu()
    {
        MenuSchema menuSchema = salesMenus[Random.Range(0, salesMenus.Count)];
        menuSchema.orderNumber = nextOrderingNumber;
        nextOrderingNumber++;
        return menuSchema;
    }

    private void customerExit()
    {
        
    }

    private void customerTake(MenuSchema menuSchema)
    {
        
    }
}