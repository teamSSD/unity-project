using UnityEngine;

public class FarmPathInteraction : MonoBehaviour
{
    private bool isPlayerNear = false;

    private void Start()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.RegisterCollider("farm-path", GetComponent<Collider2D>());
#endif
    }

    private void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.UnregisterCollider("farm-path", GetComponent<Collider2D>());
#endif
    }

    void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
            SceneLoader.SetMallReturnPosition(transform.position);
            SceneLoader.LoadScene(SceneNames.Garden);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(Tags.Player))
        {
            isPlayerNear = true;
            InteractPromptUI.Show("(press spacebar to go to farm)");
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag(Tags.Player))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
        }
    }
}
