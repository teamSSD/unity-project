using UnityEngine;

public class DeliveryNpcView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string npcId;
    [SerializeField] private GameObject speechBubblePrefab;
    private string groupId;
    private string characterName;

    public string NpcId => npcId;
    public string GroupId => groupId;
    public Sprite Sprite => spriteRenderer?.sprite;
    public string CharacterName => characterName;

    public void Init(DeliveryNpcData data, DeliveryNpcState? stateOverride = null)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = data.sprite;
        spriteRenderer.sortingOrder = 5;

        transform.position = data.position;
        npcId = data.id;
        groupId = data.groupId;
        characterName = data.characterName;

        ApplyState(stateOverride ?? data.state);
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
                if (!string.IsNullOrEmpty(groupId))
                {
                    var dialogue = gameObject.AddComponent<DeliveryNpcDialogueInteraction>();
                    dialogue.Init(groupId);
                }
                else
                {
                    var casual = gameObject.AddComponent<CasualNpcInteraction>();
                    casual.Init(npcId, characterName, spriteRenderer.sprite);
                    interactionRoot.SetInteraction(casual);
                }
                break;

            case DeliveryNpcState.WaitingReceipt:
                spriteRenderer.color = Color.yellow;
                var receipt = gameObject.AddComponent<DeliveryNpcReceiptInteraction>();
                receipt.Init(speechBubblePrefab);
                interactionRoot.SetInteraction(receipt);
                break;

            case DeliveryNpcState.Completed:
                spriteRenderer.color = Color.gray;
                interactionRoot.SetInteraction(null);
                break;
        }
    }

}
