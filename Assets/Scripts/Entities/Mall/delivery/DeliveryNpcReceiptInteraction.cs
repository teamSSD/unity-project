using UnityEngine;

public class DeliveryNpcReceiptInteraction
    : MonoBehaviour, INpcInteraction
{
    private bool hasReceived = false;

    public void Interact()
    {
        if (hasReceived) return;

        //OrderManager.Instance.ConsumeCurrentOrder();

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
