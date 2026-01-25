using System.Linq;
using UnityEngine;

public class DeliveryNpcReceiptInteraction
    : MonoBehaviour, INpcInteraction
{
    private bool hasReceived = false;
    private DeliveryNpcContext context;
    private DeliveryNpcView npcView;

    private void Awake()
    {
        context = GetComponent<DeliveryNpcContext>();
        npcView = GetComponent<DeliveryNpcView>();

    }
    public void Interact()
    {
        if (hasReceived) return;

        // 1. 이 NPC(npcId)에 해당하는 주문 찾기
        DeliveryOrderData myOrder =
            OrderManager.Instance.GetOrders()
                .FirstOrDefault(order =>
                    order.npcId == npcView.NpcId &&
                    order.state == DeliveryOrderState.Cooked);

        // 2. 주문이 없거나 아직 요리가 안 됐다면
        if (myOrder == null)
        {
            Say("아직 제 음식이 안 온 것 같은데요?");
            return;
        }

        // 3. 음식 수령 처리
        OrderManager.Instance.ConsumeBento(myOrder.questId);

        Say("아 잘 먹을게요!");

        hasReceived = true;
        Destroy(gameObject, 0.5f);
    }

    private void Say(string message)
    {
        context.speechBubble = Instantiate(context.speechBubblePrefab);
        context.speechBubbleScript = context.speechBubble.GetComponent<SpeechBubble>();

        context.speechBubbleScript.setContents(message);
        context.speechBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);
    }

}
