using UnityEngine;

public class DeliveryNpcInteraction : MonoBehaviour
{
    private INpcInteraction currentInteraction;

    public void SetInteraction(INpcInteraction interaction)
    {
        currentInteraction = interaction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        currentInteraction?.Interact();
    }
}
