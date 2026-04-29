using UnityEngine;

public class FarmPathInteraction : MonoBehaviour
{
    private bool isPlayerNear = false;

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
