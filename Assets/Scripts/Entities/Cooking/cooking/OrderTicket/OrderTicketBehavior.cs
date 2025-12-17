using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
public class OrderTicketBehavior : MonoBehaviour
{
    public float speed = 10f;
    public Vector3 defaultPosition = new Vector3(0, 0, 0);
    private ClickStateUtil clickStateUtil;
    private Camera mainCamera;

    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
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
        }
        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, defaultPosition) < 0.01f)
                transform.position = defaultPosition;
        }
    }
}