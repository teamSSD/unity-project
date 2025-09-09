using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ingredient : MonoBehaviour
{
    public IngredientData ingredientData;
    public Vector3 defaultPosition;
    private Camera mainCamera;
    private bool isDragging;
    private float speed = 10f;
    private Animator animator;

    void Start()
    {
        mainCamera = Camera.main;
        defaultPosition = transform.position;
        isDragging = false;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (IsClicked())
        {
            isDragging = true;
            animator.SetBool("Clicking", true);
        }
        if (stillClicked())
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = defaultPosition.z;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        if (returning())
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
            {
                transform.position = defaultPosition;
            }
        }
        if (clickOff())
        {
            isDragging = false;
            animator.SetBool("Clicking", false);
        }
    }

    private bool IsClicked()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

            Collider2D hit = Physics2D.OverlapPoint(worldPoint2D);
            if (hit != null && hit.transform == transform)
            {
                return true;
            }
        }
        return false;
    }

    private bool stillClicked()
    {
        return isDragging && Input.GetMouseButton(0);
    }
    private bool returning()
    {
        return !isDragging && transform.position != defaultPosition;
    }

    private bool clickOff()
    {
        return Input.GetMouseButtonUp(0) && isDragging;
    }
}
