using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public GameObject receiptPrefab;

    /// <summary>
    /// 주문 완료 시 호출
    /// </summary>
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
