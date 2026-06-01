using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MouseAnimator : MonoBehaviour
{
    Animator animator;
    private readonly int isPressedHash = Animator.StringToHash("Clicking");
    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetBool(isPressedHash, false);
    }

    public void SetPress()
    {
        animator.SetBool(isPressedHash, true);
    }

    public void SetUnpressed()
    {
        animator.SetBool(isPressedHash, false);
    }
}
