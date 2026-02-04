using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CustomerManager : MonoBehaviour
{
    public GameObject statManager;
    public GameObject orderingCustomerPrefab;
    public GameObject waitingCustomerPrefab;
    public GameObject TakingCustomerPrefab;
    public GameObject receiptPrefab;
    public Canvas canvas;
    public Canvas worldCanvas;
    public static bool isOpen = true;
    [SerializeField] private AudioClip doorSfx;
    [SerializeField] private List<CustomerData> customerDataList;

    private static int waitingCustomerCount = 0;
    private static List<int> waitingPoses = new List<int> {0, 1, 2, 3, 4};
    private List<MenuSchema> salesMenus;
    private int nextOrderingNumber = 1;
    private float whenNextVisit;
    private float timer = 0;
    private GameObject orderingCustomer;

    void Awake()
    {
        ISelectMenu iSelectMenu = new MockMenuProvider();
        salesMenus = iSelectMenu.GetTodaysMenu();

        StatsSystem.SetStamina(100);

        statManager.GetComponent<StatManager>().onTimeEnd += () => 
        {
            isOpen = false;
            if (orderingCustomer != null) Destroy(orderingCustomer);
        };

        CustomerEntry.orderingCustomerPrefab = orderingCustomerPrefab;
        CustomerEntry.receiptPrefab = receiptPrefab;
        CustomerEntry.waitingCustomerPrefab = waitingCustomerPrefab;
        CustomerEntry.TakingCustomerPrefab = TakingCustomerPrefab;
        CustomerEntry.worldCanvas = worldCanvas;
        CustomerEntry.OnGameEnd += gameEnd;
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
        whenNextVisit = RandomNormal.Get(5, 3); // 실제 - 45, 15
        
        if (orderingCustomer != null || waitingCustomerCount >= 5) return;
        CustomerEntry customerEntry = new CustomerEntry(pickRandomMenu(), pickRandomCustomerData());
        orderingCustomer = customerEntry.GenerateCustomer();
        SoundManager.Instance.Play2DSFX(doorSfx, 0.7f);
    }

    public static int PickWaitingPosition()
    {
        int index = RandomGeneral.Pick(waitingPoses);
        waitingPoses.Remove(index);
        return index;
    }

    public static void ReleaseWaitingPosition(int index)
    {
        waitingPoses.Add(index);
    }

    private MenuSchema pickRandomMenu()
    {
        MenuSchema menuSchema = salesMenus[Random.Range(0, salesMenus.Count)];
        menuSchema.orderNumber = nextOrderingNumber;
        nextOrderingNumber++;
        return menuSchema;
    }

    private CustomerData pickRandomCustomerData()
    {
        return customerDataList[Random.Range(0, customerDataList.Count)];
    }

    private void gameEnd()
    {
        Debug.Log("Game End");
    }

    private class CustomerEntry
    {
        public static GameObject orderingCustomerPrefab;
        public static GameObject receiptPrefab;
        public static GameObject waitingCustomerPrefab;
        public static GameObject TakingCustomerPrefab;
        public static Canvas worldCanvas;
        public static event Action OnGameEnd;

        private MenuSchema menuSchema;
        private CustomerData customerData;
        private static List<OrderTicketModel> reciepts = new List<OrderTicketModel>();
        
        public CustomerEntry (MenuSchema menuSchema, CustomerData customerData)
        {
            this.menuSchema = menuSchema;
            this.customerData = customerData;
        }

        public GameObject GenerateCustomer()
        {
            CustomerEntry customerEntry = new CustomerEntry(menuSchema, customerData);

            GameObject ordering = Instantiate(orderingCustomerPrefab);
            OrderingCustomer script = ordering.GetComponent<OrderingCustomer>();
            script.Inject(customerEntry.menuSchema, customerEntry.customerData);
            
            script.onExit += customerEntry.OnOrder;
            script.onExit += () => Destroy(ordering);
            return ordering;
        }

        private OrderTicketModel GenerateReciept()
        {
            GameObject receipt = Instantiate(receiptPrefab);
            OrderTicketModel orderTicketModel = receipt.GetComponent<OrderTicketModel>();
            Receipt recieptScript = receipt.GetComponent<Receipt>();
            orderTicketModel.SetMenu(menuSchema);
            recieptScript.Set(menuSchema);
            reciepts.Add(orderTicketModel);
            Vector3 newPos = getNewRecieptPos(reciepts.Count-1);
            orderTicketModel.SetDefaultPosition(newPos);
            receipt.transform.position = newPos + new Vector3(0, -2f, 0);

            return orderTicketModel;
        }

        private WaitingCustomer GenerateWaitingCustomer(int index)
        {
            GameObject waitingCustomer = Instantiate(waitingCustomerPrefab);
            WaitingCustomer waitingCustomerScript = waitingCustomer.GetComponent<WaitingCustomer>();
            waitingCustomer.transform.position = getNewWaitingPos(index);
            waitingCustomerScript.inject(worldCanvas, customerData);

            return waitingCustomerScript;
        }

        private void checkEnd()
        {
            if (!isOpen && reciepts.Count == 0)
            {
                OnGameEnd?.Invoke();
            }
        }

        private void OnOrder()
        {
            int index = PickWaitingPosition();
            waitingCustomerCount++;

            WaitingCustomer waitingCustomer = GenerateWaitingCustomer(index);
            OrderTicketModel reciept = GenerateReciept();
            Action arrange = () => {
                reciepts.Remove(reciept);
                waitingCustomer.DestroyObject();
                reciept.DestroyObject();
                arangeWaiting(index);
                checkEnd();
            };
            waitingCustomer.onExit += () =>
            {
                arrange();
                OnExit();
            };
            reciept.OnAttached += () => waitingCustomer.stopTimer = true;
            reciept.onTake += (main, sides, position) => {
                arrange();
                OnTake(main, sides, position);
            };
        }

        private void arangeWaiting(int index)
        {
            ReleaseWaitingPosition(index);
            
            for (int i = 0; i < reciepts.Count; i++)
            {
                reciepts[i].SetDefaultPosition(getNewRecieptPos(i));
            }
        }

        private void OnExit()
        {
            GameObject takingCustomer = Instantiate(TakingCustomerPrefab);
            takingCustomer.transform.position = new Vector3(-11.63f, -0.85f, 0);
            TakingCustomer script = takingCustomer.GetComponent<TakingCustomer>();
            script.customerData = customerData;
            script.exit();
            Destroy(takingCustomer, 3f);
        }

        private void OnTake(FoodSchema mainMenu, List<FoodSchema> sideMenus, Vector3 position)
        {
            GameObject takingCustomer = Instantiate(TakingCustomerPrefab);
            takingCustomer.transform.position = position + new Vector3(0,0,0);
            TakingCustomer script = takingCustomer.GetComponent<TakingCustomer>();
            script.customerData = customerData;
            script.take(menuSchema, mainMenu, sideMenus);
            Destroy(takingCustomer, 3f);
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

        private Vector3 getNewRecieptPos(int index)
        {
            Vector3 std = new Vector3(7.65f,0.94f, 0);
            Vector3 off = new Vector3(0, -2.38f, 0);
            return std + off * index;
        }
    }
}