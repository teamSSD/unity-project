using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class BentoBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    public Vector3 defaultPosition;
    private ClickStateUtil clickStateUtil;
    private HoverStateUtil hoverStateUtil;
    private Animator animator;
    private Camera mainCamera;
    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ProcessAnimation(hoverStateUtil.IsHovering(), clickStateUtil.getState());
        ProcessMovement(clickStateUtil.getState());
    }
    private void ProcessAnimation(bool isHovering, ClickState clickState)
    {
        if (isHovering && clickState != ClickState.Dragging) animator.SetBool("Hovering", true);
        else animator.SetBool("Hovering", false);
    }
    private void ProcessMovement(ClickState clickState)
    {
        if (clickState == ClickState.Dragging)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = defaultPosition.z;
            gameObject.transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            Vector3 newPosition = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            newPosition.z = defaultPosition.z;
            transform.position = newPosition;
            if (Vector3.Distance(transform.position, defaultPosition) < 0.02f)
                transform.position = defaultPosition;
        }
    }
}
