using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(ScanColliderUtil))]
public class Ingredient : MonoBehaviour
{
    [Header("Ingredient SO")]
    public IngredientData ingredientData;
    public int tempQuantity = 10;
    [Header("Metadata")]
    public Vector3 defaultPosition;
    private float speed = 10f;

    private Animator animator;
    private Camera mainCamera;
    private ClickStateUtil clickStateUtil;
    private SpriteRenderer spriteRenderer;
    private ScanColliderUtil scanColliderUtil;

    private Collider2D selfCollider;
    private ContactFilter2D overlapFilter;
    private readonly Collider2D[] overlappingCollidersBuffer = new Collider2D[8];

    private readonly Collider2D[] scanBuf = new Collider2D[8];

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        clickStateUtil = GetComponent<ClickStateUtil>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        selfCollider = GetComponent<Collider2D>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();

        clickStateUtil.OnDragStart += DragStartRoutine;
        clickStateUtil.OnDragging += DraggingRoutine;
        clickStateUtil.OnDragEnd += DragEndRoutine;
        clickStateUtil.OnNone += NoneRoutine;

        if (ingredientData != null && ingredientData.defaultImage != null)
        {
            spriteRenderer.sprite = ingredientData.defaultImage;
        }
    }

    private void DragStartRoutine()
    {
        animator.SetBool("Clicking", true);
    }

    private void DraggingRoutine()
    {
        Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void DragEndRoutine()
    {
        animator.SetBool("Clicking", false);

        CookingTool cookingTool = scanColliderUtil.GetOverlappingWithComponent<CookingTool>();
        if (cookingTool != null)
        {
            Interact(cookingTool);
        }
    }

    private void NoneRoutine()
    {
        if (transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }

    private void Interact(CookingTool cookingTool)
    {
        if (cookingTool.AddIngredient(ingredientData))
        {
            SubQuantity();
        }
    }

    private void SubQuantity()
    {
        tempQuantity -= 1;
        if (tempQuantity <= 0)
        {
            Destroy(gameObject);
        }
    }
}
