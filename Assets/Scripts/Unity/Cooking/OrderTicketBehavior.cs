using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[DisallowMultipleComponent]
public class OrderTicketBehavior : MonoBehaviour
{
    public float speed = 10f;
    public Vector3 defaultPosition = new Vector3(0, 0, 0);

    [Tooltip("호버/드래그 없을 때 y 위치 (윗쪽으로 살짝 숨음). 호버 시 defaultPosition.y로 내려옴.")]
    [SerializeField] private float restY = 5.54f;

    private ClickStateUtil clickStateUtil;
    private HoverStateUtil hoverStateUtil;
    private Camera mainCamera;

    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ClickState clickState = clickStateUtil.getState();
        ProcessMovement(clickState);
    }
    private void ProcessMovement(ClickState clickState)
    {
        if (clickState == ClickState.Dragging)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
            return;
        }

        // 호버 O → defaultPosition으로 내려옴, 호버 X → 위(restY)로 숨음.
        Vector3 rest = hoverStateUtil.IsHovering()
            ? defaultPosition
            : new Vector3(defaultPosition.x, restY, defaultPosition.z);

        if (transform.position != rest)
        {
            transform.position = Vector3.Lerp(transform.position, rest, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, rest) < 0.01f)
                transform.position = rest;
        }
    }
}
