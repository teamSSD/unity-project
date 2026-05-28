using UnityEngine;

public class StairsInteraction : MonoBehaviour
{
    [SerializeField] private float yOffset;          // +3.8 (위층), -3.8 (아래층)
    [SerializeField] private string promptText = "press spacebar";

    private bool isPlayerNear = false;
    private static int lastTeleportFrame = -1;

    void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
            UseStairs();
    }

    void UseStairs()
    {
        if (Time.frameCount == lastTeleportFrame) return;
        lastTeleportFrame = Time.frameCount;

        var player = GameObject.FindWithTag(Tags.Player);
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        player.transform.position += new Vector3(0, yOffset, 0);
        isPlayerNear = false;
        InteractPromptUI.Hide();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(Tags.Player)) return;
        isPlayerNear = true;
        InteractPromptUI.Show(promptText);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(Tags.Player)) return;
        isPlayerNear = false;
        InteractPromptUI.Hide();
    }
}
