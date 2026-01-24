using UnityEngine;

public class DeliveryNpcReceiptInteraction
    : MonoBehaviour, INpcInteraction
{
    private bool hasReceived = false;

    public void Interact()
    {
        if (hasReceived) return;

        //if(나의 npcId로 등록된 주문이 있고 그것이 성공적으로 완성된 음식이라면)
        //OrderManager.Instance.ConsumeBento(questID);

        Say("아 잘 먹을게요!");

        hasReceived = true;
        Destroy(gameObject, 0.5f);
    }

    private void Say(string message)
    {
        Debug.Log(message);
        // 나중에 말풍선 연결
    }
}
