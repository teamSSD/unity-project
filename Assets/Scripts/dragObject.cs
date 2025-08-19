using UnityEngine;

public class DragMovePhysics : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private Rigidbody2D rb;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnMouseDown()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mousePos.x, mousePos.y, 0);
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 targetPos = mousePos + offset;
        rb.MovePosition(targetPos); // 물리 기반 이동
    }
}
