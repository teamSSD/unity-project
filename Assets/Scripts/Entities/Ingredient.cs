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
    }

    void Update()
    {
        ClickState clickState = clickStateUtil.GetClickState();

        if (clickState == ClickState.ClickStart)
        {
            animator.SetBool("Clicking", true);
        }

        if (clickState == ClickState.Clicking)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = defaultPosition.z;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }

        if (clickState == ClickState.ClickEnd)
        {
            animator.SetBool("Clicking", false);
        }

        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }
}
