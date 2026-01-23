using UnityEngine;

public class DeliveryNpcView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Init(DeliveryNpcCsvData data)
    {
        spriteRenderer.sprite =
            Resources.Load<Sprite>(data.spritePath);

        transform.position = data.position;

        ApplyState(data.state);
    }

    private void ApplyState(DeliveryNpcState state)
    {
        switch (state)
        {
            case DeliveryNpcState.Orderable:
                spriteRenderer.color = Color.white;
                break;

            case DeliveryNpcState.WaitingReceipt:
                spriteRenderer.color = Color.yellow;
                break;

            case DeliveryNpcState.Completed:
                spriteRenderer.color = Color.gray;
                break;
        }
    }
}
