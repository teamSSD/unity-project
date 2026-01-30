using UnityEngine;

public class ShopInteraction : MonoBehaviour
{
    public GameObject player;
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
            OpenGardenShop();
        }
    }

    void OpenGardenShop()
    {
        isPlayerNear = false;
        interactText.SetActive(false);
        GardenShopManager.Instance.OpenGardenShop();
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
