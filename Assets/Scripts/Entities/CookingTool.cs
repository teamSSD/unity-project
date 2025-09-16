using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
public class CookingTool : MonoBehaviour
{
    public CookingToolData cookingToolData;
    public Vector3 defaultPosition;
    public int maxIngredient;
    private Camera mainCamera;
    private float speed = 10f;
    ClickStateUtil clickStateUtil;
    HoverStateUtil hoverStateUtil;
    private Animator animator;
    private bool dragging = false;
    void Start()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        clickStateUtil.OnClicked += ClickRoutine;
        clickStateUtil.OnDragging += DraggingRoutine;
        clickStateUtil.OnNone += NoneClickRoutine;

        hoverStateUtil = GetComponent<HoverStateUtil>();
        animator = GetComponent<Animator>();
        hoverStateUtil.OnHovering += HoverRoutine;
        hoverStateUtil.OnNone += NoneHoverRoutine;

        mainCamera = Camera.main;
    }

    private void ClickRoutine()
    {
        Debug.Log("호건이가~ 좋아하는~ 랜더어엄~ 게임!");
    }

    private void HoverRoutine()
    {
        if (!dragging)
        {
            animator.SetBool("Hovering", true);
        }
    }

    private void NoneHoverRoutine()
    {
        animator.SetBool("Hovering", false);
    }

    private void DraggingRoutine()
    {
        dragging = true;
        animator.SetBool("Hovering", false);
        Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void NoneClickRoutine() {
        dragging = false;
        if (transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }
}
