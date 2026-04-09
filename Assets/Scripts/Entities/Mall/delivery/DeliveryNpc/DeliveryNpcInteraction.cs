using UnityEngine;

public class DeliveryNpcInteraction : MonoBehaviour
{
    private INpcInteraction currentInteraction;
    private bool isPlayerNear;

    public void SetInteraction(INpcInteraction interaction)
    {
        currentInteraction = interaction;
    }

    private void Update()
    {
        if (isPlayerNear && currentInteraction != null && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            currentInteraction.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = true;
        if (currentInteraction != null)
            InteractPromptUI.Show("*press spacebar to interact*");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNear = false;
        InteractPromptUI.Hide();
    }
}
