using UnityEngine;

public class DeliveryNpcInteraction : MonoBehaviour
{
    private INpcInteraction currentInteraction;
    private bool isPlayerNear;
    private bool interactCooldown;

    public void SetInteraction(INpcInteraction interaction)
    {
        currentInteraction = interaction;
    }

    private void Update()
    {
        if (interactCooldown) { interactCooldown = false; return; }

        if (isPlayerNear && currentInteraction != null && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            currentInteraction.Interact();
            interactCooldown = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(Tags.Player)) return;

        isPlayerNear = true;
        if (currentInteraction != null)
            InteractPromptUI.Show("*press spacebar to talk*");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(Tags.Player)) return;

        isPlayerNear = false;
        InteractPromptUI.Hide();
    }
}
