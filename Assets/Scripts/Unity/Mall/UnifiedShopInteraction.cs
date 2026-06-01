using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class UnifiedShopInteraction : MonoBehaviour
{
    [SerializeField] private ShopUIAdapter.Tab targetTab = ShopUIAdapter.Tab.Item;

    private bool isPlayerNear = false;

    private void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
            ShopUIAdapter.Instance.OpenShop(targetTab);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Tags.Player))
        {
            isPlayerNear = true;
            InteractPromptUI.Show("(press spacebar to open shop)");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(Tags.Player))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
