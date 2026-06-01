using System;
using UnityEngine;

public class ArrowButton : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private static readonly int Successed = Animator.StringToHash("Successed");
    private static readonly int Failed = Animator.StringToHash("Failed");

    public void Success()
    {
        animator.SetTrigger(Successed);
    }

    public void Fail()
    {
        animator.SetTrigger(Failed);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}