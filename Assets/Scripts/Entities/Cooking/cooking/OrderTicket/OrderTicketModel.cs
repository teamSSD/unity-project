using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OrderTicketBehavior))]
[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
public class OrderTicketModel : MonoBehaviour
{
    public OrderTicketBehavior BehaviorInstance { get; private set; }
    public GameObject TakingCustomerPrefab;
    public GameObject waitingCustomer;
    public MenuSchema menuSchema;
    private static System.Random rand = new System.Random();
    private ScanColliderUtil scanColliderUtil;
    private ClickStateUtil clickStateUtil;
    void Awake()
    {
        BehaviorInstance = GetComponent<OrderTicketBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragEnd += AddToBento;
    }

    void OnDestroy()
    {
        clickStateUtil.OnDragEnd -= AddToBento;
    }
    public void AddToBento()
    {
        BentoModel collision = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (collision != null)
        {
            bool affected = collision.AddOrderTicket(this);
            if (affected)
            {
                waitingCustomer.GetComponent<WaitingCustomer>().stopTimer = true;
                FoodSchema main = collision.getFoodList()[0];
                List<FoodSchema> sides = collision.getFoodList();
                sides.RemoveAt(0);
                StartCoroutine(buy(menuSchema, main, sides, getRandomNormal(0.5f, 1.5f), collision));
                GetComponent<SpriteRenderer>().enabled = false;
            }
        }
    }

    private IEnumerator buy(MenuSchema menuSchema, FoodSchema mainMenu, List<FoodSchema> sideMenus, float time, BentoModel collision)
    {
        yield return new WaitForSeconds(time);

        Destroy(waitingCustomer);
        Destroy(collision.gameObject);
        GameObject generated = Instantiate(TakingCustomerPrefab);
        generated.GetComponent<TakingCustomer>().take(menuSchema, mainMenu, sideMenus);
        Destroy(gameObject);
    }

    private float getRandomNormal(float minVal, float maxVal)
    {
        double mean = (minVal + maxVal) / 2.0;
        double stdDev = (maxVal - minVal) / 6.0;

        while (true) {
            double u1 = 1.0 - rand.NextDouble(); 
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            
            double val = mean + stdDev * randStdNormal;

            if (val >= minVal && val <= maxVal) {
                return (float) val;
            }
        }
    }
}