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

    private bool triggerSet = false;

    public void Update()
    {
        if (!guided)
        {
            animator.SetBool(isPressedHash, false);
            return;
        }
        if (Input.GetKeyDown(keyCode))
        {
            animator.SetBool(isPressedHash, true);
            return;
        }
        animator.SetBool(isPressedHash, false);
        if (!triggerSet)
        {
            animator.SetTrigger(requirePressedHash);
            triggerSet = true;
        }
    }

    public void Guide()
    {
        guided = true;
        triggerSet = false;
    }

    public void Unguide()
    {
        guided = false;
        triggerSet = false;
        animator.SetBool(isPressedHash, false);
    }
}