using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ButtonAnimator : MonoBehaviour
{
    public KeyCode keyCode = KeyCode.Space;
    private Animator animator;
    private readonly int isPressedHash = Animator.StringToHash("IsPressed");

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        animator.SetBool(isPressedHash, Input.GetKey(keyCode));
    }
}
