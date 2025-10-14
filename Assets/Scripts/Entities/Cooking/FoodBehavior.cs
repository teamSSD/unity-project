using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
class FoodBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private Vector3 defaultPosition;
    private ClickStateUtil clickStateUtil;
    private Animator animator;
    private Camera mainCamera;

    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ClickState clickState = clickStateUtil.getState();
        ProcessAnimation(clickState);
        ProcessMovement(clickState);

    }

    private void ProcessAnimation(ClickState clickState)
    {
        if (clickState == ClickState.DragStart) animator.SetBool("Clicking", true);
        if (clickState == ClickState.DragEnd) animator.SetBool("Clicking", false);
    }
    
    private void ProcessMovement(ClickState clickState)
    {
        if (clickState == ClickState.Dragging)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }
}
