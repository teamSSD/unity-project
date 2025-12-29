using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DoorInteraction : MonoBehaviour
{
    public GameObject player;
    public Vector2 destination;
    public GameObject interactText;

    private bool isPlayerNear = false;

    void Start()
    {
        interactText.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Space))
        {
            GoThroughDoor();
        }
    }

    void GoThroughDoor()
    {
        player.transform.position = new Vector3(destination.x, player.transform.position.y, 0f);
        Camera.main.transform.position = new Vector3(destination.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        isPlayerNear = false;//@@
        interactText.SetActive(false);//@@
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            interactText.SetActive(false);
        }
    }
}
