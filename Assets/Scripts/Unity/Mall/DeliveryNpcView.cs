using UnityEngine;

[RequireComponent(typeof(DeliveryNpcInteraction))]
[RequireComponent(typeof(SpriteRenderer))]
public class DeliveryNpcView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("이 NPC에 적용할 데이터. 인스펙터에서 drag.")]
    [SerializeField] private DeliveryNpcData npcData;
    [SerializeField] private int sortingOrder = 5;
    [SerializeField] private GameObject speechBubblePrefab;
    private string npcId;
    private string groupId;
    private string characterName;
    private string prerequisiteGroupId;

    public string NpcId => npcId;
    public string GroupId => groupId;
    public string PrerequisiteGroupId => prerequisiteGroupId;
    public Sprite Sprite => spriteRenderer?.sprite;
    public string CharacterName => characterName;

    void Awake()
    {
        if (npcData != null)
            Init(npcData);
    }

    /// <summary>인스펙터 데이터 적용. transform은 씬 배치값 그대로 — position을 덮어쓰지 않는다.</summary>
    public void Init(DeliveryNpcData data, DeliveryNpcState? stateOverride = null)
    {
        if (data == null) return;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = data.sprite;
        spriteRenderer.sortingOrder = sortingOrder;

        npcId = data.id;
        groupId = data.groupId;
        characterName = data.characterName;
        prerequisiteGroupId = data.prerequisiteGroupId;

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
                    dialogue.Init(groupId, prerequisiteGroupId, npcId, characterName, spriteRenderer.sprite);
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


#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
