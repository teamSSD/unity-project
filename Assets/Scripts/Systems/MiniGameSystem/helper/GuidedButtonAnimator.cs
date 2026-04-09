using UnityEngine;

public class GuidedButtonAnimator : MonoBehaviour
{
    public KeyCode keyCode = KeyCode.Space;
    public bool guided = false;
    private Animator animator;
    private readonly int isPressedHash = Animator.StringToHash("IsPressed");
    private readonly int requirePressedHash = Animator.StringToHash("IsRequired");

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if (!guided)
        {
            animator.SetBool(isPressedHash, false);
            return;
        }
        if (guided && Input.GetKeyDown(keyCode))
        {
            animator.SetBool(isPressedHash, true);
            return;
        }
        animator.SetTrigger(requirePressedHash);
    }

    public void Guide()
    {
        guided = true;
    }
}