using UnityEngine;

/// <summary>
/// playerStore와 상호작용 시 MallSceneController.GoHome() 호출.
/// SceneTransitionInteraction과 같은 trigger + spacebar 패턴.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoHomeInteraction : MonoBehaviour
{
    [SerializeField] private string promptMessage = "(press spacebar to go home)";
    [Tooltip("다음 Mall 진입 시 player X 좌표. 0이면 transform.position.x 사용.")]
    [SerializeField] private float spawnX = 0f;
    private bool isPlayerNear;
    private MallSceneController mallController;

    private void Start()
    {
        mallController = Object.FindFirstObjectByType<MallSceneController>();
    }

    private void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            isPlayerNear = false;
            InteractPromptUI.Hide();
            var player = GameObject.FindGameObjectWithTag(Tags.Player);
            float x = (spawnX != 0f) ? spawnX : transform.position.x;
            float y = player != null ? player.transform.position.y : transform.position.y;
            SceneLoader.SetMallReturnPosition(new Vector3(x, y, 0));
            if (mallController != null) mallController.GoHome();
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
