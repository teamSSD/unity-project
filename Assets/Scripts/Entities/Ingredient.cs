using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
public class Ingredient : MonoBehaviour
{
    public IngredientData ingredientData;

    private Vector3 defaultPosition;
    private float speed = 10f;
    private Animator animator;
    private Camera mainCamera;
    private ClickStateUtil clickStateUtil;

    void Start()
    {
        defaultPosition = transform.position;
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragStart += DragStartRoutine;
        clickStateUtil.OnDragging += DraggingRoutine;
        clickStateUtil.OnDragEnd += DragEndRoutine;
        clickStateUtil.OnNone += NoneRoutine;
    }

    private void DragStartRoutine()
    {
        animator.SetBool("Clicking", true);
    }

    private void DraggingRoutine()
    {
        Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        target.z = defaultPosition.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void DragEndRoutine() {
        animator.SetBool("Clicking", false);
    }

    private void NoneRoutine() {
        if (transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }
}
