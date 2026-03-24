using UnityEngine;

public class DeliveryNpcView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string npcId;

    public string NpcId => npcId;

    public void Init(DeliveryNpcCsvData data)
    {
        spriteRenderer.sprite =
            Resources.Load<Sprite>(data.spritePath);

        transform.position = data.position;
        npcId = data.npcId;

        ApplyState(data.state);
    }
    private void ApplyState(DeliveryNpcState state)
    {
        var interactionRoot = GetComponent<DeliveryNpcInteraction>();

        // 1. 기존 행동 제거
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c is INpcInteraction)
                Destroy(c);
        }

        // 2. 외형 + 행동 동시 적용
        switch (state)
        {
            case DeliveryNpcState.Orderable:
                spriteRenderer.color = Color.white;
                interactionRoot.SetInteraction(
                    gameObject.AddComponent<DeliveryNpcOrderInteraction>());
                break;

            case DeliveryNpcState.WaitingReceipt:
                spriteRenderer.color = Color.yellow;
                interactionRoot.SetInteraction(
                    gameObject.AddComponent<DeliveryNpcReceiptInteraction>());
                break;

            case DeliveryNpcState.Completed:
                spriteRenderer.color = Color.gray;
                interactionRoot.SetInteraction(null);
                break;
        }
    }

}
