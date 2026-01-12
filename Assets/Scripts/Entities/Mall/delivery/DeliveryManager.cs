using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public GameObject receiptPrefab;

    private int deliveryNum = 3;

    private void Start()
    {
        //완전 임시 테스트용@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        TempSearchFoodUsecase tempSearchFoodUsecase = new TempSearchFoodUsecase();

        CreateReceipt(new MenuSchema(
            "도시락 정식 A",
            -1,
            tempSearchFoodUsecase.Search("I034"),
            new List<FoodData>
            {
                            tempSearchFoodUsecase.Search("I046"),
                            tempSearchFoodUsecase.Search("I058"),
                            tempSearchFoodUsecase.Search("I062")
            }
        ));
        Debug.Log("완전임시코드 수행함!!!");
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

        //GenerateDeliveryCustomers();
    }


    public GameObject CreateReceipt(MenuSchema menuSchema)
    {
        GameObject receipt = Instantiate(receiptPrefab);

        Receipt receiptUI = receipt.GetComponent<Receipt>();
        OrderTicketModel ticket = receipt.GetComponent<OrderTicketModel>();

        receiptUI.Set(menuSchema);
        ticket.menuSchema = menuSchema;

        return receipt;
    }
}
