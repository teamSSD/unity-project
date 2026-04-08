using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public GameObject player;
    public Vector2 destination;

    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            GoThroughDoor();
        }
    }

    void GoThroughDoor()
    {
        player.transform.position = new Vector3(destination.x, player.transform.position.y, 0f);
        Camera.main.transform.position = new Vector3(destination.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        isPlayerNear = false;
        InteractPromptUI.Hide();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            InteractPromptUI.Show("(press spacebar to go in)");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
        }
    }
}
