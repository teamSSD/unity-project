using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneTransitionInteraction : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string promptMessage = "(press spacebar)";

    private bool isPlayerNear;

    private void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
            SceneLoader.LoadScene(targetScene);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag(Tags.Player))
        {
            isPlayerNear = true;
            InteractPromptUI.Show(promptMessage);
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag(Tags.Player))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
        }
    }
}
