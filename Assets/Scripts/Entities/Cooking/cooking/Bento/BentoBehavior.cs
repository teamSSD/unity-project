using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteStackRenderer))]
[DisallowMultipleComponent]
public class BentoBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    public Vector3 defaultPosition;
    private ClickStateUtil clickStateUtil;
    private SpriteStackRenderer spriteStackRenderer;
    private Camera mainCamera;
    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        spriteStackRenderer = GetComponent<SpriteStackRenderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ClickState clickState = clickStateUtil.getState();
        ProcessMovement(clickState);
    }
    public bool locked = false;

    private void ProcessMovement(ClickState clickState)
    {
        if (locked) return;

        if (clickState == ClickState.Dragging)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = defaultPosition.z;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            Vector3 newPosition = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            newPosition.z = defaultPosition.z;
            transform.position = newPosition;
            if (Vector3.Distance(transform.position, defaultPosition) < 0.02f)
                transform.position = defaultPosition;
        }
    }
    public void AddTexture(Sprite sprite)
    {
        spriteStackRenderer.Add(sprite);
    }
    public void AddTexture(Sprite sprite, Vector2 xy)
    {
        spriteStackRenderer.Add(sprite, xy);
    }
}
