using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(DeliveryNpcView))]
public class DeliveryNpcReceiptInteraction
    : MonoBehaviour, INpcInteraction
{
    private GameObject speechBubblePrefab;

    private bool hasReceived = false;
    private DeliveryNpcView npcView;
    private GameObject speechBubble;

    private void Awake()
    {
        npcView = GetComponent<DeliveryNpcView>();
    }

    public void Init(GameObject speechBubble)
    {
        speechBubblePrefab = speechBubble;
    }

    public void Interact()
    {
        if (hasReceived) return;

        // 1. 이 NPC(npcId)에 해당하는 주문 찾기
        DeliveryOrderData myOrder =
            GameSessionRoot.Instance?.Order.GetOrders()
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
        DeliverSequenceAsync(myOrder).Forget();
    }

    private async UniTaskVoid DeliverSequenceAsync(DeliveryOrderData order)
    {
        var ct = this.GetCancellationTokenOnDestroy();
        Say("음식이 왔군요! 확인 중...");

        await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: ct);

        int reward = GameSessionRoot.Instance?.Order.ConsumeBento(order.questId) ?? 0;
        Say($"감사합니다! ({reward}원)");

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: ct);

        Destroy(gameObject);
    }

    private void Say(string message)
    {
        if (speechBubble != null)
            Destroy(speechBubble);

        speechBubble = Instantiate(speechBubblePrefab);
        speechBubble.GetComponent<SpeechBubble>().setContents(message);
        speechBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);
    }
}
