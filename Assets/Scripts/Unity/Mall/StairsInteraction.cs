using System.Collections;
using UnityEngine;

public class StairsInteraction : MonoBehaviour
{
    [Tooltip("이 계단을 통해 도달할 짝 계단. 위층 lower 또는 아랫층 upper의 Transform.")]
    [SerializeField] private Transform targetStair;
    [SerializeField] private string promptText = "press spacebar";

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private bool isPlayerNear = false;
    private static bool isClimbing = false;

    void Update()
    {
        if (isPlayerNear && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
            BeginClimb();
    }

    void BeginClimb()
    {
        if (isClimbing) return;
        if (targetStair == null) { Debug.LogError($"[Stairs] {gameObject.name}: targetStair not wired"); return; }
        var player = GameObject.FindWithTag(Tags.Player);
        if (player == null) return;
        isPlayerNear = false;
        InteractPromptUI.Hide();
        StartCoroutine(ClimbCoroutine(player));
    }

    IEnumerator ClimbCoroutine(GameObject player)
    {
        isClimbing = true;
        UILockManager.Lock(UILockManager.Owner.Loading);

        var playerMove = player.GetComponent<PlayerMove>();
        var rb = player.GetComponent<Rigidbody2D>();
        var animator = player.GetComponent<Animator>();
        var sr = player.GetComponent<SpriteRenderer>();

        if (playerMove != null) playerMove.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (animator != null) animator.SetBool(IsMovingHash, true);

        float speed = playerMove != null ? playerMove.moveSpeed : 5f;

        // Phase 1: player → 현재 stair 위치
        yield return MoveTo(player, transform.position, speed, sr);
        // Phase 2: 현재 stair → target stair (계단 타기)
        yield return MoveTo(player, targetStair.position, speed, sr);

        if (animator != null) animator.SetBool(IsMovingHash, false);
        if (rb != null) rb.simulated = true;
        if (playerMove != null) playerMove.enabled = true;
        UILockManager.Unlock(UILockManager.Owner.Loading);
        isClimbing = false;
    }

    static IEnumerator MoveTo(GameObject player, Vector3 target, float speed, SpriteRenderer sr)
    {
        Vector3 start = player.transform.position;
        Vector3 end = new Vector3(target.x, target.y, start.z);
        float dx = end.x - start.x;
        if (sr != null && dx != 0) sr.flipX = (dx < 0);

        float distance = Vector3.Distance(start, end);
        if (distance < 0.001f) yield break;
        float duration = speed > 0f ? distance / speed : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            player.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        player.transform.position = end;
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
