using System.Collections;
using System.Linq;
using UnityEngine;

public class DeliveryNpcReceiptInteraction
    : MonoBehaviour, INpcInteraction
{
    private bool hasReceived = false;
    private DeliveryNpcView npcView;
    private GameObject speechBubble;

    private void Awake()
    {
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

        hasReceived = true;
        StartCoroutine(DeliverSequence(myOrder));
    }

    private IEnumerator DeliverSequence(DeliveryOrderData order)
    {
        Say("음식이 왔군요! 확인 중...");

        yield return new WaitForSeconds(2f);

        int reward = OrderManager.Instance.ConsumeBento(order.questId);
        Say($"감사합니다! ({reward}원)");

        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
    }

    private void Say(string message)
    {
        if (speechBubble != null)
            Destroy(speechBubble);

        var prefab = Resources.Load<GameObject>("Prefabs/cooking/SpeechBubble");
        speechBubble = Instantiate(prefab);
        speechBubble.GetComponent<SpeechBubble>().setContents(message);
        speechBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);
    }
}
