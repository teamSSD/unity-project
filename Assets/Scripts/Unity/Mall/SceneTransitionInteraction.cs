using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneTransitionInteraction : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string promptMessage = "(press spacebar)";
    [Tooltip("Mall로 돌아올 때 player X 좌표. 0이면 transform.position.x 사용.")]
    [SerializeField] private float spawnX = 0f;

    private bool isPlayerNear;

    private void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
            if (SceneLoader.CurrentScene == SceneNames.Mall)
            {
                var player = GameObject.FindGameObjectWithTag(Tags.Player);
                float x = (spawnX != 0f) ? spawnX : transform.position.x;
                float y = player != null ? player.transform.position.y : transform.position.y;
                SceneLoader.SetMallReturnPosition(new Vector3(x, y, 0));
            }
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
