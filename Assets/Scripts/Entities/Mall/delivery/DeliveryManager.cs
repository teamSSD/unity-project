using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DeliveryManager : MonoBehaviour
{
    public GameObject deliveryCustomerPrefab;
    public bool isOpen = true;
    public int deliveryOrderCount = 3;

    private List<MenuSchema> salesMenus;
    private int nextOrderingNumber = 1;


    void Awake()
    {
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

    private void GenerateCustomer()
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

        // 주문 완료 → 즉시 퇴장
        script.onExit += () =>
        {
            Destroy(ordering);
        };
    }


    private MenuSchema pickRandomMenu()
    {
        MenuSchema menuSchema = salesMenus[Random.Range(0, salesMenus.Count)];
        menuSchema.orderNumber = nextOrderingNumber;
        nextOrderingNumber++;
        return menuSchema;
    }
}
