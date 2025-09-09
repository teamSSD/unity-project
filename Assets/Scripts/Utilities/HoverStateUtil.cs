using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HoverStateUtil : MonoBehaviour
{
    private Camera cam;
    private Collider2D col2d;
    private bool isHovering;

    void Start()
    {
        cam = Camera.main;
        col2d = GetComponent<Collider2D>();
    }

    void Update()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(cam.transform.position.z);
        Vector2 p = cam.ScreenToWorldPoint(mouse);

        Collider2D[] hits = Physics2D.OverlapPointAll(p);
        isHovering = false;

        foreach (var h in hits)
        {
            if (h == col2d)
            {
                isHovering = true;
                break;
            }
        }
    }

    public bool IsHovering() => isHovering;
}
