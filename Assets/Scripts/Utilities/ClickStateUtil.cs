using UnityEngine;

public enum ClickState { None, ClickStart, Clicking, ClickEnd }

[RequireComponent(typeof(Collider2D))]
public class ClickStateUtil : MonoBehaviour
{
    private bool isDragging;
    private Camera mainCamera;
    private Collider2D col2d;
    private ClickState currentState;

    void Start()
    {
        mainCamera = Camera.main;
        col2d = GetComponent<Collider2D>();
        isDragging = false;
        currentState = ClickState.None;
    }

    void Update()
    {
        currentState = DistinguishState();
    }

    public ClickState GetClickState() => currentState;

    private ClickState DistinguishState()
    {
        // 클릭 시작: 내 콜라이더 위에서 마우스 다운
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 p = new Vector2(world.x, world.y);
            var hit = Physics2D.OverlapPoint(p);

            if (hit != null && hit == col2d)
            {
                isDragging = true;
                return ClickState.ClickStart;
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            return ClickState.Clicking;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            return ClickState.ClickEnd;
        }

        return ClickState.None;
    }
}
