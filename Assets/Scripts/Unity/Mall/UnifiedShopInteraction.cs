using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class UnifiedShopInteraction : MonoBehaviour
{
    [SerializeField] private ShopUIAdapter.Tab targetTab = ShopUIAdapter.Tab.Item;

    private bool isPlayerNear = false;

    private void Start()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.RegisterCollider($"shop.{targetTab}", GetComponent<Collider2D>());
#endif
    }

    private void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            // isPlayerNear는 그대로 유지 — Shop은 씬 전환이 아닌 UI 오버레이라 플레이어는 트리거 안에 계속 있음.
            // (OpenShop이 UILockManager.Lock(Shop) 하므로 중복 오픈은 이 함수의 조건문에서 자동 차단.)
            InteractPromptUI.Hide();
            UIFlowController.TryOpenShop(targetTab);
        }
    }

    private void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.UnregisterCollider($"shop.{targetTab}", GetComponent<Collider2D>());
#endif
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
